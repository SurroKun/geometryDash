using UnityEngine;

public class JumpHeightPlatform : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerJumpHeightBonus bonus = other.GetComponent<PlayerJumpHeightBonus>();

        if (bonus != null)
        {
            bonus.ToggleJumpHeightBonus();
        }
    }
}