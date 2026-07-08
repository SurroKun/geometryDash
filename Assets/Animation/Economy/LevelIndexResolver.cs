using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelIndexResolver
{
    public static int firstLevelBuildIndex = 2;

    public static int GetCurrentLevelIndex()
    {
        return SceneManager.GetActiveScene().buildIndex - firstLevelBuildIndex;
    }
}
