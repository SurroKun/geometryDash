using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string gameSceneName = "Game";
    public string skinMenuSceneName = "SkinMenu"; // ← добавили

    public void PlayGame()
    {
        Time.timeScale = 1f;
        DeathMenuUI.DisablePracticeMode();
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenSkinMenu() // ← новый метод
    {
        SceneManager.LoadScene(skinMenuSceneName);
    }

    public void QuitGame()
    {
        Debug.Log("Quit");
        Application.Quit();
    }
    public void BackToMainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}