using System;
using UnityEngine;

public abstract class RaceTransportBehaviour : MonoBehaviour, IRaceTransport
{
    public event Action<RacePlayerSnapshot> SnapshotReceived;

    public abstract bool IsConnected { get; }

    public abstract void Connect();
    public abstract void Disconnect();
    public abstract void SendSnapshot(RacePlayerSnapshot snapshot);

    protected void RaiseSnapshotReceived(RacePlayerSnapshot snapshot)
    {
        SnapshotReceived?.Invoke(snapshot);
    }
}
