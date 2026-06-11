using UnityEngine;

public class SpeedBoostPlatform : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        PlayerSpeedBoostBonus bonus = other.GetComponent<PlayerSpeedBoostBonus>();

        if (bonus != null)
        {
            bonus.ToggleSpeedBoost();
        }
    }
}