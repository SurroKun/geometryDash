using UnityEngine;

public class RunnerCameraFollow : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Gravity Camera State")]
    [SerializeField] private bool cameraGravityInverted = false;
    [SerializeField] private float gravityFlipDuration = 0.45f;
    [SerializeField] private bool reverseLaneAnglesWhenGravityInverted = true;

    [Header("Offset")]
    [SerializeField] private float distance = -8f;
    [SerializeField] private float height = 6f;

    [Header("View Angles")]
    [SerializeField] private float leftAngle = -5f;
    [SerializeField] private float centerAngle = -17.5f;
    [SerializeField] private float rightAngle = -30f;

    [Header("Smooth")]
    [SerializeField] private float angleSmooth = 8f;

    [Header("Debug")]
    [SerializeField, Range(-1, 1)] private int currentLane = 0;
    [SerializeField] private float currentAngle;
    [SerializeField] private float currentGravityAngle;

    private float gravityAngleVelocity;

    private void Start()
    {
        currentAngle = GetTargetAngle();
        currentGravityAngle = GetTargetGravityAngle();
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        float targetAngle = GetTargetAngle();

        currentAngle = Mathf.Lerp(
            currentAngle,
            targetAngle,
            1f - Mathf.Exp(-angleSmooth * Time.deltaTime)
        );

        currentGravityAngle = Mathf.SmoothDampAngle(
            currentGravityAngle,
            GetTargetGravityAngle(),
            ref gravityAngleVelocity,
            gravityFlipDuration
        );

        Quaternion gravityRotation =
            Quaternion.AngleAxis(currentGravityAngle, Vector3.forward);

        Vector3 gravityUp = gravityRotation * Vector3.up;

        Quaternion orbit = Quaternion.AngleAxis(currentAngle, gravityUp);

        Vector3 horizontalOffset =
            orbit * (Vector3.forward * -distance);

        Vector3 verticalOffset =
            gravityUp * height;

        transform.position =
            target.position +
            horizontalOffset +
            verticalOffset;

        transform.rotation = Quaternion.LookRotation(
            target.position - transform.position,
            gravityUp
        );
    }

    public void SetLane(int lane)
    {
        currentLane = Mathf.Clamp(lane, -1, 1);
    }

    public bool IsCameraGravityInverted()
    {
        return cameraGravityInverted;
    }

    public void SetCameraGravityInverted(bool inverted)
    {
        cameraGravityInverted = inverted;
    }

    public void ToggleCameraGravity()
    {
        cameraGravityInverted = !cameraGravityInverted;
    }

    private float GetTargetAngle()
    {
        int lane = currentLane;

        if (reverseLaneAnglesWhenGravityInverted && cameraGravityInverted)
            lane *= -1;

        if (lane < 0)
            return leftAngle;

        if (lane > 0)
            return rightAngle;

        return centerAngle;
    }

    private float GetTargetGravityAngle()
    {
        return cameraGravityInverted ? 180f : 0f;
    }
}