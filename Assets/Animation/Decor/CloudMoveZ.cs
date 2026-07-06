using UnityEngine;

public class CloudMoveZ : MonoBehaviour
{
    [Header("Move")]
    public float speed = 2f;

    [Header("Pause")]
    public bool stopWhenGamePaused = true;

    void Update()
    {
        if (stopWhenGamePaused && Time.timeScale <= 0f)
            return;

        transform.position += Vector3.back * speed * Time.deltaTime;
    }
}