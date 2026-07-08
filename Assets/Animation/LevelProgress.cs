using UnityEngine;

public static class LevelProgress
{
    private const string UnlockedLevelKey = "UnlockedLevelIndex";

    public static int GetUnlockedLevelIndex()
    {
        return PlayerPrefs.GetInt(UnlockedLevelKey, GameProgress.defaultUnlockedLevels - 1);
    }

    public static void UnlockLevel(int levelIndex)
    {
        GameProgress.UnlockLevel(levelIndex);
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(UnlockedLevelKey);
        PlayerPrefs.Save();
    }
}
