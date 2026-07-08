using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class BaseCoinIdAssigner : EditorWindow
{
    private string idPrefix = "B_";
    private int startNumber = 1;
    private int digits = 3;
    private bool onlyBaseCoins = true;

    [MenuItem("Window/GeometrySurf/Base Coin ID Assigner")]
    public static void Open()
    {
        GetWindow<BaseCoinIdAssigner>("Base Coin IDs");
    }

    private void OnGUI()
    {
        GUILayout.Label("Assign unique IDs to coins in current scene", EditorStyles.boldLabel);
        GUILayout.Space(8);

        idPrefix = EditorGUILayout.TextField("ID Prefix", idPrefix);
        startNumber = EditorGUILayout.IntField("Start Number", startNumber);
        digits = EditorGUILayout.IntSlider("Digits", digits, 1, 5);
        onlyBaseCoins = EditorGUILayout.Toggle("Only Base Coins", onlyBaseCoins);

        GUILayout.Space(12);

        if (GUILayout.Button("Assign IDs In Current Scene"))
            AssignIds();
    }

    private void AssignIds()
    {
        CoinPickup[] coins = FindObjectsOfType<CoinPickup>(true);
        int number = Mathf.Max(0, startNumber);
        int changed = 0;

        System.Array.Sort(coins, CompareCoinsByHierarchy);

        for (int i = 0; i < coins.Length; i++)
        {
            CoinPickup coin = coins[i];

            if (coin == null)
                continue;

            if (!coin.gameObject.scene.IsValid())
                continue;

            if (EditorUtility.IsPersistent(coin.gameObject))
                continue;

            if (onlyBaseCoins && coin.currencyType != CurrencyType.Base)
                continue;

            Undo.RecordObject(coin, "Assign Base Coin ID");

            coin.coinId = idPrefix + number.ToString("D" + digits);
            EditorUtility.SetDirty(coin);

            number++;
            changed++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Assigned coin IDs: " + changed);
    }

    private int CompareCoinsByHierarchy(CoinPickup left, CoinPickup right)
    {
        if (left == null && right == null)
            return 0;

        if (left == null)
            return 1;

        if (right == null)
            return -1;

        string leftPath = GetHierarchyPath(left.transform);
        string rightPath = GetHierarchyPath(right.transform);
        return string.CompareOrdinal(leftPath, rightPath);
    }

    private string GetHierarchyPath(Transform target)
    {
        string path = target.name;
        Transform parent = target.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }
}
