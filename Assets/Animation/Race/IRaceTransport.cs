using System;

public interface IRaceTransport
{
    event Action<RacePlayerSnapshot> SnapshotReceived;

    bool IsConnected { get; }

    void Connect();
    void Disconnect();
    void SendSnapshot(RacePlayerSnapshot snapshot);
}
