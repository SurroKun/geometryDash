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

    private bool isMenuShown = false;
    private bool isPaused = false;
    private bool isWaitingForPause = false;
    private bool isWaitingForReset = false;

    private const string SkipDeathMenuKey = "SkipDeathMenu";
    private const string PracticeModeKey = "PracticeMode";

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

    // 👇 ДОБАВЛЕНО: сброс всех флагов
    private void ResetStateFlags()
    {
        isMenuShown = false;
        isPaused = false;
        isWaitingForPause = false;
        isWaitingForReset = false;
    }

    void Start()
    {
        if (deathMenu != null)
            deathMenu.SetActive(false);

        Time.timeScale = 1f;
    }

    public void ShowDeathMenu()
    {
        if (isMenuShown || isWaitingForPause || isWaitingForReset)
            return;

        if (SkipDeathMenu)
        {
            StartCoroutine(ResetAfterDeathDelay());
            return;
        }

        isMenuShown = true;
        isPaused = false;
        isWaitingForPause = true;

        if (resumeButton != null)
            resumeButton.SetActive(false);

        if (deathMenu != null)
            deathMenu.SetActive(true);

        StartCoroutine(PauseAfterDeathDelay());
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

        ResetStateFlags(); // 👈 фикс

        RetryLevel();
    }

    public void OpenPauseMenu()
    {
        if (isMenuShown || isWaitingForPause || isWaitingForReset)
            return;

        isMenuShown = true;
        isPaused = true;

        if (resumeButton != null)
            resumeButton.SetActive(true);

        if (deathMenu != null)
            deathMenu.SetActive(true);

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
        ResetStateFlags(); // 👈 аккуратно заменили

        if (deathMenu != null)
            deathMenu.SetActive(false);

        if (resumeButton != null)
            resumeButton.SetActive(false);

        Time.timeScale = 1f;
    }

    public void RetryLevel()
    {
        ResetStateFlags(); // 👈 фикс

        PracticeModeActive = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMenu()
    {
        ResetStateFlags(); // 👈 безопасно

        PracticeModeActive = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void OpenLevelSelect()
    {
        ResetStateFlags(); // 👈 безопасно

        PracticeModeActive = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelSelectSceneName);
    }

    public void ResetWithoutMenu()
    {
        if (isWaitingForReset)
            return;

        StopAllCoroutines(); // 👈 фикс зависших корутин
        ResetStateFlags();   // 👈 фикс состояния

        PracticeModeActive = false;
        SkipDeathMenu = true;
        Time.timeScale = 1f;
        StartCoroutine(ResetFromMenuDelay());
    }

    private IEnumerator ResetFromMenuDelay()
    {
        isWaitingForReset = true;

        if (deathMenu != null)
            deathMenu.SetActive(false);

        yield return new WaitForSeconds(resetDelay);

        ResetStateFlags(); // 👈 фикс

        RetryLevel();
    }

    public void EnableDeathMenuAgain()
    {
        SkipDeathMenu = false;
    }

    public void StartPracticeMode()
    {
        ResetStateFlags(); // 👈 безопасно

        PracticeModeActive = true;
        SkipDeathMenu = false;
        Time.timeScale = 1f;
        StartCoroutine(StartPracticeModeDelay());
    }

    private IEnumerator StartPracticeModeDelay()
    {
        isWaitingForReset = true;

        if (deathMenu != null)
            deathMenu.SetActive(false);

        yield return new WaitForSeconds(resetDelay);

        ResetStateFlags(); // 👈 фикс

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public static void DisablePracticeMode()
    {
        PracticeModeActive = false;
    }

    public void ShowPracticeCompleteMenu()
    {
        if (isMenuShown || isWaitingForPause || isWaitingForReset)
            return;

        PracticeModeActive = false;

        isMenuShown = true;
        isPaused = false;
        isWaitingForPause = false;
        isWaitingForReset = false;

        if (resumeButton != null)
            resumeButton.SetActive(false);

        if (deathMenu != null)
            deathMenu.SetActive(true);

        Time.timeScale = 0f;
    }
}