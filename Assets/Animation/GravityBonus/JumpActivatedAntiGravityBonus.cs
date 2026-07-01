using System.Collections;
using UnityEngine;

public class JumpActivatedAntiGravityBonus : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode jumpKey1 = KeyCode.W;
    public KeyCode jumpKey2 = KeyCode.UpArrow;

    [Header("Bounce")]
    public float bounceForce = 12f;
    public bool resetVerticalVelocityBeforeBounce = true;

    [Header("Bounce Fix")]
    public bool applyBounceAfterFixedUpdate = true;
    public int fixedFramesBeforeBounce = 1;

    [Header("Compatibility")]
    public bool cancelSpeedDashBeforeActivation = true;

    [Header("Timing")]
    public float jumpBufferTime = 0.18f;

    [Header("Settings")]
    public bool oneUsePerEnter = true;
    public bool destroyAfterUse = false;

    private PlayerGravityFlip currentGravityFlip;
    private PracticeModeManager currentPracticeMode;
    private PlayerJumpSpeedDashBonus currentSpeedDashBonus;
    private Rigidbody currentRb;

    private bool playerInside = false;
    private bool usedThisEnter = false;
    private float jumpBufferTimer = 0f;

    private void Update()
    {
        if (IsJumpPressed())
            jumpBufferTimer = jumpBufferTime;
        else if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

        if (!playerInside ||
            currentGravityFlip == null ||
            currentRb == null)
        {
            return;
        }

        if (oneUsePerEnter && usedThisEnter)
            return;

        if (jumpBufferTimer > 0f)
            ActivateBonus();
    }

    private bool IsJumpPressed()
    {
        return GameInput.WasKeyPressedThisFrame(jumpKey1) ||
               GameInput.WasKeyPressedThisFrame(jumpKey2);
    }

    private void ActivateBonus()
    {
        PlayerGravityFlip savedGravityFlip = currentGravityFlip;
        Rigidbody savedRb = currentRb;
        PracticeModeManager savedPracticeMode = currentPracticeMode;
        PlayerJumpSpeedDashBonus savedSpeedDashBonus = currentSpeedDashBonus;

        if (savedGravityFlip == null || savedRb == null)
            return;

        if (cancelSpeedDashBeforeActivation &&
            savedSpeedDashBonus != null)
        {
            savedSpeedDashBonus.CancelDashForOtherBonus();
        }

        if (!savedGravityFlip.CanTriggerGravity())
            return;

        Transform cam = savedGravityFlip.cameraPivot;
        Quaternion savedCameraRotation = Quaternion.identity;
        float lockTime = savedGravityFlip.cameraRotateDuration + 0.05f;

        if (cam != null)
            savedCameraRotation = cam.rotation;

        bool success = savedGravityFlip.ToggleGravityWithoutSideInvert();

        if (!success)
            return;

        if (savedPracticeMode != null)
            savedPracticeMode.NotifyTemporaryGravityBonusUsed();

        jumpBufferTimer = 0f;
        usedThisEnter = true;

        if (cam != null)
        {
            StartCoroutine(
                LockCameraRotation(
                    cam,
                    savedCameraRotation,
                    lockTime
                )
            );
        }

        if (applyBounceAfterFixedUpdate)
        {
            StartCoroutine(
                ApplyBounceDelayed(
                    savedRb,
                    savedGravityFlip
                )
            );
        }
        else
        {
            ApplyBounceNow(
                savedRb,
                savedGravityFlip
            );
        }

        if (destroyAfterUse)
            Destroy(gameObject);
    }

    private IEnumerator ApplyBounceDelayed(
        Rigidbody targetRb,
        PlayerGravityFlip targetGravityFlip
    )
    {
        int frames = Mathf.Max(1, fixedFramesBeforeBounce);

        for (int i = 0; i < frames; i++)
            yield return new WaitForFixedUpdate();

        ApplyBounceNow(
            targetRb,
            targetGravityFlip
        );
    }

    private void ApplyBounceNow(
        Rigidbody targetRb,
        PlayerGravityFlip targetGravityFlip
    )
    {
        if (targetRb == null ||
            targetGravityFlip == null)
        {
            return;
        }

        bool inverted = targetGravityFlip.IsGravityInverted();

        Vector3 jumpDirection =
            inverted ? Vector3.down : Vector3.up;

        Vector3 velocity = targetRb.linearVelocity;

        if (resetVerticalVelocityBeforeBounce)
            velocity.y = 0f;

        velocity.y = jumpDirection.y * bounceForce;
        targetRb.linearVelocity = velocity;

        Debug.Log(
            "AntiGravity bounce applied. Direction: " +
            jumpDirection +
            " | Force: " +
            bounceForce
        );
    }

    private IEnumerator LockCameraRotation(
        Transform cam,
        Quaternion targetRotation,
        float duration
    )
    {
        float timer = 0f;

        while (timer < duration)
        {
            if (cam != null)
                cam.rotation = targetRotation;

            timer += Time.deltaTime;
            yield return null;
        }

        if (cam != null)
            cam.rotation = targetRotation;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerGravityFlip gravityFlip =
            other.GetComponent<PlayerGravityFlip>();

        if (gravityFlip == null)
            gravityFlip =
                other.GetComponentInParent<PlayerGravityFlip>();

        if (gravityFlip != null)
        {
            currentGravityFlip = gravityFlip;

            currentPracticeMode =
                gravityFlip.GetComponent<PracticeModeManager>();

            if (currentPracticeMode == null)
            {
                currentPracticeMode =
                    gravityFlip.GetComponentInParent<PracticeModeManager>();
            }

            currentSpeedDashBonus =
                gravityFlip.GetComponent<PlayerJumpSpeedDashBonus>();

            if (currentSpeedDashBonus == null)
            {
                currentSpeedDashBonus =
                    gravityFlip.GetComponentInParent<PlayerJumpSpeedDashBonus>();
            }

            currentRb =
                gravityFlip.rb != null
                    ? gravityFlip.rb
                    : gravityFlip.GetComponent<Rigidbody>();

            playerInside = true;
            usedThisEnter = false;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerGravityFlip gravityFlip =
            other.GetComponent<PlayerGravityFlip>();

        if (gravityFlip == null)
            gravityFlip =
                other.GetComponentInParent<PlayerGravityFlip>();

        if (gravityFlip != null &&
            gravityFlip == currentGravityFlip)
        {
            currentGravityFlip = null;
            currentPracticeMode = null;
            currentSpeedDashBonus = null;
            currentRb = null;

            playerInside = false;
            usedThisEnter = false;
            jumpBufferTimer = 0f;
        }
    }
}
