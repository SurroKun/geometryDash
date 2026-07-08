using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFinish : MonoBehaviour
{
    [Header("Scenes")]
    public string levelSelectSceneName = "LevelSelect";

    [Header("Build Index Settings")]
    public int firstLevelBuildIndex = 2; // у тебя lvl1 начинается с 2

    public int levelCount = 16;

    [Header("Practice Testing")]
    public bool completeCurrencyInPracticeMode = false;

    private bool finished = false;

    private void OnTriggerEnter(Collider other)
    {
        if (finished) return;
        if (!other.CompareTag("Player")) return;

        finished = true;

        if (RaceModeManager.TryFinishActiveRace())
            return;

        if (DeathMenuUI.PracticeModeActive)
        {
            if (completeCurrencyInPracticeMode || GameProgress.CompleteCurrencyInPracticeMode)
                CompleteCurrencyProgress();

            DeathMenuUI.DisablePracticeMode();

            DeathMenuUI menu = FindFirstObjectByType<DeathMenuUI>();
            if (menu != null)
                menu.ShowPracticeCompleteMenu();
            else
                Debug.LogWarning("DeathMenuUI not found in scene.");
        }
        else
        {
            CompleteCurrencyProgress();
            UnlockNextLevel();
            Time.timeScale = 1f;
            SceneManager.LoadScene(levelSelectSceneName);
        }
    }

    private void CompleteCurrencyProgress()
    {
        LevelIndexResolver.firstLevelBuildIndex = firstLevelBuildIndex;

        int currentLevelIndex = SceneManager.GetActiveScene().buildIndex - firstLevelBuildIndex;
        RunCurrencyCollector collector = RunCurrencyCollector.Instance;

        if (collector != null)
            collector.CommitLevelComplete(currentLevelIndex, levelCount);
        else
        {
            GameProgress.ResetBaseCoinsForLevel(currentLevelIndex);
            GameProgress.ClearPremiumSpawnSelection(currentLevelIndex);
            GameProgress.RestorePremiumCoinsOnOpenedLevels(levelCount, currentLevelIndex);
        }
    }

    private void UnlockNextLevel()
    {
        int currentBuildIndex = SceneManager.GetActiveScene().buildIndex;
        int nextBuildIndex = currentBuildIndex + 1;

        if (nextBuildIndex >= SceneManager.sceneCountInBuildSettings)
            return;

        // переводим build index сцены в index массива levels
        // build 2 -> level 0
        // build 3 -> level 1
        // build 4 -> level 2
        // build 5 -> level 3
        int nextLevelIndex = nextBuildIndex - firstLevelBuildIndex;

        if (nextLevelIndex >= 0)
        {
            LevelProgress.UnlockLevel(nextLevelIndex);
            Debug.Log("Unlocked level index: " + nextLevelIndex);
        }
    }
}
