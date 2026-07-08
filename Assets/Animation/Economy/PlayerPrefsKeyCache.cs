using System.Collections.Generic;
using UnityEngine;

public static class PlayerPrefsKeyCache
{
    private const string KnownKeysKey = "KnownPlayerPrefsKeys";
    private const char Separator = '|';

    public static void RememberKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            return;

        List<string> keys = GetKnownKeys();

        if (keys.Contains(key))
            return;

        keys.Add(key);
        PlayerPrefs.SetString(KnownKeysKey, string.Join(Separator.ToString(), keys));
        PlayerPrefs.Save();
    }

    public static List<string> GetKnownKeys()
    {
        string raw = PlayerPrefs.GetString(KnownKeysKey, "");
        List<string> result = new List<string>();

        if (string.IsNullOrEmpty(raw))
            return result;

        string[] split = raw.Split(Separator);

        for (int i = 0; i < split.Length; i++)
        {
            if (!string.IsNullOrEmpty(split[i]) && !result.Contains(split[i]))
                result.Add(split[i]);
        }

        return result;
    }
}
