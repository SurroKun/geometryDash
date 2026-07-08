using System.Collections.Generic;
using UnityEngine;

public static class GameProgress
{
    private const string BaseCoinsKey = "Currency_BaseCoins";
    private const string PremiumCoinsKey = "Currency_PremiumCoins";
    private const string UnlockAllKey = "Debug_UnlockAllContent";
    private const string CompleteCurrencyInPracticeKey = "Debug_CompleteCurrencyInPractice";
    private const string UnlockedLevelKey = "UnlockedLevelIndex";
    private const string SelectedSkinKey = "SelectedSkin";
    private const string BaseCoinLevelResetKey = "BaseCoinLevelResetVersion_";

    public static int defaultUnlockedLevels = 6;

    public static int BaseCoins
    {
        get { return PlayerPrefs.GetInt(BaseCoinsKey, 0); }
    }

    public static int PremiumCoins
    {
        get { return PlayerPrefs.GetInt(PremiumCoinsKey, 0); }
    }

    public static bool UnlockAllContent
    {
        get { return PlayerPrefs.GetInt(UnlockAllKey, 0) == 1; }
        set
        {
            PlayerPrefs.SetInt(UnlockAllKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static bool CompleteCurrencyInPracticeMode
    {
        get { return PlayerPrefs.GetInt(CompleteCurrencyInPracticeKey, 0) == 1; }
        set
        {
            PlayerPrefs.SetInt(CompleteCurrencyInPracticeKey, value ? 1 : 0);
            PlayerPrefs.Save();
        }
    }

    public static void AddBaseCoins(int amount)
    {
        if (amount <= 0)
            return;

        PlayerPrefs.SetInt(BaseCoinsKey, BaseCoins + amount);
        PlayerPrefs.Save();
    }

    public static bool SpendBaseCoins(int amount)
    {
        if (amount <= 0)
            return true;

        if (BaseCoins < amount)
            return false;

        PlayerPrefs.SetInt(BaseCoinsKey, BaseCoins - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static void AddPremiumCoins(int amount)
    {
        if (amount <= 0)
            return;

        PlayerPrefs.SetInt(PremiumCoinsKey, PremiumCoins + amount);
        PlayerPrefs.Save();
    }

    public static void SetBaseCoins(int amount)
    {
        PlayerPrefs.SetInt(BaseCoinsKey, Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }

    public static void SetPremiumCoins(int amount)
    {
        PlayerPrefs.SetInt(PremiumCoinsKey, Mathf.Max(0, amount));
        PlayerPrefs.Save();
    }

    public static bool SpendPremiumCoins(int amount)
    {
        if (amount <= 0)
            return true;

        if (PremiumCoins < amount)
            return false;

        PlayerPrefs.SetInt(PremiumCoinsKey, PremiumCoins - amount);
        PlayerPrefs.Save();
        return true;
    }

    public static bool IsLevelUnlocked(int levelIndex)
    {
        if (UnlockAllContent)
            return true;

        if (levelIndex < defaultUnlockedLevels)
            return true;

        return levelIndex <= PlayerPrefs.GetInt(UnlockedLevelKey, defaultUnlockedLevels - 1);
    }

    public static void UnlockLevel(int levelIndex)
    {
        int currentUnlocked = PlayerPrefs.GetInt(UnlockedLevelKey, defaultUnlockedLevels - 1);

        if (levelIndex > currentUnlocked)
            PlayerPrefs.SetInt(UnlockedLevelKey, levelIndex);

        PlayerPrefs.Save();
    }

    public static bool BuyLevel(int levelIndex, int premiumCost)
    {
        if (IsLevelUnlocked(levelIndex))
            return true;

        if (!SpendPremiumCoins(premiumCost))
            return false;

        UnlockLevel(levelIndex);
        return true;
    }

    public static int GetLevelMaxPremiumCoins(int levelIndex)
    {
        return PlayerPrefs.GetInt(GetLevelMaxPremiumKey(levelIndex), GetDefaultLevelMaxPremiumCoins(levelIndex));
    }

    public static bool LevelPremiumCanRespawn(int levelIndex)
    {
        return PlayerPrefs.GetInt(GetLevelPremiumRespawnKey(levelIndex), GetDefaultLevelPremiumRespawn(levelIndex) ? 1 : 0) == 1;
    }

    public static int GetLevelAvailablePremiumCoins(int levelIndex)
    {
        int maxCoins = GetLevelMaxPremiumCoins(levelIndex);
        return Mathf.Clamp(PlayerPrefs.GetInt(GetLevelAvailablePremiumKey(levelIndex), maxCoins), 0, maxCoins);
    }

    public static int GetLevelAvailableBaseCoins(int levelIndex, int totalBaseCoins)
    {
        int collectedCount = GetLevelCollectedBaseCoinCount(levelIndex);
        return Mathf.Max(0, totalBaseCoins - collectedCount);
    }

    public static int GetLevelCollectedBaseCoinCount(int levelIndex)
    {
        string prefix = GetBaseCoinPrefix(levelIndex);
        int levelResetVersion = GetBaseCoinLevelResetVersion(levelIndex);
        int count = 0;

        foreach (string key in PlayerPrefsKeyCache.GetKnownKeys())
        {
            if (!key.StartsWith(prefix))
                continue;

            string versionKey = key + "_Version";
            int coinVersion = PlayerPrefs.HasKey(versionKey)
                ? PlayerPrefs.GetInt(versionKey, levelResetVersion)
                : -1;

            if (coinVersion == levelResetVersion && PlayerPrefs.GetInt(key, 0) == 1)
                count++;
        }

        return count;
    }

    public static void SetupLevelPremiumSettings(int levelIndex, int maxPremiumCoins, bool canRespawn)
    {
        maxPremiumCoins = Mathf.Max(0, maxPremiumCoins);

        if (levelIndex == 0)
            canRespawn = false;

        PlayerPrefs.SetInt(GetLevelMaxPremiumKey(levelIndex), maxPremiumCoins);
        PlayerPrefs.SetInt(GetLevelPremiumRespawnKey(levelIndex), canRespawn ? 1 : 0);

        string availableKey = GetLevelAvailablePremiumKey(levelIndex);
        if (!PlayerPrefs.HasKey(availableKey))
            PlayerPrefs.SetInt(availableKey, maxPremiumCoins);
        else
            PlayerPrefs.SetInt(availableKey, Mathf.Clamp(PlayerPrefs.GetInt(availableKey), 0, maxPremiumCoins));

        PlayerPrefs.Save();
    }

    public static void RemoveAvailablePremiumFromLevel(int levelIndex, int amount)
    {
        if (amount <= 0)
            return;

        int current = GetLevelAvailablePremiumCoins(levelIndex);
        PlayerPrefs.SetInt(GetLevelAvailablePremiumKey(levelIndex), Mathf.Max(0, current - amount));
        PlayerPrefs.DeleteKey(GetPremiumSpawnSelectionKey(levelIndex));
        PlayerPrefs.Save();
    }

    public static void RestorePremiumCoinsOnOpenedLevels(int levelCount, int exceptLevelIndex = -1)
    {
        for (int i = 0; i < levelCount; i++)
        {
            if (i == exceptLevelIndex)
                continue;

            if (!IsLevelUnlocked(i))
                continue;

            if (!LevelPremiumCanRespawn(i))
                continue;

            int current = GetLevelAvailablePremiumCoins(i);
            int maxCoins = GetLevelMaxPremiumCoins(i);

            if (current >= maxCoins)
                continue;

            PlayerPrefs.SetInt(GetLevelAvailablePremiumKey(i), current + 1);
            PlayerPrefs.DeleteKey(GetPremiumSpawnSelectionKey(i));
        }

        PlayerPrefs.Save();
    }

    public static bool IsBaseCoinCollected(int levelIndex, string coinId)
    {
        string key = GetBaseCoinKey(levelIndex, coinId);
        string versionKey = GetBaseCoinVersionKey(levelIndex, coinId);
        int levelResetVersion = GetBaseCoinLevelResetVersion(levelIndex);
        bool collected = PlayerPrefs.GetInt(key, 0) == 1;

        if (!collected)
        {
            if (!PlayerPrefs.HasKey(versionKey))
            {
                PlayerPrefs.SetInt(versionKey, levelResetVersion);
                PlayerPrefs.Save();
            }

            return false;
        }

        int coinVersion = PlayerPrefs.HasKey(versionKey)
            ? PlayerPrefs.GetInt(versionKey, levelResetVersion)
            : -1;

        if (coinVersion != levelResetVersion)
        {
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.SetInt(versionKey, levelResetVersion);
            PlayerPrefs.Save();
            return false;
        }

        return true;
    }

    public static void MarkBaseCoinCollected(int levelIndex, string coinId)
    {
        PlayerPrefs.SetInt(GetBaseCoinKey(levelIndex, coinId), 1);
        PlayerPrefs.SetInt(GetBaseCoinVersionKey(levelIndex, coinId), GetBaseCoinLevelResetVersion(levelIndex));
        PlayerPrefs.Save();
    }

    public static void ResetBaseCoinsForLevel(int levelIndex)
    {
        PlayerPrefs.SetInt(BaseCoinLevelResetKey + levelIndex, GetBaseCoinLevelResetVersion(levelIndex) + 1);
        PlayerPrefs.Save();
    }

    public static void ResetBaseCoinsForAllLevels(int levelCount)
    {
        for (int i = 0; i < levelCount; i++)
            PlayerPrefs.SetInt(BaseCoinLevelResetKey + i, GetBaseCoinLevelResetVersion(i) + 1);

        PlayerPrefs.Save();
    }

    public static void ResetLevelUnlocks()
    {
        PlayerPrefs.DeleteKey(UnlockedLevelKey);
        PlayerPrefs.Save();
    }

    public static void ResetPremiumAvailabilityForAllLevels(int levelCount)
    {
        for (int i = 0; i < levelCount; i++)
        {
            int maxCoins = GetLevelMaxPremiumCoins(i);
            PlayerPrefs.SetInt(GetLevelAvailablePremiumKey(i), maxCoins);
            PlayerPrefs.DeleteKey(GetPremiumSpawnSelectionKey(i));
        }

        PlayerPrefs.Save();
    }

    public static void ClearAllCurrencyWallets()
    {
        PlayerPrefs.DeleteKey(BaseCoinsKey);
        PlayerPrefs.DeleteKey(PremiumCoinsKey);
        PlayerPrefs.Save();
    }

    public static bool IsSkinUnlocked(int skinIndex)
    {
        if (UnlockAllContent)
            return true;

        if (skinIndex == 0)
            return true;

        return PlayerPrefs.GetInt(GetSkinUnlockedKey(skinIndex), 0) == 1;
    }

    public static void UnlockSkin(int skinIndex)
    {
        PlayerPrefs.SetInt(GetSkinUnlockedKey(skinIndex), 1);
        PlayerPrefs.Save();
    }

    public static void UnlockSkins(int skinCount)
    {
        for (int i = 0; i < skinCount; i++)
            PlayerPrefs.SetInt(GetSkinUnlockedKey(i), 1);

        PlayerPrefs.Save();
    }

    public static void ResetSkinUnlocks(int skinCount)
    {
        for (int i = 1; i < skinCount; i++)
            PlayerPrefs.DeleteKey(GetSkinUnlockedKey(i));

        PlayerPrefs.Save();
    }

    public static bool BuySkin(int skinIndex, CurrencyType currencyType, int cost)
    {
        if (IsSkinUnlocked(skinIndex))
            return true;

        bool paid = currencyType == CurrencyType.Premium
            ? SpendPremiumCoins(cost)
            : SpendBaseCoins(cost);

        if (!paid)
            return false;

        UnlockSkin(skinIndex);
        return true;
    }

    public static void SelectSkin(int skinIndex)
    {
        if (!IsSkinUnlocked(skinIndex))
            return;

        PlayerPrefs.SetInt(SelectedSkinKey, skinIndex);
        PlayerPrefs.Save();
    }

    public static string GetPremiumSpawnSelection(int levelIndex)
    {
        return PlayerPrefs.GetString(GetPremiumSpawnSelectionKey(levelIndex), "");
    }

    public static void SetPremiumSpawnSelection(int levelIndex, string value)
    {
        PlayerPrefs.SetString(GetPremiumSpawnSelectionKey(levelIndex), value);
        PlayerPrefs.Save();
    }

    public static void ClearPremiumSpawnSelection(int levelIndex)
    {
        PlayerPrefs.DeleteKey(GetPremiumSpawnSelectionKey(levelIndex));
        PlayerPrefs.Save();
    }

    private static int GetDefaultLevelMaxPremiumCoins(int levelIndex)
    {
        if (levelIndex <= 0)
            return 1;

        if (levelIndex == 1)
            return 1;

        if (levelIndex == 2)
            return 2;

        return 3;
    }

    private static bool GetDefaultLevelPremiumRespawn(int levelIndex)
    {
        return levelIndex > 0;
    }

    private static string GetBaseCoinPrefix(int levelIndex)
    {
        return "BaseCoinCollected_L" + levelIndex + "_";
    }

    private static string GetBaseCoinKey(int levelIndex, string coinId)
    {
        string key = GetBaseCoinPrefix(levelIndex) + coinId;
        PlayerPrefsKeyCache.RememberKey(key);
        return key;
    }

    private static int GetBaseCoinLevelResetVersion(int levelIndex)
    {
        return PlayerPrefs.GetInt(BaseCoinLevelResetKey + levelIndex, 0);
    }

    private static string GetBaseCoinVersionKey(int levelIndex, string coinId)
    {
        return GetBaseCoinPrefix(levelIndex) + coinId + "_Version";
    }

    private static string GetLevelAvailablePremiumKey(int levelIndex)
    {
        return "PremiumAvailable_L" + levelIndex;
    }

    private static string GetLevelMaxPremiumKey(int levelIndex)
    {
        return "PremiumMax_L" + levelIndex;
    }

    private static string GetLevelPremiumRespawnKey(int levelIndex)
    {
        return "PremiumRespawn_L" + levelIndex;
    }

    private static string GetPremiumSpawnSelectionKey(int levelIndex)
    {
        return "PremiumSpawnSelection_L" + levelIndex;
    }

    private static string GetSkinUnlockedKey(int skinIndex)
    {
        return "SkinUnlocked_" + skinIndex;
    }
}
