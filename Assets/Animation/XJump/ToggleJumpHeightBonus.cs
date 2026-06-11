using UnityEngine;

public class PlayerJumpHeightBonus : MonoBehaviour
{
    [Header("Jump Height Bonus")]
    public float jumpMultiplier = 2f;

    [Header("Respawn Protection")]
    public float triggerIgnoreAfterRespawn = 0.2f;

    private bool isBoosted = false;
    private float baseJumpForce = 10.1f;
    private PlayerMove playerMove;

    private float ignoreTriggerTimer = 0f;

    void Start()
    {
        playerMove = GetComponent<PlayerMove>();

        if (playerMove != null)
            baseJumpForce = playerMove.jumpForce;

        ApplyJumpForce();
    }

    void Update()
    {
        if (ignoreTriggerTimer > 0f)
            ignoreTriggerTimer -= Time.deltaTime;
    }

    public void ToggleJumpHeightBonus()
    {
        if (ignoreTriggerTimer > 0f)
            return;

        isBoosted = !isBoosted;
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
        ignoreTriggerTimer = triggerIgnoreAfterRespawn;
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