using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    public string mainMenuSceneName = "MainMenu";
    public string gameSceneName = "Game";
    public string skinMenuSceneName = "SkinMenu";

    [Header("Multiplayer")]
    public bool createMultiplayerEntry = true;
    public string multiplayerLevelSelectSceneName = "LevelSelect";
    public Vector2 multiplayerButtonPosition = new Vector2(14f, -104f);
    public Vector2 multiplayerButtonSize = new Vector2(220f, 64f);

    private GameObject multiplayerPanel;
    private TMP_InputField multiplayerAddressInput;
    private TMP_Text multiplayerStatusText;
    private Button multiplayerStartButton;

    void Start()
    {
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
        {
            RaceMultiplayerBootstrap.ClearMode();
            RaceOnlineSessionManager.Shutdown();
        }

        if (createMultiplayerEntry)
            EnsureMultiplayerEntry();
    }

    public void PlayGame()
    {
        Time.timeScale = 1f;
        RaceMultiplayerBootstrap.ClearMode();
        RaceOnlineSessionManager.Shutdown();
        DeathMenuUI.DisablePracticeMode();
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSkinMenu()
    {
        RaceMultiplayerBootstrap.ClearMode();
        RaceOnlineSessionManager.Shutdown();
        SceneManager.LoadScene(skinMenuSceneName);
    }

    public void OpenMultiplayerMenu()
    {
        EnsureMultiplayerEntry();

        if (multiplayerPanel != null)
            multiplayerPanel.SetActive(true);
    }

    public void CloseMultiplayerMenu()
    {
        if (multiplayerPanel != null)
            multiplayerPanel.SetActive(false);
    }

    public async void HostMultiplayer()
    {
        RaceMultiplayerBootstrap.ArmHostMode();
        SetMultiplayerStatus("Creating Relay...");
        SetMultiplayerStartInteractable(false);

        string joinCode = await RaceOnlineSessionManager.StartRelayHostAsync();
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            SetMultiplayerStatus("Host failed: " + RaceOnlineSessionManager.LastError);
            return;
        }

        SetMultiplayerStatus("Code: " + joinCode);
        SetMultiplayerStartInteractable(true);
    }

    public void OpenMultiplayerLevelSelect()
    {
        RaceMultiplayerBootstrap.ArmHostMode();

        Time.timeScale = 1f;
        DeathMenuUI.DisablePracticeMode();
        SceneManager.LoadScene(multiplayerLevelSelectSceneName);
    }

    public async void JoinMultiplayer()
    {
        RaceMultiplayerBootstrap.ArmJoinMode();
        string joinCode = multiplayerAddressInput != null
            ? multiplayerAddressInput.text
            : "";

        SetMultiplayerStatus("Joining...");
        bool joined = await RaceOnlineSessionManager.StartRelayClientAsync(joinCode);
        if (!joined)
        {
            SetMultiplayerStatus("Join failed: " + RaceOnlineSessionManager.LastError);
            return;
        }

        SetMultiplayerStatus("Joined");

        Time.timeScale = 1f;
        DeathMenuUI.DisablePracticeMode();
        SceneManager.LoadScene(multiplayerLevelSelectSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }

    public void BackToMainMenu()
    {
        RaceMultiplayerBootstrap.ClearMode();
        RaceOnlineSessionManager.Shutdown();
        SceneManager.LoadScene("MainMenu");
    }

    private void EnsureMultiplayerEntry()
    {
        if (SceneManager.GetActiveScene().name != mainMenuSceneName)
            return;

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        Transform root = canvas.transform;

        if (GameObject.Find("Multiplayer") == null)
        {
            CreateMenuButton(
                root,
                "Multiplayer",
                "Multiplayer",
                multiplayerButtonPosition,
                multiplayerButtonSize,
                OpenMultiplayerMenu
            );
        }

        if (multiplayerPanel == null)
            multiplayerPanel = CreateMultiplayerPanel(root);
    }

    private GameObject CreateMultiplayerPanel(Transform parent)
    {
        GameObject panel = new GameObject("Multiplayer Panel");
        panel.transform.SetParent(parent, false);

        RectTransform panelRect = panel.AddComponent<RectTransform>();
        SetRect(panelRect, Vector2.zero, new Vector2(460f, 420f));

        Image background = panel.AddComponent<Image>();
        background.color = new Color(0.03f, 0.04f, 0.05f, 0.88f);

        TMP_Text title = CreateText(panel.transform, "Title", "Multiplayer", 32f);
        SetRect(title.rectTransform, new Vector2(0f, 164f), new Vector2(360f, 60f));

        CreateMenuButton(panel.transform, "Host Button", "Host", new Vector2(0f, 104f), new Vector2(220f, 52f), HostMultiplayer);
        multiplayerAddressInput = CreateInput(panel.transform, "Code Input", "", new Vector2(0f, 34f), new Vector2(260f, 48f), "Relay code");
        CreateMenuButton(panel.transform, "Join Button", "Join", new Vector2(0f, -32f), new Vector2(220f, 52f), JoinMultiplayer);
        multiplayerStartButton = CreateMenuButton(panel.transform, "Select Level Button", "Select Level", new Vector2(0f, -94f), new Vector2(220f, 46f), OpenMultiplayerLevelSelect);
        SetMultiplayerStartInteractable(false);
        CreateMenuButton(panel.transform, "Back Button", "Back", new Vector2(0f, -166f), new Vector2(160f, 46f), CloseMultiplayerMenu);

        multiplayerStatusText = CreateText(panel.transform, "Status", "", 20f);
        SetRect(multiplayerStatusText.rectTransform, new Vector2(0f, -132f), new Vector2(400f, 34f));
        multiplayerStatusText.color = Color.white;

        panel.SetActive(false);
        return panel;
    }

    private Button CreateMenuButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchoredPosition,
        Vector2 size,
        UnityEngine.Events.UnityAction onClick
    )
    {
        GameObject buttonObject = new GameObject(objectName);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        SetRect(rect, anchoredPosition, size);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.15f, 0.53f, 0.86f, 0.95f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        TMP_Text text = CreateText(buttonObject.transform, "Label", label, 22f);
        text.color = Color.white;
        Stretch(text.rectTransform);

        return button;
    }

    private TMP_InputField CreateInput(
        Transform parent,
        string objectName,
        string value,
        Vector2 anchoredPosition,
        Vector2 size,
        string placeholderText
    )
    {
        GameObject inputObject = new GameObject(objectName);
        inputObject.transform.SetParent(parent, false);

        RectTransform rect = inputObject.AddComponent<RectTransform>();
        SetRect(rect, anchoredPosition, size);

        Image image = inputObject.AddComponent<Image>();
        image.color = new Color(0.95f, 0.96f, 0.98f, 0.96f);

        TMP_InputField input = inputObject.AddComponent<TMP_InputField>();
        input.text = value;
        input.textViewport = rect;

        TMP_Text text = CreateText(inputObject.transform, "Text", value, 20f);
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.black;
        Stretch(text.rectTransform);

        TMP_Text placeholder = CreateText(inputObject.transform, "Placeholder", placeholderText, 18f);
        placeholder.alignment = TextAlignmentOptions.Center;
        placeholder.color = new Color(0f, 0f, 0f, 0.38f);
        Stretch(placeholder.rectTransform);

        input.textComponent = text;
        input.placeholder = placeholder;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.characterLimit = 64;

        return input;
    }

    private void SetMultiplayerStatus(string value)
    {
        if (multiplayerStatusText != null)
            multiplayerStatusText.text = value;
    }

    private void SetMultiplayerStartInteractable(bool value)
    {
        if (multiplayerStartButton != null)
            multiplayerStartButton.interactable = value;
    }

    private TMP_Text CreateText(
        Transform parent,
        string objectName,
        string value,
        float fontSize
    )
    {
        GameObject textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;

        return text;
    }

    private void SetRect(RectTransform rect, Vector2 anchoredPosition, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
