using System.Collections.Generic;
using UnityEngine;

public class PremiumCoinSpawner : MonoBehaviour
{
    [Header("Level")]
    public int levelIndex = -1;
    public int firstLevelBuildIndex = 2;

    [Header("Premium Coins")]
    public GameObject premiumCoinPrefab;
    public Transform[] spawnPoints;
    public int maxPremiumCoins = 3;
    public bool canRespawn = true;

    [Header("Coin Settings")]
    public bool usePointIndexAsCoinId = true;

    private void Start()
    {
        if (levelIndex < 0)
            levelIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex - firstLevelBuildIndex;

        if (levelIndex == 0)
            canRespawn = false;

        GameProgress.SetupLevelPremiumSettings(levelIndex, maxPremiumCoins, canRespawn);
        SpawnCoins();
    }

    public void SpawnCoins()
    {
        if (premiumCoinPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        int availableCoins = GameProgress.GetLevelAvailablePremiumCoins(levelIndex);
        availableCoins = Mathf.Clamp(availableCoins, 0, spawnPoints.Length);

        List<int> selectedIndexes = GetOrCreateSpawnSelection(availableCoins);

        for (int i = 0; i < selectedIndexes.Count; i++)
        {
            int pointIndex = selectedIndexes[i];

            if (pointIndex < 0 || pointIndex >= spawnPoints.Length)
                continue;

            Transform point = spawnPoints[pointIndex];
            if (point == null)
                continue;

            GameObject coinObject = Instantiate(premiumCoinPrefab, point.position, point.rotation, point);

            CoinPickup coin = coinObject.GetComponent<CoinPickup>();
            if (coin != null)
            {
                coin.currencyType = CurrencyType.Premium;
                coin.levelIndex = levelIndex;

                if (usePointIndexAsCoinId)
                    coin.coinId = "PremiumPoint_" + pointIndex;
            }
        }
    }

    private List<int> GetOrCreateSpawnSelection(int count)
    {
        string saved = GameProgress.GetPremiumSpawnSelection(levelIndex);
        List<int> selected = ParseSelection(saved);

        if (selected.Count == count && SelectionIsValid(selected))
            return selected;

        selected = CreateRandomSelection(count);
        GameProgress.SetPremiumSpawnSelection(levelIndex, SerializeSelection(selected));
        return selected;
    }

    private List<int> CreateRandomSelection(int count)
    {
        List<int> allIndexes = new List<int>();
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] != null)
                allIndexes.Add(i);
        }

        for (int i = 0; i < allIndexes.Count; i++)
        {
            int randomIndex = Random.Range(i, allIndexes.Count);
            int temp = allIndexes[i];
            allIndexes[i] = allIndexes[randomIndex];
            allIndexes[randomIndex] = temp;
        }

        int finalCount = Mathf.Clamp(count, 0, allIndexes.Count);
        return allIndexes.GetRange(0, finalCount);
    }

    private bool SelectionIsValid(List<int> selected)
    {
        for (int i = 0; i < selected.Count; i++)
        {
            if (selected[i] < 0 || selected[i] >= spawnPoints.Length)
                return false;
        }

        return true;
    }

    private List<int> ParseSelection(string value)
    {
        List<int> selected = new List<int>();

        if (string.IsNullOrEmpty(value))
            return selected;

        string[] split = value.Split(',');
        for (int i = 0; i < split.Length; i++)
        {
            int parsed;
            if (int.TryParse(split[i], out parsed))
                selected.Add(parsed);
        }

        return selected;
    }

    private string SerializeSelection(List<int> selected)
    {
        return string.Join(",", selected);
    }
}
