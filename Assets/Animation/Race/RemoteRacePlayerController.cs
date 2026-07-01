using System.Collections.Generic;
using UnityEngine;

public class RemoteRacePlayerController : MonoBehaviour
{
    [Header("References")]
    public RaceModeManager race;
    public RaceTransportBehaviour transport;
    public GhostRunPlayback remotePlayback;

    [Header("Playback")]
    public bool autoResolveReferences = true;
    public bool connectTransportOnEnable = true;
    public bool applySnapshots = true;
    public bool applyOnlyWhenRaceRunning = true;
    public bool hideUntilRaceRunning = true;
    public bool clearSnapshotWhenRaceRestarts = true;
    public bool warnIfPlaybackLooksLocal = true;

    [Header("Smoothing")]
    public bool interpolateSnapshots = true;
    public float interpolationDelay = 0.12f;
    public float maxBufferedSeconds = 1.5f;
    public float snapDistance = 6f;

    private RacePlayerSnapshot latestSnapshot;
    private readonly List<RacePlayerSnapshot> snapshotBuffer = new List<RacePlayerSnapshot>();
    private bool hasSnapshot = false;

    public bool HasSnapshot => hasSnapshot;
    public RacePlayerSnapshot LatestSnapshot => latestSnapshot;

    void OnEnable()
    {
        ResolveReferences();
        Subscribe();
        SubscribeRace();

        if (connectTransportOnEnable && transport != null && !transport.IsConnected)
            transport.Connect();

        UpdateVisibilityForRaceState();
    }

    void OnDisable()
    {
        UnsubscribeRace();
        Unsubscribe();
    }

    void Update()
    {
        UpdateVisibilityForRaceState();

        if (!applySnapshots || !hasSnapshot || remotePlayback == null)
            return;

        if (applyOnlyWhenRaceRunning && race != null && !race.IsRunning)
            return;

        RacePlayerSnapshot snapshotToApply = GetSnapshotToApply();
        if (snapshotToApply != null)
            remotePlayback.ApplySnapshot(snapshotToApply);
    }

    public void Bind(RaceTransportBehaviour newTransport, GhostRunPlayback newPlayback)
    {
        Bind(race, newTransport, newPlayback);
    }

    public void Bind(
        RaceModeManager newRace,
        RaceTransportBehaviour newTransport,
        GhostRunPlayback newPlayback
    )
    {
        Unsubscribe();
        UnsubscribeRace();

        race = newRace;
        transport = newTransport;
        remotePlayback = newPlayback;

        Subscribe();
        SubscribeRace();
    }

    private void ResolveReferences()
    {
        if (!autoResolveReferences)
            return;

        if (transport == null)
            transport = FindFirstObjectByType<RaceTransportBehaviour>(FindObjectsInactive.Include);

        if (race == null)
            race = RaceModeManager.ActiveRace;

        if (race == null)
            race = FindFirstObjectByType<RaceModeManager>(FindObjectsInactive.Include);

        if (remotePlayback == null)
            remotePlayback = GetComponent<GhostRunPlayback>();

        if (remotePlayback == null)
            remotePlayback = FindFirstObjectByType<GhostRunPlayback>(FindObjectsInactive.Include);

        if (warnIfPlaybackLooksLocal &&
            remotePlayback != null &&
            remotePlayback.GetComponentInChildren<GhostRunRecorder>(true) != null)
        {
            Debug.LogWarning(
                "RemoteRacePlayerController playback points to an object with GhostRunRecorder. " +
                "Use a separate Ghost Player object for remote snapshots."
            );
        }
    }

    private void Subscribe()
    {
        if (transport != null)
            transport.SnapshotReceived += HandleSnapshotReceived;
    }

    private void Unsubscribe()
    {
        if (transport != null)
            transport.SnapshotReceived -= HandleSnapshotReceived;
    }

    private void SubscribeRace()
    {
        if (race != null)
            race.StateChanged += HandleRaceStateChanged;
    }

    private void UnsubscribeRace()
    {
        if (race != null)
            race.StateChanged -= HandleRaceStateChanged;
    }

    private void HandleSnapshotReceived(RacePlayerSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        if (applyOnlyWhenRaceRunning && race != null && !race.IsRunning)
            return;

        AddSnapshot(snapshot);
        hasSnapshot = true;
    }

    private void HandleRaceStateChanged(RaceModeManager.RaceState state)
    {
        if (clearSnapshotWhenRaceRestarts && state != RaceModeManager.RaceState.Running)
        {
            ClearSnapshotBuffer();
        }

        UpdateVisibilityForRaceState();
    }

    private void UpdateVisibilityForRaceState()
    {
        if (!hideUntilRaceRunning || remotePlayback == null || race == null)
            return;

        if (!race.IsRunning && !race.IsFinished)
            remotePlayback.SetVisible(false);
    }

    private void AddSnapshot(RacePlayerSnapshot snapshot)
    {
        RacePlayerSnapshot snapshotCopy = snapshot.Clone();

        if (latestSnapshot != null &&
            Vector3.Distance(latestSnapshot.position, snapshotCopy.position) > snapDistance)
        {
            ClearSnapshotBuffer();
        }

        latestSnapshot = snapshotCopy;

        int insertIndex = snapshotBuffer.Count;
        for (int i = 0; i < snapshotBuffer.Count; i++)
        {
            if (snapshotCopy.time < snapshotBuffer[i].time)
            {
                insertIndex = i;
                break;
            }
        }

        snapshotBuffer.Insert(insertIndex, snapshotCopy);
        TrimSnapshotBuffer();
    }

    private RacePlayerSnapshot GetSnapshotToApply()
    {
        if (!interpolateSnapshots || snapshotBuffer.Count < 2 || race == null)
            return latestSnapshot;

        float renderTime = race.RaceTimer - Mathf.Max(0f, interpolationDelay);

        if (renderTime <= snapshotBuffer[0].time)
            return snapshotBuffer[0];

        int lastIndex = snapshotBuffer.Count - 1;
        if (renderTime >= snapshotBuffer[lastIndex].time)
            return snapshotBuffer[lastIndex];

        for (int i = 0; i < lastIndex; i++)
        {
            RacePlayerSnapshot a = snapshotBuffer[i];
            RacePlayerSnapshot b = snapshotBuffer[i + 1];

            if (renderTime < a.time || renderTime > b.time)
                continue;

            float span = b.time - a.time;
            float t = span > 0f ? (renderTime - a.time) / span : 0f;
            return RacePlayerSnapshot.Lerp(a, b, t);
        }

        return latestSnapshot;
    }

    private void TrimSnapshotBuffer()
    {
        if (snapshotBuffer.Count == 0)
            return;

        float newestTime = snapshotBuffer[snapshotBuffer.Count - 1].time;
        float oldestAllowedTime = newestTime - Mathf.Max(0.1f, maxBufferedSeconds);

        while (snapshotBuffer.Count > 2 && snapshotBuffer[0].time < oldestAllowedTime)
            snapshotBuffer.RemoveAt(0);
    }

    private void ClearSnapshotBuffer()
    {
        snapshotBuffer.Clear();
        latestSnapshot = null;
        hasSnapshot = false;
    }
}
