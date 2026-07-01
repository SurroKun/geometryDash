using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class NetworkRaceTransport : RaceTransportBehaviour
{
    private const string SnapshotMessageName = "RaceSnapshot";

    [Header("Netcode")]
    public bool startRequestedSessionOnConnect = true;
    public bool registerOnEnable = true;
    public bool forwardClientSnapshots = true;

    private NetworkManager networkManager;
    private bool registered;

    public override bool IsConnected
    {
        get
        {
            ResolveNetworkManager();
            return networkManager != null &&
                   networkManager.IsListening &&
                   (networkManager.IsHost || networkManager.IsClient || networkManager.IsServer);
        }
    }

    void OnEnable()
    {
        if (registerOnEnable)
            Register();
    }

    void OnDisable()
    {
        Unregister();
    }

    public override void Connect()
    {
        ResolveNetworkManager();

        if (startRequestedSessionOnConnect)
            RaceOnlineSessionManager.StartRequestedSessionIfNeeded();

        Register();
    }

    public override void Disconnect()
    {
        Unregister();
    }

    public override void SendSnapshot(RacePlayerSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        ResolveNetworkManager();
        if (networkManager == null || !networkManager.IsListening)
            return;

        using FastBufferWriter writer = CreateWriter(snapshot);

        if (networkManager.IsServer)
        {
            SendToAllClients(writer);
            return;
        }

        networkManager.CustomMessagingManager.SendNamedMessage(
            SnapshotMessageName,
            NetworkManager.ServerClientId,
            writer,
            NetworkDelivery.Unreliable
        );
    }

    private void Register()
    {
        ResolveNetworkManager();
        if (registered || networkManager == null || networkManager.CustomMessagingManager == null)
            return;

        networkManager.CustomMessagingManager.RegisterNamedMessageHandler(
            SnapshotMessageName,
            HandleSnapshotMessage
        );

        registered = true;
    }

    private void Unregister()
    {
        if (!registered || networkManager == null || networkManager.CustomMessagingManager == null)
            return;

        networkManager.CustomMessagingManager.UnregisterNamedMessageHandler(SnapshotMessageName);
        registered = false;
    }

    private void HandleSnapshotMessage(ulong senderClientId, FastBufferReader reader)
    {
        RacePlayerSnapshot snapshot = ReadSnapshot(reader);
        if (snapshot == null)
            return;

        RaiseSnapshotReceived(snapshot);

        if (!forwardClientSnapshots ||
            networkManager == null ||
            !networkManager.IsServer ||
            senderClientId == NetworkManager.ServerClientId)
        {
            return;
        }

        using FastBufferWriter writer = CreateWriter(snapshot);
        SendToAllClients(writer, senderClientId);
    }

    private void SendToAllClients(FastBufferWriter writer, ulong excludedClientId = ulong.MaxValue)
    {
        if (networkManager == null || networkManager.CustomMessagingManager == null)
            return;

        foreach (ulong clientId in networkManager.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.ServerClientId || clientId == excludedClientId)
                continue;

            networkManager.CustomMessagingManager.SendNamedMessage(
                SnapshotMessageName,
                clientId,
                writer,
                NetworkDelivery.Unreliable
            );
        }
    }

    private void ResolveNetworkManager()
    {
        if (networkManager == null)
            networkManager = NetworkManager.Singleton;
    }

    private static FastBufferWriter CreateWriter(RacePlayerSnapshot snapshot)
    {
        FastBufferWriter writer = new FastBufferWriter(128, Allocator.Temp);
        writer.WriteValueSafe(snapshot.time);
        writer.WriteValueSafe(snapshot.position);
        writer.WriteValueSafe(snapshot.rotation);
        writer.WriteValueSafe(snapshot.alive);
        writer.WriteValueSafe(snapshot.finished);
        writer.WriteValueSafe(snapshot.skinIndex);
        return writer;
    }

    private static RacePlayerSnapshot ReadSnapshot(FastBufferReader reader)
    {
        reader.ReadValueSafe(out float time);
        reader.ReadValueSafe(out Vector3 position);
        reader.ReadValueSafe(out Quaternion rotation);
        reader.ReadValueSafe(out bool alive);
        reader.ReadValueSafe(out bool finished);
        reader.ReadValueSafe(out int skinIndex);

        return new RacePlayerSnapshot(time, position, rotation, alive, finished, skinIndex);
    }
}
