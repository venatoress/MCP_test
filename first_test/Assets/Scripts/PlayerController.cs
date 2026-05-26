using UnityEngine;

public class SimplePlayerController : MonoBehaviour
{
    [SerializeField]
    private float speed = 5f;

    private void Update()
    {
        float moveInput = Input.GetAxisRaw("Horizontal");
        Vector3 move = Vector3.right * moveInput * speed * Time.deltaTime;
        transform.position += move;
    }
}
