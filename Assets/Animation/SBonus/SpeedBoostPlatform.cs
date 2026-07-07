using UnityEngine;

public class SpeedBoostPlatform : MonoBehaviour
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
        PlayerSpeedBoostBonus bonus = other.GetComponent<PlayerSpeedBoostBonus>();

        if (bonus == null)
            return;

        bool wasBoosted = bonus.IsBoosted();

        if (mode == BonusPlatformMode.Toggle)
        {
            bonus.ToggleSpeedBoost();
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
            PlayPlayerSpeedEffect(other);
    }

    public void ApplyRespawnState(PlayerSpeedBoostBonus bonus)
    {
        if (bonus == null)
            return;

        bonus.SetBoostState(boostedAfterRespawn);
    }

    private void PlayPlayerSpeedEffect(Collider other)
    {
        PlayerBonusEffects effects = other.GetComponent<PlayerBonusEffects>();

        if (effects == null)
            effects = other.GetComponentInParent<PlayerBonusEffects>();

        if (effects == null)
            return;

        effects.PlaySpeedEffect();
    }
}