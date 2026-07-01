using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathMenuUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject deathMenu;
    public GameObject resumeButton;

    [Header("Scenes")]
    public string mainMenuSceneName = "MainMenu";
    public string levelSelectSceneName = "LevelSelect";

    [Header("Delays")]
    public float deathPauseDelay = 0.25f;
    public float resetDelay = 0.35f;

    private const string SkipDeathMenuKey = "SkipDeathMenu";
    private const string PracticeModeKey = "PracticeMode";

    private bool isMenuShown = false;
    private bool isPaused = false;
    private bool isWaitingForPause = false;
    private bool isWaitingForReset = false;

    private bool SkipDeathMenu
    {
        get => PlayerPrefs.GetInt(SkipDeathMenuKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(SkipDeathMenuKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool PracticeModeActive
    {
        get => PlayerPrefs.GetInt(PracticeModeKey, 0) == 1;
        set
        {
            PlayerPrefs.SetInt(PracticeModeKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    void Start()
    {
        SetDeathMenuVisible(false);
        Time.timeScale = 1f;
    }

    public void ShowDeathMenu()
    {
        if (IsMenuBusy())
            return;

        if (SkipDeathMenu)
        {
            StartCoroutine(ResetAfterDeathDelay());
            return;
        }

        ShowMenu(false);
        isWaitingForPause = true;

        StartCoroutine(PauseAfterDeathDelay());
    }

    public void OpenPauseMenu()
    {
        if (IsMenuBusy())
            return;

        ShowMenu(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        if (!isPaused)
            return;

        ClosePauseMenuOnly();
    }

    public void ClosePauseMenuOnly()
    {
        ResetStateFlags();
        HideMenu();
        Time.timeScale = 1f;
    }

    public void RetryLevel()
    {
        RaceMultiplayerBootstrap.ClearMode();
        RaceOnlineSessionManager.Shutdown();
        PrepareSceneLoad(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        RaceMultiplayerBootstrap.ClearMode();
        RaceOnlineSessionManager.Shutdown();
        PrepareSceneLoad(false);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OpenLevelSelect()
    {
        RaceMultiplayerBootstrap.ClearMode();
        RaceOnlineSessionManager.Shutdown();
        PrepareSceneLoad(false);
        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void ResetWithoutMenu()
    {
        if (isWaitingForReset)
            return;

        StopAllCoroutines();
        ResetStateFlags();

        PracticeModeActive = false;
        SkipDeathMenu = true;
        Time.timeScale = 1f;

        StartCoroutine(ResetFromMenuDelay());
    }

    public void EnableDeathMenuAgain()
    {
        SkipDeathMenu = false;
    }

    public void StartPracticeMode()
    {
        ResetStateFlags();

        PracticeModeActive = true;
        SkipDeathMenu = false;
        Time.timeScale = 1f;

        StartCoroutine(StartPracticeModeDelay());
    }

    public static void DisablePracticeMode()
    {
        PracticeModeActive = false;
    }

    public void ShowPracticeCompleteMenu()
    {
        if (IsMenuBusy())
            return;

        PracticeModeActive = false;
        ShowMenu(false);
        Time.timeScale = 0f;
    }

    private IEnumerator PauseAfterDeathDelay()
    {
        yield return new WaitForSeconds(deathPauseDelay);

        Time.timeScale = 0f;
        isWaitingForPause = false;
    }

    private IEnumerator ResetAfterDeathDelay()
    {
        isWaitingForReset = true;

        yield return new WaitForSeconds(resetDelay);

        RetryLevel();
    }

    private IEnumerator ResetFromMenuDelay()
    {
        isWaitingForReset = true;
        SetDeathMenuVisible(false);

        yield return new WaitForSeconds(resetDelay);

        RetryLevel();
    }

    private IEnumerator StartPracticeModeDelay()
    {
        isWaitingForReset = true;
        SetDeathMenuVisible(false);

        yield return new WaitForSeconds(resetDelay);

        ResetStateFlags();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private bool IsMenuBusy()
    {
        return isMenuShown || isWaitingForPause || isWaitingForReset;
    }

    private void ShowMenu(bool showResumeButton)
    {
        isMenuShown = true;
        isPaused = showResumeButton;
        isWaitingForPause = false;
        isWaitingForReset = false;

        SetResumeButtonVisible(showResumeButton);
        SetDeathMenuVisible(true);
    }

    private void HideMenu()
    {
        SetDeathMenuVisible(false);
        SetResumeButtonVisible(false);
    }

    private void SetDeathMenuVisible(bool value)
    {
        if (deathMenu != null)
            deathMenu.SetActive(value);
    }

    private void SetResumeButtonVisible(bool value)
    {
        if (resumeButton != null)
            resumeButton.SetActive(value);
    }

    private void PrepareSceneLoad(bool practiceModeActive)
    {
        ResetStateFlags();
        PracticeModeActive = practiceModeActive;
        Time.timeScale = 1f;
    }

    private void ResetStateFlags()
    {
        isMenuShown = false;
        isPaused = false;
        isWaitingForPause = false;
        isWaitingForReset = false;
    }
}
