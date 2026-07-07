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
            return;

        bool wasBoosted = bonus.IsBoosted();

        if (mode == BonusPlatformMode.Toggle)
        {
            bonus.ToggleJumpHeightBonus();
        }
        else if (mode == BonusPlatformMode.ForceOn)
        {
            bonus.SetBoostState(true);
        }
        else if (mode == BonusPlatformMode.ForceOff)
        {
            bonus.SetBoostState(false);
        }

        bool isBoosted = bonus.IsBoosted();

        if (wasBoosted != isBoosted)
            PlayPlayerJumpEffect(other);
    }

    public void ApplyRespawnState(PlayerJumpHeightBonus bonus)
    {
        if (bonus == null)
            return;

        bonus.SetBoostState(boostedAfterRespawn);
    }

    private void PlayPlayerJumpEffect(Collider other)
    {
        PlayerBonusEffects effects = other.GetComponent<PlayerBonusEffects>();

        if (effects == null)
            effects = other.GetComponentInParent<PlayerBonusEffects>();

        if (effects == null)
            return;

        effects.PlayJumpEffect();
    }
}