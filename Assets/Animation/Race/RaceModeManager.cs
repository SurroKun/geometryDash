using System.Collections;
using System;
using TMPro;
using UnityEngine;

public class RaceModeManager : MonoBehaviour
{
    public enum RaceState
    {
        Waiting,
        Countdown,
        Running,
        Finished
    }

    [Header("References")]
    public PlayerMove localPlayerMove;
    public DeathScript localDeathScript;
    public PracticeModeManager practiceModeManager;
    public GhostRunRecorder localRecorder;
    public GhostRunPlayback ghostPlayback;
    public RaceTransportBehaviour transport;
    public GroundTrailFromPoints localTrail;
    public SkinRollVisualController localRollVisualController;

    [Header("Optional UI")]
    public TMP_Text statusText;
    public TMP_Text timerText;
    public RaceUIController raceUI;

    [Header("Race Settings")]
    public bool startRaceOnSceneStart = true;
    public bool enablePracticeRespawn = true;
    public bool freezePlayerDuringCountdown = true;
    public bool disableDeathChecksDuringCountdown = true;
    public bool autoCreateRaceUI = true;
    public bool drawDebugOverlay = false;
    public bool controlRecorderTiming = true;
    public bool controlGhostTiming = true;
    public bool saveRunOnFinish = true;
    public bool waitForRemoteResult = true;
    public float countdownSeconds = 3f;

    [Header("Transport")]
    public bool enableSnapshotTransport = false;
    public bool connectTransportOnStart = true;
    public float transportSendInterval = 0.05f;

    public static RaceModeManager ActiveRace { get; private set; }

    private RaceState state = RaceState.Waiting;
    private float raceTimer = 0f;
    private float finishTime = 0f;
    private float countdownRemaining = 0f;
    private float transportSendTimer = 0f;
    private string statusMessage = "";
    private RaceResult result = new RaceResult();
    private Coroutine countdownCoroutine;
    private Rigidbody localPlayerRb;

    public event Action<RaceState> StateChanged;
    public event Action<float> TimerChanged;
    public event Action<string> StatusChanged;
    public event Action<RaceResult> ResultChanged;

    public RaceState State => state;
    public float RaceTimer => raceTimer;
    public float FinishTime => finishTime;
    public float CountdownRemaining => countdownRemaining;
    public string StatusMessage => statusMessage;
    public RaceResult Result => result;
    public bool IsRunning => state == RaceState.Running;
    public bool IsFinished => state == RaceState.Finished;
    public bool HasGhostRun => ghostPlayback != null && ghostPlayback.HasRun;

    void Awake()
    {
        ActiveRace = this;
        ResolveReferences();
        PrepareRecorderForRaceControl();
        PrepareGhostForRaceControl();
        PrepareTransport();
        EnsureRaceUI();

        if (enablePracticeRespawn)
            DeathMenuUI.PracticeModeActive = true;
    }

    void Start()
    {
        if (startRaceOnSceneStart)
            StartCountdown();
        else
            SetState(RaceState.Waiting);
    }

    void Update()
    {
        if (state != RaceState.Running)
            return;

        raceTimer += Time.deltaTime;
        SendTransportSnapshotTick();
        UpdateTimerText(raceTimer);
        TimerChanged?.Invoke(raceTimer);
    }

    void OnGUI()
    {
        if (!drawDebugOverlay)
            return;

        GUI.Label(
            new Rect(16f, 16f, 260f, 28f),
            "Race: " + state + "  " + raceTimer.ToString("0.00")
        );
    }

    void OnDestroy()
    {
        if (transport != null)
            transport.SnapshotReceived -= HandleRemoteSnapshotReceived;

        if (ActiveRace == this)
            ActiveRace = null;
    }

    public void StartCountdown()
    {
        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        StopLocalRecorder(false);
        StopGhostPlayback();
        ClearTransportQueue();
        ResetResult();
        transportSendTimer = 0f;
        countdownCoroutine = StartCoroutine(CountdownCoroutine());
    }

