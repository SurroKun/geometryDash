using UnityEngine;

public class FlightModeBonusPlatform : MonoBehaviour
{
    [Header("Settings")]
    public bool oneUse = false;
    public float reTriggerCooldown = 0.25f;

    private float lastTriggerTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        if (Time.time - lastTriggerTime < reTriggerCooldown)
            return;

        PlayerMove playerMove = other.GetComponent<PlayerMove>();

        if (playerMove == null)
            playerMove = other.GetComponentInParent<PlayerMove>();

        if (playerMove == null)
            return;

        lastTriggerTime = Time.time;

        playerMove.ToggleFlightMode();

        Debug.Log("Flight platform triggered. Flight mode: " + playerMove.IsFlightModeActive());
    }
}