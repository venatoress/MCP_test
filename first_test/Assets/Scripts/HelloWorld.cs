using UnityEngine;

public class HelloWorld : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < 10; i++)
        {
            string stars = "";

            // 핵심 로직: 별의 개수를 2*i + 1로 설정합니다.
            // i=0일 때 1개, i=1일 때 3개, i=2일 때 5개...
            int starCount = 2 * i + 1;

            for (int j = 0; j < starCount; j++)
            {
                stars += "*";
            }

            Debug.Log(stars);
        }
    }
    // Update is called once per frame
    void Update()
        {

        }
}