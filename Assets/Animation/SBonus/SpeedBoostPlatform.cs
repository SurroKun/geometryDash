using UnityEngine;

public class SpeedBoostPlatform : MonoBehaviour
{
    [Header("Practice Respawn")]
    public bool boostedAfterRespawn = true;

    private void OnTriggerEnter(Collider other)
    {
        PlayerSpeedBoostBonus bonus = other.GetComponent<PlayerSpeedBoostBonus>();

        if (bonus != null)
            bonus.ToggleSpeedBoost();
    }

    public void ApplyRespawnState(PlayerSpeedBoostBonus bonus)
    {
        if (bonus == null)
            return;

        bonus.SetBoostState(boostedAfterRespawn);
    }
}