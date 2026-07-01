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

            if (HasMaterials(profile.trailMaterialsOverride))
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

        ApplyTrailMaterial(finalTrailMaterial, finalTrailMaterials);

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

            if (HasMaterials(profile.deathMaterialsOverride))
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

        ApplyDeathMaterial(finalDeathMaterial, finalDeathMaterials);
    }

    private void ApplyAnimation(SkinVFXProfile profile)
    {
        if (rollVisualController == null)
            return;

        SkinAnimationMode mode = GetProfileAnimationMode(profile);

        rollVisualController.SetMode(mode == SkinAnimationMode.Rolling);
        rollVisualController.RefreshTarget();
    }

    private bool HasMaterials(Material[] materials)
    {
        return materials != null && materials.Length > 0;
    }

    private void ApplyTrailMaterial(
        Material material,
        Material[] materials
    )
    {
        if (HasMaterials(materials))
            trail.SetOverrideMaterials(materials);
        else if (material != null)
            trail.SetOverrideMaterial(material);
        else
            trail.ClearOverrideMaterial();
    }

    private void ApplyDeathMaterial(
        Material material,
        Material[] materials
    )
    {
        if (HasMaterials(materials))
            deathEffect.SetOverrideMaterials(materials);
        else if (material != null)
            deathEffect.SetOverrideMaterial(material);
        else
            deathEffect.ClearOverrideMaterial();
    }

    private SkinAnimationMode GetProfileAnimationMode(SkinVFXProfile profile)
    {
        return profile != null
            ? profile.animationMode
            : SkinAnimationMode.Default;
    }
}
