using UnityEngine;
using UnityEngine.SceneManagement;

public static class RaceMultiplayerBootstrap
{
    public const string MultiplayerModeKey = "RaceMultiplayerMode";
    public const string MultiplayerLaunchArmedKey = "RaceMultiplayerLaunchArmed";

    private const int ModeOff = 0;
    private const int ModeHost = 1;
    private const int ModeJoin = 2;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    public static bool IsMultiplayerRequested()
    {
        return PlayerPrefs.GetInt(MultiplayerModeKey, ModeOff) != ModeOff &&
               PlayerPrefs.GetInt(MultiplayerLaunchArmedKey, 0) == 1;
    }

    public static bool IsHostRequested()
    {
        return IsMultiplayerRequested() &&
               PlayerPrefs.GetInt(MultiplayerModeKey, ModeOff) == ModeHost;
    }

    public static bool IsJoinRequested()
    {
        return IsMultiplayerRequested() &&
               PlayerPrefs.GetInt(MultiplayerModeKey, ModeOff) == ModeJoin;
    }

    public static void ArmHostMode()
    {
        ArmMode(ModeHost);
    }

    public static void ArmJoinMode()
    {
        ArmMode(ModeJoin);
    }

    public static void ClearMode()
    {
        PlayerPrefs.SetInt(MultiplayerModeKey, ModeOff);
        PlayerPrefs.SetInt(MultiplayerLaunchArmedKey, 0);
        PlayerPrefs.Save();
    }

    private static void ArmMode(int mode)
    {
        PlayerPrefs.SetInt(MultiplayerModeKey, mode);
        PlayerPrefs.SetInt(MultiplayerLaunchArmedKey, 1);
        PlayerPrefs.Save();
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsMultiplayerRequested())
            return;

        if (IsMenuScene(scene.name))
            return;

