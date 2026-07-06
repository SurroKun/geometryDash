using UnityEngine;

public class JumpHeightPlatform : MonoBehaviour
{
    [Header("Practice Respawn")]
    public bool boostedAfterRespawn = true;

    private void OnTriggerEnter(Collider other)
    {
        PlayerJumpHeightBonus bonus = other.GetComponent<PlayerJumpHeightBonus>();

        if (bonus != null)
            bonus.ToggleJumpHeightBonus();
    }

    public void ApplyRespawnState(PlayerJumpHeightBonus bonus)
    {
        if (bonus == null)
            return;

        bonus.SetBoostState(boostedAfterRespawn);
    }
}