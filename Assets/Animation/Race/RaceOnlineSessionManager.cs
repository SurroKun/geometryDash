using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public static class RaceOnlineSessionManager
{
    public const string OnlineModeKey = "RaceOnlineMode";
    public const string OnlineConnectionModeKey = "RaceOnlineConnectionMode";
    public const string OnlineAddressKey = "RaceOnlineAddress";
    public const string OnlinePortKey = "RaceOnlinePort";
    public const string OnlineRelayJoinCodeKey = "RaceOnlineRelayJoinCode";

    private const int ModeOff = 0;
    private const int ModeHost = 1;
    private const int ModeClient = 2;
    private const int ConnectionDirect = 0;
    private const int ConnectionRelay = 1;
    private const string NetworkManagerObjectName = "Race Network Manager";
    private const string RelayConnectionType = "dtls";
    private const ushort DefaultPort = 7777;
    private const int MaxRelayConnections = 1;

    public static string LastRelayJoinCode { get; private set; } = "";
    public static string LastError { get; private set; } = "";

    public static bool IsOnlineRequested()
    {
        return PlayerPrefs.GetInt(OnlineModeKey, ModeOff) != ModeOff;
    }

    public static bool IsHostRequested()
    {
        return PlayerPrefs.GetInt(OnlineModeKey, ModeOff) == ModeHost;
    }

    public static bool IsClientRequested()
    {
        return PlayerPrefs.GetInt(OnlineModeKey, ModeOff) == ModeClient;
    }

    public static bool IsRelayRequested()
    {
        return PlayerPrefs.GetInt(OnlineConnectionModeKey, ConnectionDirect) == ConnectionRelay;
    }

    public static void ArmHost()
    {
        PlayerPrefs.SetInt(OnlineModeKey, ModeHost);
        PlayerPrefs.SetInt(OnlineConnectionModeKey, ConnectionDirect);
        PlayerPrefs.Save();
    }

    public static void ArmClient(string address)
    {
        PlayerPrefs.SetInt(OnlineModeKey, ModeClient);
        PlayerPrefs.SetInt(OnlineConnectionModeKey, ConnectionDirect);
        PlayerPrefs.SetString(OnlineAddressKey, string.IsNullOrWhiteSpace(address) ? "127.0.0.1" : address);
        PlayerPrefs.Save();
    }

    public static void ArmRelayHost()
    {
        PlayerPrefs.SetInt(OnlineModeKey, ModeHost);
        PlayerPrefs.SetInt(OnlineConnectionModeKey, ConnectionRelay);
        PlayerPrefs.Save();
    }

    public static void ArmRelayClient(string joinCode)
    {
        PlayerPrefs.SetInt(OnlineModeKey, ModeClient);
        PlayerPrefs.SetInt(OnlineConnectionModeKey, ConnectionRelay);
        PlayerPrefs.SetString(OnlineRelayJoinCodeKey, NormalizeJoinCode(joinCode));
        PlayerPrefs.Save();
    }

    public static void ClearMode()
    {
        PlayerPrefs.SetInt(OnlineModeKey, ModeOff);
        PlayerPrefs.SetInt(OnlineConnectionModeKey, ConnectionDirect);
        PlayerPrefs.Save();
    }

    public static NetworkManager EnsureNetworkManager()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null)
            return manager;

        GameObject managerObject = new GameObject(NetworkManagerObjectName);
        UnityEngine.Object.DontDestroyOnLoad(managerObject);

        UnityTransport transport = managerObject.AddComponent<UnityTransport>();
        manager = managerObject.AddComponent<NetworkManager>();

        NetworkConfig config = new NetworkConfig
        {
            NetworkTransport = transport,
            EnableSceneManagement = false
        };

        manager.NetworkConfig = config;
        return manager;
    }

    public static void StartRequestedSessionIfNeeded()
    {
        if (!IsOnlineRequested())
            return;

        if (IsRelayRequested())
        {
            if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
                _ = StartRequestedRelaySessionAsync();

            return;
        }

        NetworkManager manager = EnsureNetworkManager();
        if (manager == null || manager.IsListening)
            return;

        UnityTransport transport = manager.GetComponent<UnityTransport>();
        if (transport == null)
            transport = manager.gameObject.AddComponent<UnityTransport>();

        ushort port = (ushort)Mathf.Clamp(PlayerPrefs.GetInt(OnlinePortKey, DefaultPort), 1, ushort.MaxValue);
        string address = PlayerPrefs.GetString(OnlineAddressKey, "127.0.0.1");

        if (IsHostRequested())
        {
            transport.SetConnectionData("0.0.0.0", port);
            manager.StartHost();
            RaceNetworkLevelLoader.RegisterIfPossible();
            return;
        }

        if (IsClientRequested())
        {
            transport.SetConnectionData(address, port);
            manager.StartClient();
            RaceNetworkLevelLoader.RegisterIfPossible();
        }
    }

    public static async Task<string> StartRelayHostAsync()
    {
        LastError = "";
        LastRelayJoinCode = "";

        try
        {
            ArmRelayHost();
            NetworkManager manager = EnsureNetworkManager();
            if (manager == null)
                throw new InvalidOperationException("NetworkManager is not available.");

            if (manager.IsListening)
                manager.Shutdown();

            await EnsureUnityServicesSignedInAsync();

            UnityTransport transport = EnsureUnityTransport(manager);
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(MaxRelayConnections);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, RelayConnectionType));

            if (!manager.StartHost())
                throw new InvalidOperationException("StartHost failed.");

            RaceNetworkLevelLoader.RegisterIfPossible();

            LastRelayJoinCode = joinCode;
            PlayerPrefs.SetString(OnlineRelayJoinCodeKey, joinCode);
            PlayerPrefs.Save();

            return joinCode;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Debug.LogError("Relay host failed: " + exception);
            Shutdown();
            return null;
        }
    }

    public static async Task<bool> StartRelayClientAsync(string joinCode)
    {
        LastError = "";
        LastRelayJoinCode = NormalizeJoinCode(joinCode);

        try
        {
            if (string.IsNullOrWhiteSpace(LastRelayJoinCode))
                throw new ArgumentException("Relay join code is empty.");

            ArmRelayClient(LastRelayJoinCode);

            NetworkManager manager = EnsureNetworkManager();
            if (manager == null)
                throw new InvalidOperationException("NetworkManager is not available.");

            if (manager.IsListening)
                manager.Shutdown();

            await EnsureUnityServicesSignedInAsync();

            UnityTransport transport = EnsureUnityTransport(manager);
            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(LastRelayJoinCode);
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, RelayConnectionType));

            if (!manager.StartClient())
                throw new InvalidOperationException("StartClient failed.");

            RaceNetworkLevelLoader.RegisterIfPossible();

            return true;
        }
        catch (Exception exception)
        {
            LastError = exception.Message;
            Debug.LogError("Relay client failed: " + exception);
            Shutdown();
            return false;
        }
    }

    public static void Shutdown()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager != null && manager.IsListening)
            manager.Shutdown();

        RaceNetworkLevelLoader.Unregister();
        ClearMode();
    }

    private static async Task StartRequestedRelaySessionAsync()
    {
        if (IsHostRequested())
        {
            await StartRelayHostAsync();
            return;
        }

        if (IsClientRequested())
            await StartRelayClientAsync(PlayerPrefs.GetString(OnlineRelayJoinCodeKey, ""));
    }

    private static async Task EnsureUnityServicesSignedInAsync()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
            await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private static UnityTransport EnsureUnityTransport(NetworkManager manager)
    {
        UnityTransport transport = manager.GetComponent<UnityTransport>();
        if (transport == null)
            transport = manager.gameObject.AddComponent<UnityTransport>();

        return transport;
    }

    private static string NormalizeJoinCode(string joinCode)
    {
        return string.IsNullOrWhiteSpace(joinCode)
            ? ""
            : joinCode.Trim().ToUpperInvariant();
    }
}
