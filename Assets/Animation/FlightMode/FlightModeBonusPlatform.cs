using UnityEngine;

public class FlightModeBonusPlatform : MonoBehaviour
{
    public enum FlightPlatformMode
    {
        Toggle,
        ForceOn,
        ForceOff
    }

    [Header("Platform Mode")]
    public FlightPlatformMode mode = FlightPlatformMode.Toggle;

    [Header("Settings")]
    public bool oneUse = false;
    public float reTriggerCooldown = 0.25f;

    [Header("Practice Respawn")]
    public bool flightAfterRespawn = true;

    private float lastTriggerTime = -999f;
    private bool wasUsed = false;

    private void OnTriggerEnter(Collider other)
    {
        if (oneUse && wasUsed)
            return;

        if (Time.time - lastTriggerTime < reTriggerCooldown)
            return;

        PlayerMove playerMove = other.GetComponent<PlayerMove>();

        if (playerMove == null)
            playerMove = other.GetComponentInParent<PlayerMove>();

        if (playerMove == null)
            return;

        bool wasFlying = playerMove.IsFlightModeActive();

        lastTriggerTime = Time.time;
        wasUsed = true;

        if (mode == FlightPlatformMode.Toggle)
        {
            playerMove.ToggleFlightMode();
        }
        else if (mode == FlightPlatformMode.ForceOn)
        {
            if (!playerMove.IsFlightModeActive())
                playerMove.ToggleFlightMode();
        }
        else if (mode == FlightPlatformMode.ForceOff)
        {
            if (playerMove.IsFlightModeActive())
                playerMove.ToggleFlightMode();
        }

        bool isFlying = playerMove.IsFlightModeActive();

        if (wasFlying != isFlying)
            PlayPlayerFlightEffect(other);

        Debug.Log("Flight platform triggered. Flight mode: " + playerMove.IsFlightModeActive());
    }

    public void ApplyRespawnState(PlayerMove playerMove)
    {
        if (playerMove == null)
            return;

        bool isFlying = playerMove.IsFlightModeActive();

        if (flightAfterRespawn && !isFlying)
            playerMove.ToggleFlightMode();

        if (!flightAfterRespawn && isFlying)
            playerMove.ToggleFlightMode();
    }

    private void PlayPlayerFlightEffect(Collider other)
    {
        PlayerBonusEffects effects = other.GetComponent<PlayerBonusEffects>();

        if (effects == null)
            effects = other.GetComponentInParent<PlayerBonusEffects>();

        if (effects == null)
            return;

        effects.PlayFlightEffect();
    }
}