using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class RaceNetworkLevelLoader
{
    private const string LoadLevelMessageName = "RaceLoadLevel";

    private static NetworkManager registeredManager;
    private static bool registered;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        RegisterIfPossible();
    }

    public static void RegisterIfPossible()
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || manager.CustomMessagingManager == null)
            return;

        if (registered && registeredManager == manager)
            return;

        Unregister();

        manager.CustomMessagingManager.RegisterNamedMessageHandler(
            LoadLevelMessageName,
            HandleLoadLevelMessage
        );

        registeredManager = manager;
        registered = true;
    }

    public static void Unregister()
    {
        if (!registered || registeredManager == null || registeredManager.CustomMessagingManager == null)
            return;

        registeredManager.CustomMessagingManager.UnregisterNamedMessageHandler(LoadLevelMessageName);
        registeredManager = null;
        registered = false;
    }

    public static void SendLoadLevelToClients(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        RegisterIfPossible();

        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null || !manager.IsServer || manager.CustomMessagingManager == null)
            return;

        FixedString128Bytes sceneNameValue = sceneName;
        using FastBufferWriter writer = new FastBufferWriter(160, Allocator.Temp);
        writer.WriteValueSafe(sceneNameValue);

        foreach (ulong clientId in manager.ConnectedClientsIds)
        {
            if (clientId == NetworkManager.ServerClientId)
                continue;

            manager.CustomMessagingManager.SendNamedMessage(
                LoadLevelMessageName,
                clientId,
                writer,
                NetworkDelivery.Reliable
            );
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RegisterIfPossible();
    }

    private static void HandleLoadLevelMessage(ulong senderClientId, FastBufferReader reader)
    {
        NetworkManager manager = NetworkManager.Singleton;
        if (manager == null ||
            manager.IsServer ||
            senderClientId != NetworkManager.ServerClientId)
        {
            return;
        }

        reader.ReadValueSafe(out FixedString128Bytes sceneNameValue);
        string sceneName = sceneNameValue.ToString();

        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        Time.timeScale = 1f;
        RaceMultiplayerBootstrap.ArmJoinMode();
        SceneManager.LoadScene(sceneName);
    }
}
