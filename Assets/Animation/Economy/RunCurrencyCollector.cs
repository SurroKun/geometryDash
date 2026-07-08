using System.Collections.Generic;
using UnityEngine;

public class RunCurrencyCollector : MonoBehaviour
{
    private static RunCurrencyCollector instance;

    private readonly HashSet<string> collectedPremiumIds = new HashSet<string>();

    public static RunCurrencyCollector Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<RunCurrencyCollector>();

            if (instance == null)
            {
                GameObject collectorObject = new GameObject("RunCurrencyCollector");
                instance = collectorObject.AddComponent<RunCurrencyCollector>();
            }

            return instance;
        }
    }

    private void Awake()
    {
        instance = this;
    }

    public void CollectPremiumCoin(string coinId)
    {
        if (string.IsNullOrEmpty(coinId))
            return;

        collectedPremiumIds.Add(coinId);
    }

    public bool HasCollectedPremiumCoin(string coinId)
    {
        return collectedPremiumIds.Contains(coinId);
    }

    public int GetCollectedPremiumCount()
    {
        return collectedPremiumIds.Count;
    }

    public void CommitLevelComplete(int levelIndex, int openedLevelCount)
    {
        int premiumCount = collectedPremiumIds.Count;

        if (premiumCount > 0)
        {
            GameProgress.AddPremiumCoins(premiumCount);
            GameProgress.RemoveAvailablePremiumFromLevel(levelIndex, premiumCount);
        }

        GameProgress.ResetBaseCoinsForLevel(levelIndex);
        GameProgress.ClearPremiumSpawnSelection(levelIndex);
        GameProgress.RestorePremiumCoinsOnOpenedLevels(openedLevelCount, levelIndex);

        collectedPremiumIds.Clear();
    }

    public void ResetRun()
    {
        collectedPremiumIds.Clear();
    }
}
