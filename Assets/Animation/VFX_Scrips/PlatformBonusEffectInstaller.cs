using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class PlatformBonusEffectInstaller : EditorWindow
{
    private GameObject jumpPlatformEffectPrefab;
    private GameObject speedPlatformEffectPrefab;
    private GameObject gravityPlatformEffectPrefab;
    private GameObject flightPlatformEffectPrefab;

    private Vector3 localPosition = Vector3.zero;
    private Vector3 localRotation = Vector3.zero;
    private Vector3 localScale = Vector3.one;

    [MenuItem("Window/GeometrySurf/Platform Bonus Effect Installer")]
    public static void Open()
    {
        GetWindow<PlatformBonusEffectInstaller>("Platform Effects");
    }

    private void OnGUI()
    {
        GUILayout.Label("Install idle effects on bonus platforms", EditorStyles.boldLabel);

        GUILayout.Space(8);

        jumpPlatformEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Jump Platform Effect",
            jumpPlatformEffectPrefab,
            typeof(GameObject),
            false
        );

        speedPlatformEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Speed Platform Effect",
            speedPlatformEffectPrefab,
            typeof(GameObject),
            false
        );

        gravityPlatformEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Gravity Platform Effect",
            gravityPlatformEffectPrefab,
            typeof(GameObject),
            false
        );

        flightPlatformEffectPrefab = (GameObject)EditorGUILayout.ObjectField(
            "Flight Platform Effect",
            flightPlatformEffectPrefab,
            typeof(GameObject),
            false
        );

        GUILayout.Space(10);

        localPosition = EditorGUILayout.Vector3Field("Local Position", localPosition);
        localRotation = EditorGUILayout.Vector3Field("Local Rotation", localRotation);
        localScale = EditorGUILayout.Vector3Field("Local Scale", localScale);

        GUILayout.Space(10);

        if (GUILayout.Button("Install On All Bonus Platforms"))
            InstallAllEffects();
    }

    private void InstallAllEffects()
    {
        int totalInstalled = 0;
        int totalSkipped = 0;

        InstallForComponent<JumpHeightPlatform>(
            jumpPlatformEffectPrefab,
            "Jump Platform",
            ref totalInstalled,
            ref totalSkipped
        );

        InstallForComponent<SpeedBoostPlatform>(
            speedPlatformEffectPrefab,
            "Speed Platform",
            ref totalInstalled,
            ref totalSkipped
        );

        InstallForComponent<GravityFlipBonus>(
            gravityPlatformEffectPrefab,
            "Gravity Platform",
            ref totalInstalled,
            ref totalSkipped
        );

        InstallForComponent<FlightModeBonusPlatform>(
            flightPlatformEffectPrefab,
            "Flight Platform",
            ref totalInstalled,
            ref totalSkipped
        );

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log(
            "Platform bonus effect install complete. Installed: " +
            totalInstalled +
            " | Skipped existing: " +
            totalSkipped
        );
    }

    private void InstallForComponent<T>(
        GameObject effectPrefab,
        string label,
        ref int totalInstalled,
        ref int totalSkipped
    ) where T : Component
    {
        if (effectPrefab == null)
        {
            Debug.LogWarning("Skipped " + label + ": effect prefab is not assigned.");
            return;
        }

        int installedCount = 0;
        int skippedCount = 0;

        T[] targets = Resources.FindObjectsOfTypeAll<T>();

        foreach (T targetComponent in targets)
        {
            if (targetComponent == null)
                continue;

            GameObject target = targetComponent.gameObject;

            if (!target.scene.IsValid())
                continue;

            if (EditorUtility.IsPersistent(target))
                continue;

            if (target.GetComponentInChildren<BonusEffectController>(true) != null)
            {
                skippedCount++;
                continue;
            }

            GameObject effectInstance = (GameObject)PrefabUtility.InstantiatePrefab(
                effectPrefab,
                target.transform
            );

            if (effectInstance == null)
                continue;

            Undo.RegisterCreatedObjectUndo(effectInstance, "Install Platform Bonus Effect");

            effectInstance.transform.localPosition = localPosition;
            effectInstance.transform.localEulerAngles = localRotation;
            effectInstance.transform.localScale = localScale;

            installedCount++;
        }

        totalInstalled += installedCount;
        totalSkipped += skippedCount;

        Debug.Log(
            label +
            " effects installed: " +
            installedCount +
            " | skipped existing: " +
            skippedCount
        );
    }
}