using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelFinish : MonoBehaviour
{
    [Header("Scenes")]
    public string levelSelectSceneName = "LevelSelect";

    [Header("Build Index Settings")]
    public int firstLevelBuildIndex = 2; // у тебя lvl1 начинается с 2

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
            DeathMenuUI.DisablePracticeMode();

            DeathMenuUI menu = FindFirstObjectByType<DeathMenuUI>();
            if (menu != null)
                menu.ShowPracticeCompleteMenu();
            else
                Debug.LogWarning("DeathMenuUI not found in scene.");
        }
        else
        {
            UnlockNextLevel();
            Time.timeScale = 1f;
            SceneManager.LoadScene(levelSelectSceneName);
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
