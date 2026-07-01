using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class RaceUIController : MonoBehaviour
{
    [Header("References")]
    public RaceModeManager race;
    public TMP_Text timerText;
    public TMP_Text statusText;
    public TMP_Text countdownText;
    public TMP_Text playerText;
    public TMP_Text ghostText;
    public GameObject finishPanel;
    public TMP_Text finishText;
    public Button restartButton;
    public Button leaveButton;

    [Header("Formatting")]
    public string timerFormat = "0.00";
    public bool hideCountdownWhenInactive = true;
    public float goMessageSeconds = 0.75f;

    [Header("Session")]
    public bool showDebugRestartButton = false;
    public string leaveSceneName = "LevelSelect";

    private const float PanelAlpha = 0.68f;
    private string lastStatusMessage = "";
    private float goMessageHideTime = 0f;

    void Awake()
    {
        if (race == null)
            race = RaceModeManager.ActiveRace;

        WireButtons();

        ApplyFinishLayout();
    }

    void OnDestroy()
    {
        UnwireButtons();
    }

    void Update()
    {
        if (race == null)
            race = RaceModeManager.ActiveRace;

        if (race == null)
            return;

        Refresh();
    }

    public void Bind(RaceModeManager newRace)
    {
        race = newRace;
        ApplyFinishLayout();
        Refresh();
    }

    public static RaceUIController CreateRuntimeUI(RaceModeManager race)
    {
        GameObject canvasObject = new GameObject("Race UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();

        RaceUIController controller = canvasObject.AddComponent<RaceUIController>();
        controller.race = race;

        RectTransform root = canvasObject.GetComponent<RectTransform>();

        GameObject topPanel = CreatePanel(root, "Timer Panel", new Color(0.03f, 0.04f, 0.05f, PanelAlpha));
        SetAnchor(topPanel.GetComponent<RectTransform>(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
        topPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -36f);
        topPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(360f, 72f);

        controller.timerText = CreateText(topPanel.transform, "Timer", "0.00", 34f, TextAlignmentOptions.Center);
        SetStretch(controller.timerText.rectTransform, 0f, 0f, 0f, 28f);

        controller.statusText = CreateText(topPanel.transform, "Status", "Ready", 18f, TextAlignmentOptions.Center);
        SetStretch(controller.statusText.rectTransform, 0f, 0f, 42f, 0f);

        GameObject sidePanel = CreatePanel(root, "Race Status Panel", new Color(0.03f, 0.04f, 0.05f, PanelAlpha));
        SetAnchor(sidePanel.GetComponent<RectTransform>(), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f));
        sidePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-150f, -54f);
        sidePanel.GetComponent<RectTransform>().sizeDelta = new Vector2(280f, 92f);

        controller.playerText = CreateText(sidePanel.transform, "Player Status", "Player: ready", 17f, TextAlignmentOptions.Left);
        SetStretch(controller.playerText.rectTransform, 16f, 16f, 44f, 8f);

        controller.ghostText = CreateText(sidePanel.transform, "Ghost Status", "Ghost: loading", 17f, TextAlignmentOptions.Left);
        SetStretch(controller.ghostText.rectTransform, 16f, 16f, 8f, 44f);

        controller.countdownText = CreateText(root, "Countdown", "", 96f, TextAlignmentOptions.Center);
        SetAnchor(controller.countdownText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        controller.countdownText.rectTransform.sizeDelta = new Vector2(280f, 140f);

        controller.finishPanel = CreatePanel(root, "Finish Panel", new Color(0.03f, 0.04f, 0.05f, 0.84f));
        RectTransform finishRect = controller.finishPanel.GetComponent<RectTransform>();
        SetAnchor(finishRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f));
        finishRect.sizeDelta = new Vector2(520f, 300f);

        controller.finishText = CreateText(controller.finishPanel.transform, "Finish Text", "Finished", 28f, TextAlignmentOptions.Center);
        controller.finishText.lineSpacing = 14f;
        SetStretch(controller.finishText.rectTransform, 24f, 24f, 28f, 96f);

        controller.restartButton = CreateButton(controller.finishPanel.transform, "Restart Button", "Restart");
        controller.restartButton.onClick.AddListener(controller.HandleRestartButton);

        controller.leaveButton = CreateButton(controller.finishPanel.transform, "Leave Button", "Leave");
        controller.leaveButton.onClick.AddListener(controller.LeaveSession);

        controller.ApplyFinishLayout();
        controller.Refresh();
        return controller;
    }

    private void ApplyFinishLayout()
    {
        if (finishPanel != null)
        {
            RectTransform finishRect = finishPanel.GetComponent<RectTransform>();
            if (finishRect != null)
                finishRect.sizeDelta = new Vector2(520f, 300f);
        }

        if (finishText != null)
        {
            finishText.fontSize = 28f;
            finishText.lineSpacing = 14f;
            SetStretch(finishText.rectTransform, 24f, 24f, 28f, 96f);
        }

        bool restartActsAsLeave = ShouldRestartButtonActAsLeave();

        LayoutFinishButton(restartButton, showDebugRestartButton ? -96f : 0f);
        LayoutFinishButton(leaveButton, showDebugRestartButton ? 96f : 0f);

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(showDebugRestartButton || restartActsAsLeave);
            SetButtonLabel(restartButton, restartActsAsLeave ? "Leave" : "Restart");
        }

        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(true);
            SetButtonLabel(leaveButton, "Leave");
        }
    }

    private void LayoutFinishButton(Button button, float x)
    {
        if (button == null)
            return;

        RectTransform buttonRect = button.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            SetAnchor(buttonRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f));
            buttonRect.anchoredPosition = new Vector2(x, 40f);
            buttonRect.sizeDelta = new Vector2(180f, 48f);
        }
    }

    private void Refresh()
    {
        TrackStatusMessage();

        if (timerText != null)
            timerText.text = race.RaceTimer.ToString(timerFormat);

        if (statusText != null)
            statusText.text = GetStatusText();

        if (countdownText != null)
        {
            bool showGo = Time.unscaledTime < goMessageHideTime;
            bool showCountdown = race.State == RaceModeManager.RaceState.Countdown || showGo;
            countdownText.gameObject.SetActive(showCountdown || !hideCountdownWhenInactive);
            countdownText.text = race.State == RaceModeManager.RaceState.Countdown
                ? Mathf.CeilToInt(race.CountdownRemaining).ToString()
                : "Go";
        }

        if (playerText != null)
            playerText.text = GetPlayerText();

        if (ghostText != null)
            ghostText.text = GetGhostText();

        if (finishPanel != null)
            finishPanel.SetActive(ShouldShowFinishPanel());

        if (finishText != null && ShouldShowFinishPanel())
            finishText.text = GetFinishText();
    }

    private void TrackStatusMessage()
    {
        if (lastStatusMessage == race.StatusMessage)
            return;

        lastStatusMessage = race.StatusMessage;

        if (race.StatusMessage == "Go")
            goMessageHideTime = Time.unscaledTime + Mathf.Max(0f, goMessageSeconds);
    }

    private string GetStatusText()
    {
        if (race.State == RaceModeManager.RaceState.Countdown)
            return "Starting";

        if (race.IsRunning)
            return "Race";

        if (race.IsFinished)
        {
            if (race.Result.IsComplete)
                return "Finished";

            return "Waiting";
        }

        return "Ready";
    }

    private string GetPlayerText()
    {
        if (race.IsFinished)
            return "Player: " + race.FinishTime.ToString(timerFormat);

        if (race.IsRunning)
            return "Player: running";

        return "Player: ready";
    }

    private string GetGhostText()
    {
        GhostRunPlayback ghost = race.ghostPlayback;
        if (ghost == null || !ghost.HasRun)
        {
            if (race.Result.remoteFinished)
                return "Opponent: " + race.Result.remoteFinishTime.ToString(timerFormat);

            return "Opponent: no run";
        }

        if (ghost.IsPlaying)
            return "Opponent: racing " + ghost.PlaybackTime.ToString(timerFormat);

        return "Opponent: " + ghost.RunDuration.ToString(timerFormat);
    }

    private string GetFinishText()
    {
        RaceResult result = race.Result;

        string localTime = result.localFinished
            ? result.localFinishTime.ToString(timerFormat)
            : "--";

        string remoteTime = result.remoteFinished
            ? result.remoteFinishTime.ToString(timerFormat)
            : "--";

        if (result.localFinished && !result.remoteFinished)
        {
            return "Finished\n" +
                   "You: " + localTime +
                   "\nWaiting for opponent...";
        }

        return result.WinnerLabel +
               "\nYou: " + localTime +
               "\nOpponent: " + remoteTime;
    }

    private bool ShouldShowFinishPanel()
    {
        return race.IsFinished || race.Result.localFinished;
    }

    private void WireButtons()
    {
        if (restartButton != null)
            restartButton.onClick.AddListener(HandleRestartButton);

        if (leaveButton != null)
            leaveButton.onClick.AddListener(LeaveSession);
    }

    private void UnwireButtons()
    {
        if (restartButton != null)
            restartButton.onClick.RemoveListener(HandleRestartButton);

        if (leaveButton != null)
            leaveButton.onClick.RemoveListener(LeaveSession);
    }

    private void HandleRestartButton()
    {
        if (ShouldRestartButtonActAsLeave())
        {
            LeaveSession();
            return;
        }

        RestartRace();
    }

    private bool ShouldRestartButtonActAsLeave()
    {
        return !showDebugRestartButton && leaveButton == null;
    }

    private void RestartRace()
    {
        if (race != null)
            race.ResetRace();
    }

    private void LeaveSession()
    {
        if (string.IsNullOrEmpty(leaveSceneName))
            return;

        Time.timeScale = 1f;
        RaceMultiplayerBootstrap.ClearMode();
        RaceOnlineSessionManager.Shutdown();
        SceneManager.LoadScene(leaveSceneName);
    }

    private static void SetButtonLabel(Button button, string value)
    {
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = value;
    }

    private static GameObject CreatePanel(Transform parent, string name, Color color)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        Image image = panel.AddComponent<Image>();
        image.color = color;
        return panel;
    }

    private static TMP_Text CreateText(Transform parent, string name, string value, float size, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        return text;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject buttonObject = CreatePanel(parent, name, new Color(0.16f, 0.56f, 0.88f, 0.95f));
        Button button = buttonObject.AddComponent<Button>();

        TMP_Text buttonText = CreateText(buttonObject.transform, "Label", label, 18f, TextAlignmentOptions.Center);
        SetStretch(buttonText.rectTransform, 0f, 0f, 0f, 0f);

        return button;
    }

    private static void SetAnchor(RectTransform rect, Vector2 min, Vector2 max, Vector2 pivot)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.pivot = pivot;
    }

    private static void SetStretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }
}
