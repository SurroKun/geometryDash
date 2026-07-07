using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class BonusEffectInstaller : EditorWindow
{
    [Header("Prefabs")]
    private GameObject jumpEffectPrefab;
    private GameObject speedEffectPrefab;
    private GameObject gravityEffectPrefab;

    [Header("Local Transform")]
    private Vector3 localPosition = Vector3.zero;
    private Vector3 localRotation = Vector3.zero;
    private Vector3 localScale = Vector3.one;

    [MenuItem("Window/GeometrySurf/Bonus Effect Installer")]
    public static void Open()
    {
        GetWindow<BonusEffectInstaller>("Bonus Effects");
    }

    private void OnGUI()
    {
        GUILayout.Label("Install bonus effects by tag", EditorStyles.boldLabel);

        GUILayout.Space(8);

        jumpEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "AirJump Effect",
            jumpEffectPrefab,
            typeof(GameObject),
            false
        );

        speedEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "SpeedBonus Effect",
            speedEffectPrefab,
            typeof(GameObject),
            false
        );

        gravityEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "GravityBonus Effect",
            gravityEffectPrefab,
            typeof(GameObject),
            false
        );

        GUILayout.Space(10);

        localPosition = EditorGUILayout.Vector3Field("Local Position", localPosition);
        localRotation = EditorGUILayout.Vector3Field("Local Rotation", localRotation);
        localScale = EditorGUILayout.Vector3Field("Local Scale", localScale);

        GUILayout.Space(10);

        if (GUILayout.Button("Install All Bonus Effects"))
            InstallAllEffects();
    }

    private void InstallAllEffects()
    {
        int totalInstalled = 0;
        int totalSkipped = 0;

        InstallForTag(
            "AirJump",
            jumpEffectPrefab,
            ref totalInstalled,
            ref totalSkipped
        );

        InstallForTag(
            "SpeedBonus",
            speedEffectPrefab,
            ref totalInstalled,
            ref totalSkipped
        );

        InstallForTag(
            "GravityBonus",
            gravityEffectPrefab,
            ref totalInstalled,
            ref totalSkipped
        );

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log(
            "Bonus effect install complete. Installed: " +
            totalInstalled +
            " | Skipped existing: " +
            totalSkipped
        );
    }

    private void InstallForTag(
        string tagName,
        GameObject effectPrefab,
        ref int totalInstalled,
        ref int totalSkipped
    )
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning("Skipped " + tagName + ": effect prefab is not assigned.");
            return;
        }

        int installedCount = 0;
        int skippedCount = 0;

        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform target in allTransforms)
        {
            if (target == null)
                continue;

            if (!target.gameObject.scene.IsValid())
                continue;

            if (EditorUtility.IsPersistent(target.gameObject))
                continue;

            if (!HasTag(target.gameObject, tagName))
                continue;

            if (target.GetComponentInChildren<BonusEffectController>(true) != null)
            {
                skippedCount++;
                continue;
            }

            GameObject effectInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                effectPrefab,
                target
            );

            if (effectInstance == null)
                continue;

            Undo.RegisterCreatedObjectUndo(effectInstance, "Install Bonus Effect");

            effectInstance.transform.localPosition = localPosition;
            effectInstance.transform.localEulerAngles = localRotation;
            effectInstance.transform.localScale = localScale;

            installedCount++;
        }

        totalInstalled += installedCount;
        totalSkipped += skippedCount;

        Debug.Log(
            tagName +
            " effects installed: " +
            installedCount +
            " | skipped existing: " +
            skippedCount
        );
    }

    private bool HasTag(GameObject obj, string tagName)
    {
        try
        {
            return obj.CompareTag(tagName);
        }
        catch
        {
            Debug.LogWarning(
                "Tag does not exist: " +
                tagName +
                ". Create it in Unity Tags first."
            );

            return false;
        }
    }
}