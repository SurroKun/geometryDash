using UnityEngine;

public class SpeedDashZone : MonoBehaviour
{
    [Header("Dash Settings")]
    public float dashSpeedMultiplier = 2.2f;
    public float dashDuration = 0.35f;

    [Header("Vertical Freeze")]
    public float verticalFreezeDuration = 0.35f;

    [Header("Settings")]
    public bool oneUsePerEnter = true;
}