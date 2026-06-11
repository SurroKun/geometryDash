using UnityEngine;

public class PlayerSpeedBoostBonus : MonoBehaviour
{
    [Header("Speed Boost")]
    public float speedMultiplier = 1.5f;

    [Header("Respawn Protection")]
    public float triggerIgnoreAfterRespawn = 0.2f;

    private bool isBoosted = false;
    private float baseForwardSpeed = 6.3f;
    private PlayerMove playerMove;

    private float ignoreTriggerTimer = 0f;

    void Start()
    {
        playerMove = GetComponent<PlayerMove>();

        if (playerMove != null)
            baseForwardSpeed = playerMove.forwardSpeed;

        ApplySpeed();
    }

    void Update()
    {
        if (ignoreTriggerTimer > 0f)
            ignoreTriggerTimer -= Time.deltaTime;
    }

    public void ToggleSpeedBoost()
    {
        if (ignoreTriggerTimer > 0f)
            return;

        isBoosted = !isBoosted;
        ApplySpeed();

        Debug.Log("Speed Boost: " + (isBoosted ? "ON" : "OFF"));
    }

    public bool IsBoosted()
    {
        return isBoosted;
    }

    public void SetBoostState(bool value)
    {
        isBoosted = value;
        ApplySpeed();

        Debug.Log("Speed Boost Restored: " + (isBoosted ? "ON" : "OFF"));
    }

    public void IgnoreTriggersAfterRespawn()
    {
        ignoreTriggerTimer = triggerIgnoreAfterRespawn;
    }

    private void ApplySpeed()
    {
        if (playerMove != null)
        {
            playerMove.forwardSpeed = isBoosted
                ? baseForwardSpeed * speedMultiplier
                : baseForwardSpeed;
        }
    }
}