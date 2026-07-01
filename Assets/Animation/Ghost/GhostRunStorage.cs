using UnityEngine;
using UnityEngine.SceneManagement;

public static class GhostRunStorage
{
    private const string KeyPrefix = "GhostRun:";

    public static void SaveForCurrentScene(GhostRunData runData)
    {
        Save(GetCurrentSceneKey(), runData);
    }

    public static GhostRunData LoadForCurrentScene()
    {
        return Load(GetCurrentSceneKey());
    }

    public static void Save(string key, GhostRunData runData)
    {
        if (runData == null || !runData.HasFrames())
            return;

        string json = JsonUtility.ToJson(runData);
        PlayerPrefs.SetString(key, json);
        PlayerPrefs.Save();
    }

    public static GhostRunData Load(string key)
    {
        if (!PlayerPrefs.HasKey(key))
            return null;

        string json = PlayerPrefs.GetString(key);
        if (string.IsNullOrEmpty(json))
            return null;

        return JsonUtility.FromJson<GhostRunData>(json);
    }

    public static void ClearForCurrentScene()
    {
        PlayerPrefs.DeleteKey(GetCurrentSceneKey());
        PlayerPrefs.Save();
    }

    private static string GetCurrentSceneKey()
    {
        return KeyPrefix + SceneManager.GetActiveScene().name;
    }
}
