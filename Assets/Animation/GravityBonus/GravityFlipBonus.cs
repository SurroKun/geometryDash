using UnityEngine;

public class GravityFlipBonus : MonoBehaviour
{
    public enum GravityPlatformMode
    {
        Toggle,
        ForceInverted,
        ForceNormal
    }

    [Header("Platform Mode")]
    public GravityPlatformMode mode = GravityPlatformMode.Toggle;

    [Header("Trigger Protection")]
    public float retriggerDelay = 0.15f;

    [Header("Practice Respawn")]
    public bool gravityInvertedAfterRespawn = true;
    public bool sideInputInvertedAfterRespawn = true;
    public bool syncCameraAfterRespawn = true;

    private float retriggerTimer = 0f;

    void Update()
    {
        if (retriggerTimer > 0f)
            retriggerTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (retriggerTimer > 0f)
            return;

        PlayerGravityFlip gravityFlip = other.GetComponent<PlayerGravityFlip>();

        if (gravityFlip == null)
            gravityFlip = other.GetComponentInParent<PlayerGravityFlip>();

        if (gravityFlip == null)
            return;

        bool success = false;

        if (mode == GravityPlatformMode.Toggle)
            success = gravityFlip.ToggleGravity();
        else if (mode == GravityPlatformMode.ForceInverted)
            success = gravityFlip.SetGravityStateFromPlatform(true);
        else if (mode == GravityPlatformMode.ForceNormal)
            success = gravityFlip.SetGravityStateFromPlatform(false);

        if (!success)
            return;

        retriggerTimer = retriggerDelay;
    }

    public void ApplyRespawnState(PlayerGravityFlip gravityFlip)
    {
        if (gravityFlip == null)
            return;

        gravityFlip.SnapGravityState(
            gravityInvertedAfterRespawn,
            sideInputInvertedAfterRespawn,
            syncCameraAfterRespawn
        );
    }
}