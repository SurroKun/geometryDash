using UnityEngine;

public class SpeedBoostPlatform : MonoBehaviour
{
    public enum RespawnBonusState
    {
        KeepCurrent,
        ForceEnabled,
        ForceDisabled
    }

    [Header("Practice Respawn")]
    public RespawnBonusState respawnState = RespawnBonusState.ForceEnabled;

    private void OnTriggerEnter(Collider other)
    {
        PlayerSpeedBoostBonus bonus = other.GetComponent<PlayerSpeedBoostBonus>();

        if (bonus == null)
            bonus = other.GetComponentInParent<PlayerSpeedBoostBonus>();

        if (bonus != null)
            bonus.ToggleSpeedBoost();
    }

    public void ApplyRespawnState(PlayerSpeedBoostBonus bonus)
    {
        if (bonus == null)
            return;

        if (respawnState == RespawnBonusState.KeepCurrent)
            return;

        bool enabledAfterRespawn =
            respawnState == RespawnBonusState.ForceEnabled;

        bonus.SetBoostState(enabledAfterRespawn);
    }
}