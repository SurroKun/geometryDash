using UnityEngine;

public class MenuFloatingParticle : MonoBehaviour
{
    [Header("Movement")]
    public Vector3 moveDirection = new Vector3(-1f, -1f, 0f);
    public float moveDistance = 5f;
    public float moveSpeed = 0.2f;

    [Header("Rotation")]
    public Vector3 rotationAxis = new Vector3(0f, 0f, 1f);
    public float rotationSpeed = 50f;

    private Vector3 startPos;
    private float t;

    void Start()
    {
        startPos = transform.localPosition;

        if (moveDirection != Vector3.zero)
            moveDirection.Normalize();

        // Чтобы все частицы не двигались одинаково
        t = Random.Range(0f, 1f);
    }

    void Update()
    {
        // Постепенно движемся
        t += moveSpeed * Time.deltaTime;

        // Когда дошли до конца - начинаем сначала
        if (t > 1f)
            t = 0f;

        transform.localPosition = startPos + moveDirection * (t * moveDistance);

        // Вращение
        transform.Rotate(rotationAxis, rotationSpeed * Time.deltaTime, Space.Self);
    }
}