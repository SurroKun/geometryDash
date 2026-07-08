using UnityEditor;
using UnityEngine;

public class EconomyDebugWindow : EditorWindow
{
    private int levelCount = 16;
    private int skinCount = 60;
    private int giveBaseCoins = 1000;
    private int givePremiumCoins = 100;

    [MenuItem("Window/GeometrySurf/Economy Debug")]
    public static void Open()
    {
        GetWindow<EconomyDebugWindow>("Economy Debug");
    }

    private void OnGUI()
    {
        GUILayout.Label("Economy Debug Tools", EditorStyles.boldLabel);
        GUILayout.Space(8);

        levelCount = EditorGUILayout.IntField("Level Count", levelCount);
        skinCount = EditorGUILayout.IntField("Skin Count", skinCount);

        GUILayout.Space(12);
        GUILayout.Label("Coins", EditorStyles.boldLabel);

        if (GUILayout.Button("Reset Premium Coins On All Levels"))
        {
            GameProgress.ResetPremiumAvailabilityForAllLevels(levelCount);
            Debug.Log("Premium coins reset on all levels.");
        }

        if (GUILayout.Button("Reset Base Coins On All Levels"))
        {
            GameProgress.ResetBaseCoinsForAllLevels(levelCount);
            Debug.Log("Base coins reset on all levels.");
        }

        if (GUILayout.Button("Clear Wallet Currency"))
        {
            GameProgress.ClearAllCurrencyWallets();
            Debug.Log("Wallet currency cleared.");
        }

        GUILayout.BeginHorizontal();
        giveBaseCoins = EditorGUILayout.IntField("Base", giveBaseCoins);
        if (GUILayout.Button("Set Base Coins"))
        {
            GameProgress.SetBaseCoins(giveBaseCoins);
            Debug.Log("Base coins set to: " + giveBaseCoins);
        }
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        givePremiumCoins = EditorGUILayout.IntField("Premium", givePremiumCoins);
        if (GUILayout.Button("Set Premium Coins"))
        {
            GameProgress.SetPremiumCoins(givePremiumCoins);
            Debug.Log("Premium coins set to: " + givePremiumCoins);
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(12);
        GUILayout.Label("Unlocks", EditorStyles.boldLabel);

        if (GUILayout.Button("Unlock All Levels"))
        {
            GameProgress.UnlockLevel(levelCount - 1);
            Debug.Log("All levels unlocked.");
        }

        if (GUILayout.Button("Reset Level Unlocks"))
        {
            GameProgress.ResetLevelUnlocks();
            Debug.Log("Level unlocks reset. Default unlocked levels stay available.");
        }

        if (GUILayout.Button("Unlock All Skins"))
        {
            GameProgress.UnlockSkins(skinCount);
            Debug.Log("All skins unlocked.");
        }

        if (GUILayout.Button("Reset Skin Unlocks"))
        {
            GameProgress.ResetSkinUnlocks(skinCount);
            Debug.Log("Skin unlocks reset. Skin 0 stays free.");
        }

        GUILayout.Space(12);
        GUILayout.Label("Testing Switch", EditorStyles.boldLabel);

        bool unlockAll = GameProgress.UnlockAllContent;
        bool newUnlockAll = EditorGUILayout.Toggle("Unlock Everything Flag", unlockAll);

        if (newUnlockAll != unlockAll)
        {
            GameProgress.UnlockAllContent = newUnlockAll;
            Debug.Log("Unlock Everything Flag: " + newUnlockAll);
        }

        bool practiceCurrency = GameProgress.CompleteCurrencyInPracticeMode;
        bool newPracticeCurrency = EditorGUILayout.Toggle("Currency In Practice Mode", practiceCurrency);

        if (newPracticeCurrency != practiceCurrency)
        {
            GameProgress.CompleteCurrencyInPracticeMode = newPracticeCurrency;
            Debug.Log("Currency In Practice Mode: " + newPracticeCurrency);
        }
    }
}
