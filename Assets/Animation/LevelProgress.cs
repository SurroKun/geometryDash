using UnityEngine;

public static class LevelProgress
{
    private const string UnlockedLevelKey = "UnlockedLevelIndex";

    public static int GetUnlockedLevelIndex()
    {
        return PlayerPrefs.GetInt(UnlockedLevelKey, 0);
    }

    public static void UnlockLevel(int levelIndex)
    {
        int currentUnlocked = GetUnlockedLevelIndex();

        if (levelIndex > currentUnlocked)
        {
            PlayerPrefs.SetInt(UnlockedLevelKey, levelIndex);
            PlayerPrefs.Save();
        }
    }

    public static void ResetProgress()
    {
        PlayerPrefs.DeleteKey(UnlockedLevelKey);
        PlayerPrefs.Save();
    }
}