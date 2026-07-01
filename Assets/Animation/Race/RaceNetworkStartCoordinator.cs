using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class RaceNetworkStartCoordinator : MonoBehaviour
{
    private const string StartMessageName = "RaceStartTime";

    [Header("References")]
    public RaceModeManager race;

    [Header("Start")]
    public int minimumPlayers = 2;
    public double startLeadTime = 1.0;
    public bool startOnlyFromServer = true;

    private NetworkManager networkManager;
    private bool registered;
    private bool startScheduled;
    private bool countdownStarted;
    private double scheduledStartTime;

    void OnEnable()
    {
        ResolveReferences();
        Register();
    }

    void OnDisable()
    {
        Unregister();
    }

    void Update()
    {
        ResolveReferences();

        if (race == null || countdownStarted || networkManager == null || !networkManager.IsListening)
            return;

        if (!startScheduled && CanScheduleStart())
            ScheduleAndBroadcastStart();

        if (startScheduled && networkManager.ServerTime.Time >= scheduledStartTime)
            StartRaceCountdown();
    }

    public void Bind(RaceModeManager raceManager)
    {
        race = raceManager;
    }

    private void ResolveReferences()
    {
        if (race == null)
            race = RaceModeManager.ActiveRace;

        if (race == null)
            race = FindFirstObjectByType<RaceModeManager>(FindObjectsInactive.Include);

        if (networkManager == null)
            networkManager = NetworkManager.Singleton;
    }

    private void Register()
    {
        ResolveReferences();
        if (registered || networkManager == null || networkManager.CustomMessagingManager == null)
            return;

        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(
            StartMessageName,
            HandleStartMessage
        );

        registered = true;
    }

    private void Unregister()
    {
        if (!registered || networkManager == null || networkManager.CustomMessagingManager == null)
            return;

        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(StartMessageName);
        registered = false;
    }

    private bool CanScheduleStart()
    {
        if (startOnlyFromServer && !networkManager.IsServer)
            return false;

        return networkManager.ConnectedClientsIds.Count >= Mathf.Max(1, minimumPlayers);
    }

    private void ScheduleAndBroadcastStart()
    {
        scheduledStartTime = networkManager.ServerTime.Time + Mathf.Max(0.1f, (float)startLeadTime);
        startScheduled = true;

        using FastBufferWriter writer = new FastBufferWriter(sizeof(double), Allocator.Temp);
        writer.WriteValueSafe(scheduledStartTime);

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.ServerClientId)
                continue;

            networkManager.CustomMessagingManager.SendNamedMessage(
                StartMessageName,
                clientId,
                writer,
                NetworkDelivery.Reliable
            );
        }
    }

    private void HandleStartMessage(ulong senderClientId, FastBufferReader reader)
    {
        if (networkManager != null &&
            !networkManager.IsServer &&
            senderClientId != NetworkManager.ServerClientId)
        {
            return;
        }

        reader.ReadValueSafe(out double startTime);
        scheduledStartTime = startTime;
        startScheduled = true;
    }

    private void StartRaceCountdown()
    {
        countdownStarted = true;

        if (race != null && race.State == RaceModeManager.RaceState.Waiting)
            race.StartCountdown();
    }
}
