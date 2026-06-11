using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

[System.Serializable]
public class LevelData
{
    [Header("Main Info")]
    public string levelName;
    public string sceneName;
    public bool unlocked = true;

    [Header("Preview Scene Objects")]
    public GameObject previewRoot;
    public Transform previewStart;
    public Transform previewEnd;
    public Transform previewLookTarget;
}

public class LevelSelectManager : MonoBehaviour
{
    [Header("Levels")]
    public LevelData[] levels;
    public int currentLevelIndex = 0;

    [Header("UI")]
    public TMP_Text levelNameText;
    public TMP_Text levelCountText;
    public TMP_Text lockText;

    public Button playButton;
    public Button leftButton;
    public Button rightButton;

    [Header("Preview Camera")]
    public PreviewCameraSimpleFly previewCameraFly;

    [Header("Debug")]
    public bool resetProgressOnStart = false;

    void Start()
    {
        if (levels == null || levels.Length == 0)
        {
            Debug.LogError("LevelSelectManager: levels array is empty.");
            return;
        }

        if (resetProgressOnStart)
        {
            LevelProgress.ResetProgress();
            Debug.Log("Progress reset from inspector");
        }

        ApplySavedProgress();

        currentLevelIndex = Mathf.Clamp(currentLevelIndex, 0, levels.Length - 1);
        RefreshUI();
    }

    void ApplySavedProgress()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            levels[i].unlocked = true;
        }
    }

    public void NextLevel()
    {
        if (levels == null || levels.Length == 0)
            return;

        if (currentLevelIndex < levels.Length - 1)
        {
            currentLevelIndex++;
            RefreshUI();
        }
    }

    public void PreviousLevel()
    {
        if (levels == null || levels.Length == 0)
            return;

        if (currentLevelIndex > 0)
        {
            currentLevelIndex--;
            RefreshUI();
        }
    }

    public void PlayCurrentLevel()
    {
        if (levels == null || levels.Length == 0)
            return;

        LevelData level = levels[currentLevelIndex];

        if (!level.unlocked)
        {
            Debug.Log("Level is locked.");
            return;
        }

        if (string.IsNullOrEmpty(level.sceneName))
        {
            Debug.LogError("LevelSelectManager: sceneName is empty for level: " + level.levelName);
            return;
        }

        Time.timeScale = 1f;
        DeathMenuUI.DisablePracticeMode();
        SceneManager.LoadScene(level.sceneName);
    }

    void RefreshUI()
    {
        LevelData level = levels[currentLevelIndex];

        if (levelNameText != null)
            levelNameText.text = level.levelName;

        if (levelCountText != null)
            levelCountText.text = (currentLevelIndex + 1) + " / " + levels.Length;

        if (lockText != null)
            lockText.gameObject.SetActive(!level.unlocked);

        if (playButton != null)
            playButton.interactable = level.unlocked;

        if (leftButton != null)
            leftButton.interactable = currentLevelIndex > 0;

        if (rightButton != null)
            rightButton.interactable = currentLevelIndex < levels.Length - 1;

        UpdatePreview();
    }

    void UpdatePreview()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i].previewRoot != null)
                levels[i].previewRoot.SetActive(i == currentLevelIndex);
        }

        if (previewCameraFly == null)
            return;

        LevelData level = levels[currentLevelIndex];

        previewCameraFly.customLookTarget = level.previewLookTarget;
        previewCameraFly.SetPoints(level.previewStart, level.previewEnd);
    }
}