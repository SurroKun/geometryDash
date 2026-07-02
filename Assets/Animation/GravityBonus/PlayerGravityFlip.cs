using System.Collections;
using UnityEngine;

public class PlayerGravityFlip : MonoBehaviour
{
    [Header("References")]
    public Rigidbody rb;
    public PlayerMove playerMove;
    public Transform cameraPivot;
    public RunnerCameraFollow cameraFollow;
    public PracticeModeManager practiceModeManager;

    [Header("Gravity")]
    public float gravityForce = 20f;

    [Header("Rotate")]
    public bool rotatePlayerVisual = true;
    public Transform playerVisual;

    [Header("Flip Smoothing")]
    public float velocityKeepPercent = 0.18f;
    public float gravityPauseTime = 0.02f;

    [Header("Separate Rotate Speeds")]
    public float visualRotateDuration = 0.03f;
    public float cameraRotateDuration = 0.10f;

    [Header("Respawn Protection")]
    public float triggerIgnoreAfterRespawn = 0.2f;

    private bool isGravityInverted = false;
    private bool isFlipping = false;
    private bool gravityPaused = false;
    private float ignoreTriggerTimer = 0f;

    public bool IsGravityInverted()
    {
        return isGravityInverted;
    }

    public bool CanTriggerGravity()
    {
        return ignoreTriggerTimer <= 0f && !isFlipping;
    }

    private void Start()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (playerMove == null)
            playerMove = GetComponent<PlayerMove>();

        if (practiceModeManager == null)
            practiceModeManager = GetComponent<PracticeModeManager>();

        if (rb != null)
            rb.useGravity = false;
    }

    private void Update()
    {
        if (ignoreTriggerTimer > 0f)
            ignoreTriggerTimer -= Time.deltaTime;
    }

    private void FixedUpdate()
    {
        if (rb == null || gravityPaused)
            return;

        Vector3 gravityDirection = isGravityInverted ? Vector3.up : Vector3.down;
        rb.AddForce(gravityDirection * gravityForce, ForceMode.Acceleration);
    }

    public bool ToggleGravity()
    {
        if (!CanTriggerGravity())
            return false;

        StartCoroutine(SmoothFlip(true, true));
        return true;
    }

    public bool ToggleGravityWithoutSideInvert()
    {
        if (!CanTriggerGravity())
            return false;

        StartCoroutine(SmoothFlip(false, false));
        return true;
    }

    private IEnumerator SmoothFlip(
        bool changeSideInput,
        bool changeCamera
    )
    {
        isFlipping = true;
        gravityPaused = true;

        isGravityInverted = !isGravityInverted;

        if (playerMove != null && changeSideInput)
            playerMove.SetSideInputInverted(isGravityInverted);

        if (practiceModeManager != null)
            practiceModeManager.NotifyGravityChanged(isGravityInverted);

        if (changeCamera && cameraFollow != null)
            cameraFollow.SetCameraGravityInverted(isGravityInverted);

        if (rb != null)
        {
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                rb.linearVelocity.y * velocityKeepPercent,
                rb.linearVelocity.z
            );
        }

        float targetZ = isGravityInverted ? 180f : 0f;

        Transform visualTarget = playerVisual != null ? playerVisual : transform;

        Quaternion startVisualRot = visualTarget.rotation;
        Quaternion endVisualRot = Quaternion.Euler(0f, 0f, targetZ);

        Quaternion startCameraRot = Quaternion.identity;
        Quaternion endCameraRot = Quaternion.identity;

        if (cameraPivot != null)
        {
            startCameraRot = cameraPivot.rotation;
            endCameraRot = Quaternion.Euler(0f, 0f, targetZ);
        }

        float elapsed = 0f;
        float totalDuration = changeCamera
            ? Mathf.Max(visualRotateDuration, cameraRotateDuration)
            : visualRotateDuration;

        while (elapsed < totalDuration)
        {
            elapsed += Time.deltaTime;

            if (rotatePlayerVisual && visualTarget != null)
            {
                float visualT = visualRotateDuration > 0f
                    ? Mathf.Clamp01(elapsed / visualRotateDuration)
                    : 1f;

                visualT = Mathf.SmoothStep(0f, 1f, visualT);

                visualTarget.rotation = Quaternion.Lerp(
                    startVisualRot,
                    endVisualRot,
                    visualT
                );
            }

            if (changeCamera && cameraPivot != null)
            {
                float cameraT = cameraRotateDuration > 0f
                    ? Mathf.Clamp01(elapsed / cameraRotateDuration)
                    : 1f;

                cameraT = Mathf.SmoothStep(0f, 1f, cameraT);

                cameraPivot.rotation = Quaternion.Lerp(
                    startCameraRot,
                    endCameraRot,
                    cameraT
                );
            }

            if (gravityPaused && elapsed >= gravityPauseTime)
                gravityPaused = false;

            yield return null;
        }

        if (rotatePlayerVisual && visualTarget != null)
            visualTarget.rotation = endVisualRot;

        if (changeCamera && cameraPivot != null)
            cameraPivot.rotation = endCameraRot;

        gravityPaused = false;
        isFlipping = false;
    }

    public void SnapGravityState(bool value)
    {
        SnapGravityState(value, true);
    }

    public void SnapGravityState(bool value, bool syncCamera)
    {
        StopAllCoroutines();

        isGravityInverted = value;
        isFlipping = false;
        gravityPaused = false;

        if (playerMove != null)
            playerMove.SetSideInputInverted(isGravityInverted);

        if (syncCamera && cameraFollow != null)
            cameraFollow.SetCameraGravityInverted(isGravityInverted);

        float targetZ = isGravityInverted ? 180f : 0f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetZ);

        Transform visualTarget = playerVisual != null ? playerVisual : transform;

        if (rotatePlayerVisual && visualTarget != null)
            visualTarget.rotation = targetRot;

        if (syncCamera && cameraPivot != null)
            cameraPivot.rotation = targetRot;

        if (rb != null)
        {
            rb.useGravity = false;
            rb.angularVelocity = Vector3.zero;
            rb.linearVelocity = new Vector3(
                rb.linearVelocity.x,
                0f,
                rb.linearVelocity.z
            );
        }
    }

    public void IgnoreTriggersAfterRespawn()
    {
        ignoreTriggerTimer = triggerIgnoreAfterRespawn;
    }
}