        BootstrapRaceScene();
        ClearMode();
    }

    private static bool IsMenuScene(string sceneName)
    {
        return sceneName == "MainMenu" ||
               sceneName == "LevelSelect" ||
               sceneName == "SkinMenu";
    }

    private static void BootstrapRaceScene()
    {
        PlayerMove player = FindLocalPlayer();
        if (player == null)
        {
            Debug.LogWarning("Multiplayer bootstrap could not find PlayerMove.");
            return;
        }

        bool onlineMode = RaceOnlineSessionManager.IsOnlineRequested();

        if (onlineMode)
            RaceOnlineSessionManager.StartRequestedSessionIfNeeded();

        PlayerSkinSwitcher playerSkinSwitcher = player.GetComponentInChildren<PlayerSkinSwitcher>(true);
        GhostRunRecorder recorder = EnsureRecorder(player);
        GhostRunPlayback playback = FindOrCreateGhostPlayback(playerSkinSwitcher);
        RaceTransportBehaviour transport = onlineMode ? FindOrCreateNetworkTransport() : null;
        RaceModeManager race = FindOrCreateRaceManager();

        race.localPlayerMove = player;
        race.localDeathScript = player.GetComponent<DeathScript>();
        race.practiceModeManager = player.GetComponent<PracticeModeManager>();
        race.localRecorder = recorder;
        race.ghostPlayback = playback;

        race.startRaceOnSceneStart = !onlineMode;
        race.enablePracticeRespawn = true;
        race.freezePlayerDuringCountdown = true;
        race.disableDeathChecksDuringCountdown = true;
        race.autoCreateRaceUI = true;
        race.controlRecorderTiming = true;
        race.controlGhostTiming = !onlineMode;
        race.saveRunOnFinish = true;
        race.enableSnapshotTransport = onlineMode;
        race.connectTransportOnStart = onlineMode;
        race.waitForRemoteResult = true;
        race.SetTransport(transport, onlineMode);

        if (onlineMode)
        {
            FindOrCreateRemoteController(race, transport, playback);
            FindOrCreateStartCoordinator(race);
        }

        DeathMenuUI.PracticeModeActive = true;
    }

    private static PlayerMove FindLocalPlayer()
    {
        PlayerMove[] players = Object.FindObjectsByType<PlayerMove>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null && players[i].CompareTag("Player"))
                return players[i];
        }

        return players.Length > 0 ? players[0] : null;
    }

    private static GhostRunRecorder EnsureRecorder(PlayerMove player)
    {
        GhostRunRecorder recorder = player.GetComponent<GhostRunRecorder>();
        if (recorder == null)
            recorder = player.gameObject.AddComponent<GhostRunRecorder>();

        recorder.recordOnStart = false;
        recorder.saveOnDisable = true;

        return recorder;
    }

    private static GhostRunPlayback FindOrCreateGhostPlayback(PlayerSkinSwitcher sourceSkinSwitcher)
    {
        GhostRunPlayback playback = Object.FindFirstObjectByType<GhostRunPlayback>(
            FindObjectsInactive.Include
        );

        if (playback != null)
        {
            EnsureGhostSkinSwitcher(playback, sourceSkinSwitcher);
            return playback;
        }

        GameObject ghost = new GameObject("Ghost Player");
        ghost.name = "Ghost Player";
        ghost.tag = "Untagged";
        ghost.transform.localScale = Vector3.one * 1.6f;

        playback = ghost.AddComponent<GhostRunPlayback>();
        playback.ghostRoot = ghost.transform;
        playback.playSavedRunOnStart = false;
        playback.hideWhenNoRun = true;
        playback.sanitizeGhostOnStart = true;

        EnsureGhostSkinSwitcher(playback, sourceSkinSwitcher);

        return playback;
    }

    private static void EnsureGhostSkinSwitcher(
        GhostRunPlayback playback,
        PlayerSkinSwitcher sourceSkinSwitcher
    )
    {
        if (playback == null || sourceSkinSwitcher == null)
            return;

        PlayerSkinSwitcher ghostSkinSwitcher = playback.skinSwitcher;
        if (ghostSkinSwitcher == null && playback.ghostRoot != null)
        {
            ghostSkinSwitcher = playback.ghostRoot.GetComponentInChildren<PlayerSkinSwitcher>(true);
        }

        if (ghostSkinSwitcher == null || !HasUsableSkins(ghostSkinSwitcher))
            ghostSkinSwitcher = CreateGhostSkinSwitcher(playback, sourceSkinSwitcher);

        if (ghostSkinSwitcher == null)
            return;

        ghostSkinSwitcher.applyOnStart = false;
        playback.skinSwitcher = ghostSkinSwitcher;
    }

    private static bool HasUsableSkins(PlayerSkinSwitcher skinSwitcher)
    {
        if (skinSwitcher == null || skinSwitcher.skins == null || skinSwitcher.skins.Length == 0)
            return false;

        for (int i = 0; i < skinSwitcher.skins.Length; i++)
        {
            if (skinSwitcher.skins[i] != null)
                return true;
        }

        return false;
    }

    private static PlayerSkinSwitcher CreateGhostSkinSwitcher(
        GhostRunPlayback playback,
        PlayerSkinSwitcher sourceSkinSwitcher
    )
    {
        if (playback.ghostRoot == null ||
            sourceSkinSwitcher.skins == null ||
            sourceSkinSwitcher.skins.Length == 0)
        {
            return null;
        }

        GameObject visualRoot = new GameObject("Ghost Skin Visuals");
        visualRoot.transform.SetParent(playback.ghostRoot, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one;

        PlayerSkinSwitcher ghostSkinSwitcher = visualRoot.AddComponent<PlayerSkinSwitcher>();
        ghostSkinSwitcher.applyOnStart = false;
        ghostSkinSwitcher.skins = new GameObject[sourceSkinSwitcher.skins.Length];

        for (int i = 0; i < sourceSkinSwitcher.skins.Length; i++)
        {
            GameObject sourceSkin = sourceSkinSwitcher.skins[i];
            if (sourceSkin == null)
                continue;

            GameObject ghostSkin = Object.Instantiate(sourceSkin, visualRoot.transform, false);
            ghostSkin.name = sourceSkin.name + " Ghost";
            ghostSkin.SetActive(false);
            ghostSkinSwitcher.skins[i] = ghostSkin;
        }

        return ghostSkinSwitcher;
    }

    private static RaceModeManager FindOrCreateRaceManager()
    {
        RaceModeManager race = Object.FindFirstObjectByType<RaceModeManager>(
            FindObjectsInactive.Include
        );

        if (race != null)
            return race;

        GameObject raceObject = new GameObject("RaceModeManager");
        return raceObject.AddComponent<RaceModeManager>();
    }

    private static RaceTransportBehaviour FindOrCreateNetworkTransport()
    {
        NetworkRaceTransport transport = Object.FindFirstObjectByType<NetworkRaceTransport>(
            FindObjectsInactive.Include
        );

        if (transport != null)
            return transport;

        GameObject transportObject = new GameObject("NetworkRaceTransport");
        transport = transportObject.AddComponent<NetworkRaceTransport>();
        transport.startRequestedSessionOnConnect = true;
        transport.registerOnEnable = true;

        return transport;
    }

    private static RemoteRacePlayerController FindOrCreateRemoteController(
        RaceModeManager race,
        RaceTransportBehaviour transport,
        GhostRunPlayback playback
    )
    {
        RemoteRacePlayerController controller = Object.FindFirstObjectByType<RemoteRacePlayerController>(
            FindObjectsInactive.Include
        );

        if (controller == null)
        {
            GameObject controllerObject = new GameObject("RemoteRacePlayerController");
            controller = controllerObject.AddComponent<RemoteRacePlayerController>();
        }

        controller.race = race;
        controller.transport = transport;
        controller.remotePlayback = playback;
        controller.connectTransportOnEnable = true;
        controller.applySnapshots = true;
        controller.applyOnlyWhenRaceRunning = true;
        controller.hideUntilRaceRunning = true;
        controller.Bind(race, transport, playback);

        return controller;
    }

    private static RaceNetworkStartCoordinator FindOrCreateStartCoordinator(RaceModeManager race)
    {
        RaceNetworkStartCoordinator coordinator = Object.FindFirstObjectByType<RaceNetworkStartCoordinator>(
            FindObjectsInactive.Include
        );

        if (coordinator == null)
        {
            GameObject coordinatorObject = new GameObject("RaceNetworkStartCoordinator");
            coordinator = coordinatorObject.AddComponent<RaceNetworkStartCoordinator>();
        }

        coordinator.minimumPlayers = 2;
        coordinator.startLeadTime = 1.0;
        coordinator.startOnlyFromServer = true;
        coordinator.Bind(race);

        return coordinator;
    }
}
