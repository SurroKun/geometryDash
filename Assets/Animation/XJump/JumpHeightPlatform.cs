using UnityEngine;

public class JumpHeightPlatform : MonoBehaviour
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
        PlayerJumpHeightBonus bonus = other.GetComponent<PlayerJumpHeightBonus>();

        if (bonus == null)
            bonus = other.GetComponentInParent<PlayerJumpHeightBonus>();

        if (bonus != null)
            bonus.ToggleJumpHeightBonus();
    }

    public void ApplyRespawnState(PlayerJumpHeightBonus bonus)
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