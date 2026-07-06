using UnityEngine;

public class JumpHeightPlatform : MonoBehaviour
{
    public enum BonusPlatformMode
    {
        Toggle,
        ForceOn,
        ForceOff
    }

    [Header("Platform Mode")]
    public BonusPlatformMode mode = BonusPlatformMode.Toggle;

    [Header("Practice Respawn")]
    public bool boostedAfterRespawn = true;

    private void OnTriggerEnter(Collider other)
    {
        PlayerJumpHeightBonus bonus = other.GetComponent<PlayerJumpHeightBonus>();

        if (bonus == null)
            bonus = other.GetComponentInParent<PlayerJumpHeightBonus>();

        if (bonus == null)
            return;

        if (mode == BonusPlatformMode.Toggle)
            bonus.ToggleJumpHeightBonus();
        else if (mode == BonusPlatformMode.ForceOn)
            bonus.SetBoostStateFromPlatform(true);
        else if (mode == BonusPlatformMode.ForceOff)
            bonus.SetBoostStateFromPlatform(false);
    }

    public void ApplyRespawnState(PlayerJumpHeightBonus bonus)
    {
        if (bonus == null)
            return;

        bonus.SetBoostState(boostedAfterRespawn);
    }
}