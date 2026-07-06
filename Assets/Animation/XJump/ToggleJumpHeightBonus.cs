using UnityEngine;

public class PlayerJumpHeightBonus : MonoBehaviour
{
    [Header("Jump Height Bonus")]
    public float jumpMultiplier = 2f;

    private bool isBoosted = false;
    private float baseJumpForce = 10.1f;
    private PlayerMove playerMove;

    void Start()
    {
        playerMove = GetComponent<PlayerMove>();

        if (playerMove != null)
            baseJumpForce = playerMove.jumpForce;

        ApplyJumpForce();
    }

    public void ToggleJumpHeightBonus()
    {
        SetBoostStateFromPlatform(!isBoosted);
    }

    public void SetBoostStateFromPlatform(bool value)
    {
        if (isBoosted == value)
            return;

        isBoosted = value;
        ApplyJumpForce();

        Debug.Log("Jump Height Boost: " + (isBoosted ? "ON" : "OFF"));
    }

    public bool IsBoosted()
    {
        return isBoosted;
    }

    public void SetBoostState(bool value)
    {
        isBoosted = value;
        ApplyJumpForce();

        Debug.Log("Jump Height Boost Restored: " + (isBoosted ? "ON" : "OFF"));
    }

    public void IgnoreTriggersAfterRespawn()
    {
        // Kept for compatibility with older PracticeModeManager versions.
    }

    private void ApplyJumpForce()
    {
        if (playerMove != null)
        {
            playerMove.jumpForce = isBoosted
                ? baseJumpForce * jumpMultiplier
                : baseJumpForce;
        }
    }
}