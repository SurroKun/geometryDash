using UnityEngine;

public class PlayerSkinVFXController : MonoBehaviour
{
    [System.Serializable]
    public class SkinVFXProfile
    {
        [Header("Skin")]
        public int skinIndex = 0;

        [Header("Trail")]
        public GameObject trailPrefabOverride;
        public Material trailMaterialOverride;
        public Material[] trailMaterialsOverride;
        public Transform[] trailSpawnPointsOverride;
        public bool spawnTrailOnlyWhenGrounded = true;

        [Header("Death")]
        public Transform deathFragmentsRootOverride;
        public Material deathMaterialOverride;
        public Material[] deathMaterialsOverride;

        [Header("Animation")]
        public SkinAnimationMode animationMode = SkinAnimationMode.Default;
    }

    public enum SkinAnimationMode
    {
        Default,
        Rolling,
        Special
    }

    [Header("References")]
    public PlayerSkinSwitcher skinSwitcher;
    public GroundTrailFromPoints trail;
    public PlayerDeathEffect deathEffect;
    public SkinRollVisualController rollVisualController;

    [Header("Defaults")]
    public GameObject defaultTrailPrefab;
    public Material defaultTrailMaterial;
    public Transform[] defaultTrailSpawnPoints;
    public bool defaultSpawnTrailOnlyWhenGrounded = true;
    public Transform defaultDeathFragmentsRoot;
    public Material defaultDeathMaterial;

    [Header("Profiles")]
    public SkinVFXProfile[] profiles;

    void Start()
    {
        ApplyCurrentSkinProfile();
    }

    public void ApplyCurrentSkinProfile()
    {
        if (skinSwitcher == null)
            return;

        int skinIndex = skinSwitcher.GetCurrentSkinIndex();
        SkinVFXProfile profile = GetProfile(skinIndex);

        ApplyTrail(profile);
        ApplyDeath(profile);
        ApplyAnimation(profile);
    }

    public SkinAnimationMode GetAnimationModeForCurrentSkin()
    {
        if (skinSwitcher == null)
            return SkinAnimationMode.Default;

        SkinVFXProfile profile = GetProfile(skinSwitcher.GetCurrentSkinIndex());
        return profile != null ? profile.animationMode : SkinAnimationMode.Default;
    }

    private SkinVFXProfile GetProfile(int skinIndex)
    {
        if (profiles == null || profiles.Length == 0)
            return null;

        for (int i = 0; i < profiles.Length; i++)
        {
            if (profiles[i] != null && profiles[i].skinIndex == skinIndex)
                return profiles[i];
        }

        return null;
    }

    private void ApplyTrail(SkinVFXProfile profile)
    {
        if (trail == null)
            return;

        GameObject finalTrailPrefab = defaultTrailPrefab;
        Material finalTrailMaterial = defaultTrailMaterial;
        Material[] finalTrailMaterials = null;
        Transform[] finalSpawnPoints = defaultTrailSpawnPoints;
        bool finalSpawnOnlyWhenGrounded = defaultSpawnTrailOnlyWhenGrounded;

        if (profile != null)
        {
            if (profile.trailPrefabOverride != null)
                finalTrailPrefab = profile.trailPrefabOverride;

            if (profile.trailMaterialsOverride != null && profile.trailMaterialsOverride.Length > 0)
            {
                finalTrailMaterials = profile.trailMaterialsOverride;
                finalTrailMaterial = null;
            }
            else if (profile.trailMaterialOverride != null)
            {
                finalTrailMaterial = profile.trailMaterialOverride;
            }

            if (profile.trailSpawnPointsOverride != null && profile.trailSpawnPointsOverride.Length > 0)
                finalSpawnPoints = profile.trailSpawnPointsOverride;

            finalSpawnOnlyWhenGrounded = profile.spawnTrailOnlyWhenGrounded;
        }

        trail.SetTrailPrefab(finalTrailPrefab);
        trail.SetSpawnPoints(finalSpawnPoints);
        trail.SetSpawnOnlyWhenGrounded(finalSpawnOnlyWhenGrounded);

        if (finalTrailMaterials != null && finalTrailMaterials.Length > 0)
            trail.SetOverrideMaterials(finalTrailMaterials);
        else if (finalTrailMaterial != null)
            trail.SetOverrideMaterial(finalTrailMaterial);
        else
            trail.ClearOverrideMaterial();

        trail.ResetTrail();
    }

    private void ApplyDeath(SkinVFXProfile profile)
    {
        if (deathEffect == null)
            return;

        Transform finalFragmentsRoot = defaultDeathFragmentsRoot;
        Material finalDeathMaterial = defaultDeathMaterial;
        Material[] finalDeathMaterials = null;

        if (profile != null)
        {
            if (profile.deathFragmentsRootOverride != null)
                finalFragmentsRoot = profile.deathFragmentsRootOverride;

            if (profile.deathMaterialsOverride != null && profile.deathMaterialsOverride.Length > 0)
            {
                finalDeathMaterials = profile.deathMaterialsOverride;
                finalDeathMaterial = null;
            }
            else if (profile.deathMaterialOverride != null)
            {
                finalDeathMaterial = profile.deathMaterialOverride;
            }
        }

        deathEffect.SetOverrideFragmentsRoot(finalFragmentsRoot);

        if (finalDeathMaterials != null && finalDeathMaterials.Length > 0)
            deathEffect.SetOverrideMaterials(finalDeathMaterials);
        else if (finalDeathMaterial != null)
            deathEffect.SetOverrideMaterial(finalDeathMaterial);
        else
            deathEffect.ClearOverrideMaterial();
    }

    private void ApplyAnimation(SkinVFXProfile profile)
    {
        if (rollVisualController == null)
            return;

        SkinAnimationMode mode = SkinAnimationMode.Default;

        if (profile != null)
            mode = profile.animationMode;

        rollVisualController.SetMode(mode == SkinAnimationMode.Rolling);
        rollVisualController.RefreshTarget();
    }
}