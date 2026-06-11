using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelSelectMenu : MonoBehaviour
{
    public void LoadLevel(string levelName)
    {
        Time.timeScale = 1f;
        DeathMenuUI.DisablePracticeMode();
        SceneManager.LoadScene(levelName);
    }
}