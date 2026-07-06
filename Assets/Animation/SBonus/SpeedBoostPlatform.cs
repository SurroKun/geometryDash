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
            bonus = other.GetComponentInParent<PlayerSpeedBoostBonus>();

        if (bonus == null)
            return;

        if (mode == BonusPlatformMode.Toggle)
            bonus.ToggleSpeedBoost();
        else if (mode == BonusPlatformMode.ForceOn)
            bonus.SetBoostStateFromPlatform(true);
        else if (mode == BonusPlatformMode.ForceOff)
            bonus.SetBoostStateFromPlatform(false);
    }

    public void ApplyRespawnState(PlayerSpeedBoostBonus bonus)
    {
        if (bonus == null)
            return;

        bonus.SetBoostState(boostedAfterRespawn);
    }
}