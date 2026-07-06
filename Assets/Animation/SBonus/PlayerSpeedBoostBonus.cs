using UnityEngine;

public class PlayerSpeedBoostBonus : MonoBehaviour
{
    [Header("Speed Boost")]
    public float speedMultiplier = 1.5f;

    private bool isBoosted = false;
    private float baseForwardSpeed = 6.3f;
    private PlayerMove playerMove;

    void Start()
    {
        playerMove = GetComponent<PlayerMove>();

        if (playerMove != null)
            baseForwardSpeed = playerMove.forwardSpeed;

        ApplySpeed();
    }

    public void ToggleSpeedBoost()
    {
        SetBoostStateFromPlatform(!isBoosted);
    }

    public void SetBoostStateFromPlatform(bool value)
    {
        if (isBoosted == value)
            return;

        isBoosted = value;
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
        // Kept for compatibility with older PracticeModeManager versions.
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