    public bool TryFinishLocalPlayer()
    {
        if (state != RaceState.Running)
            return false;

        finishTime = raceTimer;
        result.localFinished = true;
        result.localFinishTime = finishTime;
        CaptureGhostReplayFinishIfNeeded();
        SendTransportSnapshot(true);
        StopLocalRecorder(saveRunOnFinish);
        SetState(RaceState.Finished);
        FreezeLocalPlayerOnFinish();
        SetStatusText("Finished: " + finishTime.ToString("0.00"));
        NotifyResultChanged();

        return true;
    }

    public static bool TryFinishActiveRace()
    {
        if (ActiveRace == null)
            return false;

        return ActiveRace.TryFinishLocalPlayer();
    }

    public void ResetRace()
    {
        raceTimer = 0f;
        finishTime = 0f;
        countdownRemaining = 0f;
        ResetResult();
        StartCountdown();
    }

    public void SetTransport(RaceTransportBehaviour newTransport, bool enableSnapshots)
    {
        if (transport != null)
            transport.SnapshotReceived -= HandleRemoteSnapshotReceived;

        transport = newTransport;
        enableSnapshotTransport = enableSnapshots;
        PrepareTransport();
    }

    private IEnumerator CountdownCoroutine()
    {
        SetState(RaceState.Countdown);
        raceTimer = 0f;
        finishTime = 0f;
        countdownRemaining = Mathf.Max(0f, countdownSeconds);
        TimerChanged?.Invoke(raceTimer);

        if (enablePracticeRespawn)
            DeathMenuUI.PracticeModeActive = true;

        SetDeathChecksEnabled(!ShouldPauseDeathChecksDuringCountdown());

        if (freezePlayerDuringCountdown)
            SetPlayerMovementEnabled(false);

        float remaining = countdownRemaining;

        while (remaining > 0f)
        {
            countdownRemaining = remaining;
            SetStatusText(Mathf.CeilToInt(remaining).ToString());
            yield return null;
            remaining -= Time.deltaTime;
        }

        countdownRemaining = 0f;
        SetStatusText("Go");
        SetDeathChecksEnabled(true);
        ResetLocalDeathState();
        RestoreLocalPlayerGameplayComponents();
        UnfreezeLocalPlayerForRace();
        BeginLocalRecorder();
        StartGhostPlayback();
        SetPlayerMovementEnabled(true);
        SetState(RaceState.Running);
    }

    private void ResolveReferences()
    {
        if (localPlayerMove == null)
            localPlayerMove = FindLocalPlayerMove();

        if (localDeathScript == null && localPlayerMove != null)
            localDeathScript = localPlayerMove.GetComponent<DeathScript>();

        if (practiceModeManager == null && localPlayerMove != null)
            practiceModeManager = localPlayerMove.GetComponent<PracticeModeManager>();

        if (localRecorder == null && localPlayerMove != null)
            localRecorder = localPlayerMove.GetComponent<GhostRunRecorder>();

        if (localPlayerRb == null && localPlayerMove != null)
            localPlayerRb = localPlayerMove.GetComponent<Rigidbody>();

        if (localTrail == null && localPlayerMove != null)
            localTrail = localPlayerMove.GetComponentInChildren<GroundTrailFromPoints>(true);

        if (localRollVisualController == null && localPlayerMove != null)
            localRollVisualController = localPlayerMove.GetComponent<SkinRollVisualController>();

        if (ghostPlayback == null)
            ghostPlayback = FindFirstObjectByType<GhostRunPlayback>(FindObjectsInactive.Include);

        if (transport == null)
            transport = FindFirstObjectByType<RaceTransportBehaviour>(FindObjectsInactive.Include);

        if (localPlayerMove != null &&
            localPlayerMove.GetComponentInParent<GhostRunPlayback>() != null)
        {
            Debug.LogWarning("RaceModeManager localPlayerMove points to a ghost object.");
        }
    }

