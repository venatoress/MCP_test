using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;

public class TetrisGame : MonoBehaviour
{
    private const int Width = 10;
    private const int Height = 20;
    private const int Empty = -1;
    private const float CellSize = 0.5f;

    private readonly Color[] colors =
    {
        new Color(0.12f, 0.82f, 0.92f),
        new Color(0.32f, 0.56f, 0.96f),
        new Color(0.96f, 0.55f, 0.12f),
        new Color(0.98f, 0.84f, 0.18f),
        new Color(0.22f, 0.78f, 0.47f),
        new Color(0.70f, 0.42f, 0.95f),
        new Color(0.96f, 0.28f, 0.38f)
    };

    private readonly string[] baseMasks =
    {
        "0000111100000000",
        "1000111000000000",
        "0010111000000000",
        "0110011000000000",
        "0110110000000000",
        "0100111000000000",
        "1100011000000000"
    };

    private int[,] board;
    private SpriteRenderer[,] cells;
    private SpriteRenderer[] nextCells;
        private Material cellMaterial;
private Sprite cellSprite;
    private string[,] masks;
    private Piece activePiece;
    private Piece nextPiece;
    private List<int> bag = new List<int>();
    private System.Random random = new System.Random();
    private float dropTimer;
    private float dropInterval = 0.85f;
    private int score;
    private int level = 1;
    private int lines;
    private bool paused;
    private bool gameOver;
    private Vector2 origin;
    private GUIStyle titleStyle;
    private GUIStyle labelStyle;
    private GUIStyle buttonStyle;

    private class Piece
    {
        public int Type;
        public int Rotation;
        public int X;
        public int Y;

        public Piece(int type)
        {
            Type = type;
            Rotation = 0;
            X = 3;
            Y = -1;
        }
    }

    private void Awake()
    {
        origin = new Vector2(-Width * CellSize * 0.5f, -Height * CellSize * 0.5f);
        board = new int[Width, Height];
        masks = BuildMasks();
        CreateSprite();
        CreateCells();
        ConfigureCamera();
        NewGame();
    }

private void Update()
    {
        HandleKeyboard();

        if (paused || gameOver)
        {
            return;
        }

        dropTimer += Time.deltaTime;
        if (dropTimer >= dropInterval)
        {
            SoftDrop(false);
        }
    }

private void OnGUI()
    {
        EnsureStyles();
        float x = Screen.width - 260f;
        GUILayout.BeginArea(new Rect(x, 28f, 230f, 320f), GUI.skin.box);
        GUILayout.Label("TETRIS", titleStyle);
        GUILayout.Space(8f);
        GUILayout.Label("Score  " + score, labelStyle);
        GUILayout.Label("Level  " + level, labelStyle);
        GUILayout.Label("Lines  " + lines, labelStyle);
        GUILayout.Space(12f);
        GUILayout.Label(gameOver ? "Game Over" : paused ? "Paused" : "Playing", labelStyle);
        GUILayout.Space(12f);
        if (GUILayout.Button(gameOver ? "Restart" : "New Game", buttonStyle, GUILayout.Height(36f)))
        {
            NewGame();
        }
        if (GUILayout.Button(paused ? "Resume" : "Pause", buttonStyle, GUILayout.Height(36f)))
        {
            TogglePause();
        }
        GUILayout.Space(12f);
        GUILayout.Label("Move: Left / Right", labelStyle);
        GUILayout.Label("Rotate: Up or X", labelStyle);
        GUILayout.Label("Drop: Down / Space", labelStyle);
        GUILayout.Label("Pause: P   Restart: R", labelStyle);
        GUILayout.EndArea();
    }

private void HandleKeyboard()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard.rKey.wasPressedThisFrame)
        {
            NewGame();
            return;
        }

        if (keyboard.pKey.wasPressedThisFrame)
        {
            TogglePause();
            return;
        }

        if (paused || gameOver)
        {
            return;
        }

        if (keyboard.leftArrowKey.wasPressedThisFrame)
        {
            TryMove(-1, 0);
        }
        else if (keyboard.rightArrowKey.wasPressedThisFrame)
        {
            TryMove(1, 0);
        }

        if (keyboard.downArrowKey.wasPressedThisFrame)
        {
            SoftDrop(true);
        }

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.xKey.wasPressedThisFrame)
        {
            TryRotate();
        }

        if (keyboard.spaceKey.wasPressedThisFrame)
        {
            HardDrop();
        }
    }


    private string[,] BuildMasks()
    {
        var result = new string[baseMasks.Length, 4];
        for (int type = 0; type < baseMasks.Length; type++)
        {
            result[type, 0] = baseMasks[type];
            for (int rotation = 1; rotation < 4; rotation++)
            {
                result[type, rotation] = RotateMask(result[type, rotation - 1]);
            }
        }
        return result;
    }

