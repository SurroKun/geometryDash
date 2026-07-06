using UnityEngine;

public class GravityFlipBonus : MonoBehaviour
{
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

        if (!gravityFlip.CanTriggerGravity())
            return;

        bool success = gravityFlip.ToggleGravity();

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