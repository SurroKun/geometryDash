using System.Collections;
using UnityEngine;

public class PlayerJumpSpeedDashBonus : MonoBehaviour
{
    [Header("Keys")]
    public KeyCode jumpKey1 = KeyCode.W;
    public KeyCode jumpKey2 = KeyCode.UpArrow;

    [Header("Input Buffer")]
    public float jumpBufferTime = 0.18f;
    public bool allowBufferedPressBeforeEnter = true;

    [Header("VFX")]
    public GameObject activationEffectPrefab;
    public Vector3 effectOffset = Vector3.zero;

    [Header("Respawn Protection")]
    public float triggerIgnoreAfterRespawn = 0.2f;

    [Header("Cooldown")]
    public float cooldown = 0.15f;

    [Header("Compatibility")]
    public bool disableGravityFlipDuringDash = false;

    private PlayerMove playerMove;
    private PlayerGravityFlip gravityFlip;
    private Rigidbody rb;

    private SpeedDashZone currentZone;

    private bool insideDashZone = false;
    private bool usedThisEnter = false;

    private float cooldownTimer = 0f;
    private float ignoreTriggerTimer = 0f;
    private float jumpBufferTimer = 0f;

    private Coroutine dashCoroutine;

    private float speedBeforeDash;
    private bool rbGravityBeforeDash;
    private bool gravityFlipWasEnabled;

    private bool isDashing = false;

    private void Start()
    {
        playerMove = GetComponent<PlayerMove>();
        gravityFlip = GetComponent<PlayerGravityFlip>();
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        UpdateTimers();
        UpdateJumpBuffer();

        if (insideDashZone && jumpBufferTimer > 0f)
            TryUseSpeedDash();
    }

    private void UpdateTimers()
    {
        if (cooldownTimer > 0f)
            cooldownTimer -= Time.deltaTime;

        if (ignoreTriggerTimer > 0f)
            ignoreTriggerTimer -= Time.deltaTime;
    }

    private void UpdateJumpBuffer()
    {
        if (IsJumpPressed())
        {
            jumpBufferTimer = jumpBufferTime;
            return;
        }

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;
    }

    private bool IsJumpPressed()
    {
        return GameInput.WasKeyPressedThisFrame(jumpKey1) ||
               GameInput.WasKeyPressedThisFrame(jumpKey2);
    }

    private void OnTriggerEnter(Collider other)
    {
        SpeedDashZone zone =
            other.GetComponent<SpeedDashZone>() ??
            other.GetComponentInParent<SpeedDashZone>();

        if (zone != null)
        {
            insideDashZone = true;
            currentZone = zone;
            usedThisEnter = false;

            ResetCurrentZoneEffect();

            if (allowBufferedPressBeforeEnter &&
                jumpBufferTimer > 0f)
            {
                TryUseSpeedDash();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        SpeedDashZone zone =
            other.GetComponent<SpeedDashZone>() ??
            other.GetComponentInParent<SpeedDashZone>();

        if (zone != null && zone == currentZone)
        {
            insideDashZone = false;
            currentZone = null;
            usedThisEnter = false;
        }
    }

    public bool TryUseSpeedDash()
    {
        if (jumpBufferTimer <= 0f) return false;
        if (ignoreTriggerTimer > 0f) return false;
        if (!insideDashZone || currentZone == null) return false;
        if (cooldownTimer > 0f) return false;
        if (currentZone.oneUsePerEnter && usedThisEnter) return false;
        if (playerMove == null || rb == null) return false;

        usedThisEnter = true;
        cooldownTimer = cooldown;
        jumpBufferTimer = 0f;

        PlayCurrentZoneEffect();

        StartSpeedDash(
            currentZone.dashSpeedMultiplier,
            currentZone.dashDuration,
            currentZone.verticalFreezeDuration
        );

        return true;
    }

    private void PlayCurrentZoneEffect()
    {
        if (currentZone == null)
        {
            SpawnActivationEffect();
            return;
        }

        BonusEffectController effect =
            currentZone.GetComponent<BonusEffectController>();

        if (effect == null)
            effect = currentZone.GetComponentInChildren<BonusEffectController>(true);

        if (effect == null)
            effect = currentZone.GetComponentInParent<BonusEffectController>();

        if (effect != null)
        {
            effect.PlayActivation();
            return;
        }

        SpawnActivationEffect();
    }

    private void ResetCurrentZoneEffect()
    {
        if (currentZone == null)
            return;

        BonusEffectController effect =
            currentZone.GetComponent<BonusEffectController>();

        if (effect == null)
            effect = currentZone.GetComponentInChildren<BonusEffectController>(true);

        if (effect == null)
            effect = currentZone.GetComponentInParent<BonusEffectController>();

        if (effect != null)
            effect.ResetEffect();
    }

    private void SpawnActivationEffect()
    {
        if (activationEffectPrefab == null || rb == null)
            return;

        Instantiate(
            activationEffectPrefab,
            rb.position + effectOffset,
            Quaternion.identity
        );
    }

    private void StartSpeedDash(
        float multiplier,
        float dashDuration,
        float freezeDuration
    )
    {
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            RestoreAfterDash();
        }

        dashCoroutine =
            StartCoroutine(
                SpeedDashCoroutine(
                    multiplier,
                    dashDuration,
                    freezeDuration
                )
            );
    }

    private IEnumerator SpeedDashCoroutine(
        float multiplier,
        float dashDuration,
        float freezeDuration
    )
    {
        isDashing = true;

        speedBeforeDash = playerMove.forwardSpeed;
        rbGravityBeforeDash = rb.useGravity;

        if (gravityFlip != null)
            gravityFlipWasEnabled = gravityFlip.enabled;

        rb.useGravity = false;

        if (disableGravityFlipDuringDash &&
            gravityFlip != null)
        {
            gravityFlip.enabled = false;
        }

        playerMove.forwardSpeed = speedBeforeDash * multiplier;

        float timer = 0f;
        bool speedRestored = false;

        while (timer < Mathf.Max(dashDuration, freezeDuration))
        {
            if (timer < freezeDuration)
            {
                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    0f,
                    rb.linearVelocity.z
                );
            }

            if (!speedRestored && timer >= dashDuration)
            {
                playerMove.forwardSpeed = speedBeforeDash;
                speedRestored = true;
            }

            timer += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        RestoreAfterDash();
    }

    private void RestoreAfterDash()
    {
        if (playerMove != null)
            playerMove.forwardSpeed = speedBeforeDash;

        if (rb != null)
            rb.useGravity = rbGravityBeforeDash;

        if (disableGravityFlipDuringDash &&
            gravityFlip != null)
        {
            gravityFlip.enabled = gravityFlipWasEnabled;
        }

        isDashing = false;
        dashCoroutine = null;
    }

    public void CancelDashAndRestore()
    {
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            RestoreAfterDash();
        }

        insideDashZone = false;
        currentZone = null;
        usedThisEnter = false;
        jumpBufferTimer = 0f;
        cooldownTimer = 0f;
        ignoreTriggerTimer = triggerIgnoreAfterRespawn;
    }

    public void CancelDashForOtherBonus()
    {
        if (dashCoroutine != null)
        {
            StopCoroutine(dashCoroutine);
            RestoreAfterDash();
        }

        cooldownTimer = 0f;
    }

    public bool IsDashing()
    {
        return isDashing;
    }
}