private string RotateMask(string mask)
    {
        char[] rotated = new char[16];
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                int sourceIndex = x + y * 4;
                int rotatedX = 3 - y;
                int rotatedY = x;
                rotated[rotatedX + rotatedY * 4] = mask[sourceIndex];
            }
        }
        return new string(rotated);
    }

private void CreateSprite()
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        cellSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }
        cellMaterial = new Material(shader);
    }

    private void CreateCells()
    {
        cells = new SpriteRenderer[Width, Height];
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                cells[x, y] = CreateCell("Cell_" + x + "_" + y, BoardToWorld(x, y), transform, 0);
            }
        }

        nextCells = new SpriteRenderer[4];
        var previewRoot = new GameObject("Next Preview").transform;
        previewRoot.SetParent(transform, false);
        for (int i = 0; i < nextCells.Length; i++)
        {
            nextCells[i] = CreateCell("Next_" + i, Vector3.zero, previewRoot, 2);
        }
    }

    private SpriteRenderer CreateCell(string name, Vector3 position, Transform parent, int sortingOrder)
    {
        var cellObject = new GameObject(name);
        cellObject.transform.SetParent(parent, false);
        cellObject.transform.position = position;
        cellObject.transform.localScale = Vector3.one * (CellSize * 0.92f);
        var renderer = cellObject.AddComponent<SpriteRenderer>();
                renderer.sharedMaterial = cellMaterial;
renderer.sprite = cellSprite;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private void ConfigureCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            var cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            camera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
        }
        camera.orthographic = true;
        camera.orthographicSize = 5.8f;
        camera.transform.position = new Vector3(1.5f, 0f, -10f);
        camera.backgroundColor = new Color(0.05f, 0.07f, 0.10f);
    }

    private Vector3 BoardToWorld(int x, int y)
    {
        return new Vector3(origin.x + x * CellSize + CellSize * 0.5f, origin.y + (Height - 1 - y) * CellSize + CellSize * 0.5f, 0f);
    }

    private void NewGame()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                board[x, y] = Empty;
            }
        }

        score = 0;
        level = 1;
        lines = 0;
        dropInterval = 0.85f;
        dropTimer = 0f;
        paused = false;
        gameOver = false;
        bag.Clear();
        RefillBag();
        activePiece = CreatePiece(NextType());
        nextPiece = CreatePiece(NextType());
        Render();
    }

    private Piece CreatePiece(int type)
    {
        var piece = new Piece(type);
        piece.X = type == 3 ? 4 : 3;
        piece.Y = -1;
        return piece;
    }

    private int NextType()
    {
        if (bag.Count == 0)
        {
            RefillBag();
        }
        int last = bag.Count - 1;
        int value = bag[last];
        bag.RemoveAt(last);
        return value;
    }

    private void RefillBag()
    {
        bag.Clear();
        for (int i = 0; i < baseMasks.Length; i++)
        {
            bag.Add(i);
        }
        for (int i = bag.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            int temp = bag[i];
            bag[i] = bag[j];
            bag[j] = temp;
        }
    }

    private bool TryMove(int dx, int dy)
    {
        if (Collides(activePiece, dx, dy, activePiece.Rotation))
        {
            return false;
        }
        activePiece.X += dx;
        activePiece.Y += dy;
        Render();
        return true;
    }

    private void TryRotate()
    {
        int nextRotation = (activePiece.Rotation + 1) % 4;
        int[] kicks = { 0, -1, 1, -2, 2 };
        foreach (int kick in kicks)
        {
            if (!Collides(activePiece, kick, 0, nextRotation))
            {
                activePiece.X += kick;
                activePiece.Rotation = nextRotation;
                Render();
                return;
            }
        }
    }

    private void SoftDrop(bool manual)
    {
        dropTimer = 0f;
        if (TryMove(0, 1))
        {
            if (manual)
            {
                score += 1;
            }
            return;
        }
        LockPiece();
    }

    private void HardDrop()
    {
        int distance = 0;
        while (TryMove(0, 1))
        {
            distance++;
        }
        score += distance * 2;
        LockPiece();
    }

    private void LockPiece()
    {
        foreach (Vector2Int cell in GetCells(activePiece, activePiece.Rotation))
        {
            int x = activePiece.X + cell.x;
            int y = activePiece.Y + cell.y;
            if (y >= 0 && y < Height && x >= 0 && x < Width)
            {
                board[x, y] = activePiece.Type;
            }
        }

        ClearLines();
        activePiece = nextPiece;
        nextPiece = CreatePiece(NextType());
        if (Collides(activePiece, 0, 0, activePiece.Rotation))
        {
            gameOver = true;
        }
        Render();
    }

    private void ClearLines()
    {
        int cleared = 0;
        for (int y = Height - 1; y >= 0; y--)
        {
            bool full = true;
            for (int x = 0; x < Width; x++)
            {
                if (board[x, y] == Empty)
                {
                    full = false;
                    break;
                }
            }

            if (!full)
            {
                continue;
            }

            for (int moveY = y; moveY > 0; moveY--)
            {
                for (int x = 0; x < Width; x++)
                {
                    board[x, moveY] = board[x, moveY - 1];
                }
            }
            for (int x = 0; x < Width; x++)
            {
                board[x, 0] = Empty;
            }
            cleared++;
            y++;
        }

        if (cleared > 0)
        {
            int[] lineScores = { 0, 100, 300, 500, 800 };
            score += lineScores[cleared] * level;
            lines += cleared;
            level = lines / 10 + 1;
            dropInterval = Mathf.Max(0.12f, 0.85f - (level - 1) * 0.07f);
        }
    }

    private bool Collides(Piece piece, int dx, int dy, int rotation)
    {
        foreach (Vector2Int cell in GetCells(piece, rotation))
        {
            int x = piece.X + cell.x + dx;
            int y = piece.Y + cell.y + dy;
            if (x < 0 || x >= Width || y >= Height)
            {
                return true;
            }
            if (y >= 0 && board[x, y] != Empty)
            {
                return true;
            }
        }
        return false;
    }

    private IEnumerable<Vector2Int> GetCells(Piece piece, int rotation)
    {
        string mask = masks[piece.Type, rotation];
        for (int y = 0; y < 4; y++)
        {
            for (int x = 0; x < 4; x++)
            {
                if (mask[x + y * 4] == '1')
                {
                    yield return new Vector2Int(x, y);
                }
            }
        }
    }

    private void TogglePause()
    {
        if (!gameOver)
        {
            paused = !paused;
        }
    }

    private void Render()
    {
        Color emptyColor = new Color(0.10f, 0.13f, 0.17f);
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int value = board[x, y];
                cells[x, y].color = value == Empty ? emptyColor : colors[value];
            }
        }

        if (activePiece != null)
        {
            DrawGhost();
            foreach (Vector2Int cell in GetCells(activePiece, activePiece.Rotation))
            {
                int x = activePiece.X + cell.x;
                int y = activePiece.Y + cell.y;
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    cells[x, y].color = colors[activePiece.Type];
                }
            }
        }

        RenderNextPiece();
    }

    private void DrawGhost()
    {
        var ghost = new Piece(activePiece.Type)
        {
            X = activePiece.X,
            Y = activePiece.Y,
            Rotation = activePiece.Rotation
        };

        while (!Collides(ghost, 0, 1, ghost.Rotation))
        {
            ghost.Y++;
        }

        Color ghostColor = colors[activePiece.Type];
        ghostColor.a = 0.35f;
        foreach (Vector2Int cell in GetCells(ghost, ghost.Rotation))
        {
            int x = ghost.X + cell.x;
            int y = ghost.Y + cell.y;
            if (x >= 0 && x < Width && y >= 0 && y < Height && board[x, y] == Empty)
            {
                cells[x, y].color = Color.Lerp(new Color(0.10f, 0.13f, 0.17f), ghostColor, 0.45f);
            }
        }
    }

    private void RenderNextPiece()
    {
        foreach (SpriteRenderer renderer in nextCells)
        {
            renderer.enabled = false;
        }

        if (nextPiece == null)
        {
            return;
        }

        int index = 0;
        Vector3 previewOrigin = new Vector3(origin.x + Width * CellSize + 0.75f, origin.y + Height * CellSize - 1.3f, 0f);
        foreach (Vector2Int cell in GetCells(nextPiece, 0))
        {
            if (index >= nextCells.Length)
            {
                break;
            }
            nextCells[index].enabled = true;
            nextCells[index].color = colors[nextPiece.Type];
            nextCells[index].transform.position = previewOrigin + new Vector3(cell.x * CellSize, -cell.y * CellSize, 0f);
            index++;
        }
    }

    private void EnsureStyles()
    {
        if (titleStyle != null)
        {
            return;
        }

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 28,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 16,
            normal = { textColor = new Color(0.90f, 0.94f, 1f) }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold
        };
    }
}