    private PlayerMove FindLocalPlayerMove()
    {
        PlayerMove[] players = FindObjectsByType<PlayerMove>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
                continue;

            if (players[i].GetComponentInParent<GhostRunPlayback>() != null)
                continue;

            if (players[i].CompareTag("Player"))
                return players[i];
        }

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] != null &&
                players[i].GetComponentInParent<GhostRunPlayback>() == null)
            {
                return players[i];
            }
        }

        return null;
    }

    private void SetState(RaceState newState)
    {
        state = newState;
        UpdateTimerText(raceTimer);
        StateChanged?.Invoke(state);
    }

    private void SetPlayerMovementEnabled(bool value)
    {
        if (localPlayerMove != null)
            localPlayerMove.enabled = value;
    }

    private void SetDeathChecksEnabled(bool value)
    {
        if (localDeathScript != null)
            localDeathScript.enabled = value;
    }

    private bool ShouldPauseDeathChecksDuringCountdown()
    {
        return disableDeathChecksDuringCountdown || freezePlayerDuringCountdown;
    }

    private void ResetLocalDeathState()
    {
        if (localDeathScript != null)
            localDeathScript.ResetDeathState();
    }

    private void RestoreLocalPlayerGameplayComponents()
    {
        if (localPlayerMove == null)
            return;

        EnableComponent<PlayerCollision>();
        EnableComponent<DeathScript>();
        EnableComponent<PlayerGravityFlip>();
        EnableComponent<PlayerJumpHeightBonus>();
        EnableComponent<PlayerSpeedBoostBonus>();
        EnableComponent<PlayerJumpSpeedDashBonus>();
        EnableComponent<GroundTrailFromPoints>();
        EnableComponent<GhostRunRecorder>();

        if (practiceModeManager != null)
            practiceModeManager.enabled = true;
    }

    private void FreezeLocalPlayerOnFinish()
    {
        SetPlayerMovementEnabled(false);
        SetDeathChecksEnabled(false);

        PlayerCollision playerCollision = localPlayerMove != null
            ? localPlayerMove.GetComponent<PlayerCollision>()
            : null;

        if (playerCollision != null)
            playerCollision.enabled = false;

        if (localRecorder != null)
            localRecorder.enabled = false;

        if (localTrail != null)
            localTrail.StopTrail();

        if (localRollVisualController != null)
            localRollVisualController.ResetMotionTracking();

        if (localPlayerRb != null)
        {
            localPlayerRb.linearVelocity = Vector3.zero;
            localPlayerRb.angularVelocity = Vector3.zero;
            localPlayerRb.isKinematic = true;
        }
    }

    private void UnfreezeLocalPlayerForRace()
    {
        if (localPlayerRb != null)
        {
            localPlayerRb.isKinematic = false;
            localPlayerRb.linearVelocity = Vector3.zero;
            localPlayerRb.angularVelocity = Vector3.zero;
        }

        if (localRollVisualController != null)
            localRollVisualController.ResetMotionTracking();
    }

    private void PrepareRecorderForRaceControl()
    {
        if (!controlRecorderTiming || localRecorder == null)
            return;

        localRecorder.recordOnStart = false;
    }

    private void PrepareGhostForRaceControl()
    {
        if (!controlGhostTiming || ghostPlayback == null)
            return;

        ghostPlayback.playSavedRunOnStart = false;
    }

    private void BeginLocalRecorder()
    {
        if (!controlRecorderTiming || localRecorder == null)
            return;

        localRecorder.enabled = true;
        localRecorder.BeginRecording();
    }

    private void StopLocalRecorder(bool save)
    {
        if (!controlRecorderTiming || localRecorder == null || !localRecorder.IsRecording)
            return;

        localRecorder.StopRecording(save);
    }

    private void StartGhostPlayback()
    {
        if (!controlGhostTiming || ghostPlayback == null)
            return;

        ghostPlayback.enabled = true;
        ghostPlayback.PlaySavedRun();
    }

    private void StopGhostPlayback()
    {
        if (!controlGhostTiming || ghostPlayback == null)
            return;

        ghostPlayback.Stop();
    }

    private void PrepareTransport()
    {
        if (!enableSnapshotTransport || transport == null)
            return;

        if (connectTransportOnStart && !transport.IsConnected)
            transport.Connect();

        transport.SnapshotReceived -= HandleRemoteSnapshotReceived;
        transport.SnapshotReceived += HandleRemoteSnapshotReceived;
    }

    private void ClearTransportQueue()
    {
        LocalLoopbackRaceTransport loopback = transport as LocalLoopbackRaceTransport;
        if (loopback != null)
            loopback.ClearQueue();
    }

    private void SendTransportSnapshotTick()
    {
        if (!enableSnapshotTransport || transport == null || !transport.IsConnected)
            return;

        transportSendTimer += Time.deltaTime;
        if (transportSendTimer < Mathf.Max(0.01f, transportSendInterval))
            return;

        transportSendTimer = 0f;
        SendTransportSnapshot(false);
    }

    private void SendTransportSnapshot(bool finished)
    {
        if (!enableSnapshotTransport || transport == null || !transport.IsConnected)
            return;

        RacePlayerSnapshot snapshot = CreateLocalSnapshot(finished);
        if (snapshot != null)
            transport.SendSnapshot(snapshot);
    }

    private void HandleRemoteSnapshotReceived(RacePlayerSnapshot snapshot)
    {
        if (snapshot == null || !snapshot.finished || result.remoteFinished)
            return;

        result.remoteFinished = true;
        result.remoteFinishTime = snapshot.time;
        CompleteRaceIfResultsReady();
        NotifyResultChanged();
    }

    private void CaptureGhostReplayFinishIfNeeded()
    {
        if (result.remoteFinished || ghostPlayback == null || !ghostPlayback.HasRun)
            return;

        result.remoteFinished = true;
        result.remoteFinishTime = ghostPlayback.RunDuration;
        CompleteRaceIfResultsReady();
    }

    private RacePlayerSnapshot CreateLocalSnapshot(bool finished)
    {
        if (localRecorder != null)
        {
            RacePlayerSnapshot snapshot = localRecorder.GetCurrentSnapshot();
            if (snapshot != null)
            {
                snapshot.time = raceTimer;
                snapshot.finished = finished;
                return snapshot;
            }
        }

        if (localPlayerMove == null)
            return null;

        PlayerSkinSwitcher skinSwitcher = localPlayerMove.GetComponentInChildren<PlayerSkinSwitcher>(true);
        DeathScript deathScript = localDeathScript != null
            ? localDeathScript
            : localPlayerMove.GetComponent<DeathScript>();

        return new RacePlayerSnapshot(
            raceTimer,
            localPlayerMove.transform.position,
            localPlayerMove.transform.rotation,
            deathScript == null || !deathScript.IsDead(),
            finished,
            skinSwitcher != null ? skinSwitcher.GetCurrentSkinIndex() : 0
        );
    }

    private void EnsureRaceUI()
    {
        if (!autoCreateRaceUI)
            return;

        if (raceUI == null)
            raceUI = FindFirstObjectByType<RaceUIController>(FindObjectsInactive.Include);

        if (raceUI == null)
            raceUI = RaceUIController.CreateRuntimeUI(this);
        else
            raceUI.Bind(this);
    }

    private void ResetResult()
    {
        result.Reset();
        NotifyResultChanged();
    }

    private void NotifyResultChanged()
    {
        ResultChanged?.Invoke(result);
    }

    private void CompleteRaceIfResultsReady()
    {
        if (!waitForRemoteResult)
            return;

        if (result.localFinished && result.remoteFinished)
            SetStatusText("Finished");
    }

    private void EnableComponent<T>() where T : Behaviour
    {
        T component = localPlayerMove.GetComponent<T>();
        if (component != null)
            component.enabled = true;
    }

    private void SetStatusText(string value)
    {
        statusMessage = value;

        if (statusText != null)
            statusText.text = value;

        StatusChanged?.Invoke(statusMessage);
    }

    private void UpdateTimerText(float value)
    {
        if (timerText != null)
            timerText.text = value.ToString("0.00");
    }
}
