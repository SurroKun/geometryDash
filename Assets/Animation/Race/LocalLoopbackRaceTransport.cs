using System.Collections.Generic;
using UnityEngine;

public class LocalLoopbackRaceTransport : RaceTransportBehaviour
{
    public enum LoopbackMode
    {
        SavedRunReplay,
        EchoLocalSnapshots
    }

    private class QueuedSnapshot
    {
        public float deliverAt;
        public RacePlayerSnapshot snapshot;
    }

    [Header("Loopback")]
    public bool connectOnStart = true;
    public LoopbackMode mode = LoopbackMode.SavedRunReplay;
    public float simulatedLatency = 0.08f;
    public Vector3 receivedPositionOffset = new Vector3(1.4f, 0f, 0f);
    public bool hideWhenNoSavedRun = true;

    private readonly List<QueuedSnapshot> queue = new List<QueuedSnapshot>();
    private GhostRunData savedRun;
    private bool connected = false;

    public override bool IsConnected => connected;

    void Start()
    {
        if (connectOnStart)
            Connect();
    }

    void Update()
    {
        if (!connected || queue.Count == 0)
            return;

        float now = Time.time;

        for (int i = queue.Count - 1; i >= 0; i--)
        {
            QueuedSnapshot queued = queue[i];
            if (queued.deliverAt > now)
                continue;

            queue.RemoveAt(i);
            RaiseSnapshotReceived(queued.snapshot);
        }
    }

    public override void Connect()
    {
        connected = true;
        savedRun = GhostRunStorage.LoadForCurrentScene();
    }

    public override void Disconnect()
    {
        connected = false;
        queue.Clear();
    }

    public void ClearQueue()
    {
        queue.Clear();
    }

    public override void SendSnapshot(RacePlayerSnapshot snapshot)
    {
        if (!connected || snapshot == null)
            return;

        RacePlayerSnapshot remoteSnapshot = CreateRemoteSnapshot(snapshot);
        if (remoteSnapshot == null)
            return;

        remoteSnapshot.position += receivedPositionOffset;

        queue.Add(
            new QueuedSnapshot
            {
                deliverAt = Time.time + Mathf.Max(0f, simulatedLatency),
                snapshot = remoteSnapshot
            }
        );
    }

    private RacePlayerSnapshot CreateRemoteSnapshot(RacePlayerSnapshot localSnapshot)
    {
        if (mode == LoopbackMode.EchoLocalSnapshots)
            return localSnapshot.Clone();

        if (savedRun == null || !savedRun.HasFrames())
            return hideWhenNoSavedRun ? null : localSnapshot.Clone();

        return SampleSavedRun(localSnapshot.time);
    }

    private RacePlayerSnapshot SampleSavedRun(float time)
    {
        if (savedRun == null || !savedRun.HasFrames())
            return null;

        if (time <= savedRun.frames[0].time)
            return RacePlayerSnapshot.FromGhostFrame(savedRun.frames[0]);

        int lastIndex = savedRun.frames.Count - 1;
        if (time >= savedRun.frames[lastIndex].time)
        {
            RacePlayerSnapshot lastSnapshot = RacePlayerSnapshot.FromGhostFrame(savedRun.frames[lastIndex]);
            if (lastSnapshot != null)
                lastSnapshot.finished = true;

            return lastSnapshot;
        }

        for (int i = 0; i < lastIndex; i++)
        {
            GhostRunFrame a = savedRun.frames[i];
            GhostRunFrame b = savedRun.frames[i + 1];

            if (time < a.time || time > b.time)
                continue;

            float span = b.time - a.time;
            float t = span > 0f ? (time - a.time) / span : 0f;

            return new RacePlayerSnapshot(
                time,
                Vector3.Lerp(a.position, b.position, t),
                Quaternion.Slerp(a.rotation, b.rotation, t),
                a.alive || b.alive,
                false,
                t < 0.5f ? a.skinIndex : b.skinIndex
            );
        }

        return RacePlayerSnapshot.FromGhostFrame(savedRun.frames[lastIndex]);
    }
}
