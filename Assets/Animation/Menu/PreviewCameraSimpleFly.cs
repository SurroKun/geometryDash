using UnityEngine;

public class PreviewCameraSimpleFly : MonoBehaviour
{
    [Header("Points")]
    public Transform startPoint;
    public Transform endPoint;

    [Header("Movement")]
    public float moveSpeed = 5f;
    public float pauseAtEnd = 1f;

    [Header("Rotation")]
    public bool useStartPointRotation = true;
    public bool autoRotateToEnd = false;
    public Transform customLookTarget;

    private float journeyLength;
    private float currentDistance;
    private bool isPaused;
    private float pauseTimer;

    void Start()
    {
        RecalculatePath();
        ResetToStart();
    }

    void Update()
    {
        if (startPoint == null || endPoint == null)
            return;

        if (journeyLength <= 0.001f)
            return;

        if (isPaused)
        {
            pauseTimer -= Time.deltaTime;

            if (pauseTimer <= 0f)
            {
                isPaused = false;
                ResetToStart();
            }

            return;
        }

        currentDistance += moveSpeed * Time.deltaTime;
        float t = currentDistance / journeyLength;
        t = Mathf.Clamp01(t);

        transform.position = Vector3.Lerp(startPoint.position, endPoint.position, t);

        if (autoRotateToEnd)
        {
            UpdateLook();
        }

        if (t >= 1f)
        {
            isPaused = true;
            pauseTimer = pauseAtEnd;
        }
    }

    void UpdateLook()
    {
        if (customLookTarget != null)
        {
            transform.LookAt(customLookTarget.position);
        }
        else if (endPoint != null)
        {
            transform.LookAt(endPoint.position);
        }
    }

    void RecalculatePath()
    {
        if (startPoint != null && endPoint != null)
            journeyLength = Vector3.Distance(startPoint.position, endPoint.position);
        else
            journeyLength = 0f;
    }

    public void ResetToStart()
    {
        currentDistance = 0f;

        if (startPoint != null)
        {
            transform.position = startPoint.position;

            if (useStartPointRotation)
                transform.rotation = startPoint.rotation;
        }

        if (autoRotateToEnd)
        {
            UpdateLook();
        }
    }

    public void SetPoints(Transform newStart, Transform newEnd)
    {
        startPoint = newStart;
        endPoint = newEnd;

        isPaused = false;
        pauseTimer = 0f;

        RecalculatePath();
        ResetToStart();
    }
}