using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-5000)]
public class GhostRunPlayback : MonoBehaviour
{
    private const string UntaggedTag = "Untagged";

    [Header("References")]
    public Transform ghostRoot;
    public PlayerSkinSwitcher skinSwitcher;

    [Header("Playback")]
    public bool playSavedRunOnStart = true;
    public bool hideWhenNoRun = true;
    public bool blockPlaybackOnRecorderRoot = true;
    public bool sanitizeGhostOnStart = true;
    public bool forceUntaggedOnStart = true;
    public bool disablePhysicsOnStart = true;
    public bool disableGameplayScriptsOnStart = true;
    public bool disableCamerasOnStart = true;
    public float startDelay = 0f;

    [Header("Ghost Visuals")]
    public bool applyGhostVisualStyle = true;
    public Color ghostTint = new Color(0.35f, 0.75f, 1f, 0.42f);
    public bool forceTransparentMaterials = true;
    public bool updateSkinFromFrames = true;
    public bool logSkinChanges = false;

    private GhostRunData runData;
    private Renderer[] ghostRenderers;
    private Collider[] ghostColliders;
    private readonly Dictionary<Renderer, bool> rendererDefaultEnabled = new Dictionary<Renderer, bool>();
    private float playbackTime = 0f;
    private bool isPlaying = false;
    private int appliedSkinIndex = -1;

    public bool HasRun => runData != null && runData.HasFrames();
    public bool IsPlaying => isPlaying;
    public float PlaybackTime => playbackTime;
    public float RunDuration => runData != null ? runData.duration : 0f;

    void Awake()
    {
        ResolveReferences();
        CaptureRendererDefaultStates();

        if (IsLikelyRealPlayerRoot())
        {
            Debug.LogWarning(
                "GhostRunPlayback is on a recorder/player root. " +
                "Move it to a separate ghost visual object."
            );

            enabled = false;
            return;
        }

        if (sanitizeGhostOnStart)
            SanitizeGhostObject();

        RefreshVisualCache();
        ApplyGhostVisualStyle();
    }

    void Start()
    {
        if (playSavedRunOnStart)
            Play(GhostRunStorage.LoadForCurrentScene());
    }

    void Update()
    {
        if (!isPlaying || runData == null || !runData.HasFrames())
            return;

        playbackTime += Time.deltaTime;

        float sampleTime = playbackTime - startDelay;
        if (sampleTime < 0f)
        {
            SetGhostVisible(false);
            return;
        }

        SetGhostVisible(true);
        ApplyFrameAtTime(sampleTime);

        if (sampleTime >= runData.duration)
            isPlaying = false;
    }

    public void Play(GhostRunData data)
    {
        runData = data;
        playbackTime = 0f;
        isPlaying = runData != null && runData.HasFrames();

        if (!isPlaying)
        {
            SetGhostVisible(!hideWhenNoRun);
            return;
        }

        ApplySkin(runData.skinIndex);

        ApplyFrame(runData.frames[0]);
        SetGhostVisible(startDelay <= 0f);
    }

    public void PlaySavedRun()
    {
        Play(GhostRunStorage.LoadForCurrentScene());
    }

    public void Stop()
    {
        isPlaying = false;
        SetGhostVisible(false);
    }

    public void SetVisible(bool value)
    {
        SetGhostVisible(value);
    }

    public void ApplySnapshot(RacePlayerSnapshot snapshot)
    {
        if (snapshot == null)
            return;

        ApplySkin(snapshot.skinIndex);
        ghostRoot.position = snapshot.position;
        ghostRoot.rotation = snapshot.rotation;
        SetGhostVisible(snapshot.alive);
    }

    private void ResolveReferences()
    {
        if (ghostRoot == null)
            ghostRoot = transform;

        if (skinSwitcher == null)
            skinSwitcher = GetComponentInChildren<PlayerSkinSwitcher>(true);

        RefreshVisualCache();
    }

    private void RefreshVisualCache()
    {
        if (ghostRoot == null)
            return;

        ghostRenderers = ghostRoot.GetComponentsInChildren<Renderer>(true);
        ghostColliders = ghostRoot.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            Renderer ghostRenderer = ghostRenderers[i];
            if (ghostRenderer != null && !rendererDefaultEnabled.ContainsKey(ghostRenderer))
                rendererDefaultEnabled.Add(ghostRenderer, ghostRenderer.enabled);
        }
    }

    private void CaptureRendererDefaultStates()
    {
        if (ghostRenderers == null)
            return;

        rendererDefaultEnabled.Clear();

        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            Renderer ghostRenderer = ghostRenderers[i];
            if (ghostRenderer != null)
                rendererDefaultEnabled.Add(ghostRenderer, ghostRenderer.enabled);
        }
    }

    private bool IsLikelyRealPlayerRoot()
    {
        if (!blockPlaybackOnRecorderRoot || ghostRoot == null)
            return false;

        GhostRunRecorder recorder = ghostRoot.GetComponentInChildren<GhostRunRecorder>(true);
        return recorder != null && recorder.enabled;
    }

    private void SanitizeGhostObject()
    {
        if (forceUntaggedOnStart)
            SetTagRecursively(ghostRoot, UntaggedTag);

        if (disableGameplayScriptsOnStart)
            DisableGameplayScripts();

        if (disableCamerasOnStart)
            DisableCameras();

        if (disablePhysicsOnStart)
            DisableGhostPhysics();
    }

    private void SetTagRecursively(Transform root, string tagName)
    {
        if (root == null)
            return;

        root.gameObject.tag = tagName;

        for (int i = 0; i < root.childCount; i++)
            SetTagRecursively(root.GetChild(i), tagName);
    }

    private void DisableGameplayScripts()
    {
        DisableComponents<PlayerMove>();
        DisableComponents<PlayerCollision>();
        DisableComponents<DeathScript>();
        DisableComponents<PracticeModeManager>();
        DisableComponents<PlayerDeathEffect>();
        DisableComponents<PlayerGravityFlip>();
        DisableComponents<PlayerJumpHeightBonus>();
        DisableComponents<PlayerSpeedBoostBonus>();
        DisableComponents<PlayerJumpSpeedDashBonus>();
        DisableComponents<GroundTrailFromPoints>();
        DisableComponents<GhostRunRecorder>();
    }

    private void DisableComponents<T>() where T : Behaviour
    {
        if (ghostRoot == null)
            return;

        T[] components = ghostRoot.GetComponentsInChildren<T>(true);
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
                components[i].enabled = false;
        }
    }

    private void DisableCameras()
    {
        DisableComponents<Camera>();
        DisableComponents<AudioListener>();
    }

    private void DisableGhostPhysics()
    {
        if (ghostRoot == null)
            return;

        Rigidbody[] bodies = ghostRoot.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            if (bodies[i] != null)
                bodies[i].isKinematic = true;
        }

        Collider[] colliders = ghostRoot.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }
    }

    private void ApplyFrameAtTime(float time)
    {
        List<GhostRunFrame> frames = runData.frames;

        if (time <= frames[0].time)
        {
            ApplyFrame(frames[0]);
            return;
        }

        int lastIndex = frames.Count - 1;
        if (time >= frames[lastIndex].time)
        {
            ApplyFrame(frames[lastIndex]);
            return;
        }

        for (int i = 0; i < lastIndex; i++)
        {
            GhostRunFrame a = frames[i];
            GhostRunFrame b = frames[i + 1];

            if (time < a.time || time > b.time)
                continue;

            float span = b.time - a.time;
            float t = span > 0f ? (time - a.time) / span : 0f;

            ghostRoot.position = Vector3.Lerp(a.position, b.position, t);
            ghostRoot.rotation = Quaternion.Slerp(a.rotation, b.rotation, t);
            SetGhostVisible(a.alive || b.alive);
            return;
        }
    }

    private void ApplyFrame(GhostRunFrame frame)
    {
        RacePlayerSnapshot snapshot = RacePlayerSnapshot.FromGhostFrame(frame);

        if (!updateSkinFromFrames && snapshot != null)
            snapshot.skinIndex = appliedSkinIndex >= 0 ? appliedSkinIndex : runData.skinIndex;

        ApplySnapshot(snapshot);
    }

    private void ApplySkin(int skinIndex)
    {
        if (skinSwitcher == null || appliedSkinIndex == skinIndex)
            return;

        skinSwitcher.ApplySkin(skinIndex);
        appliedSkinIndex = skinIndex;

        if (logSkinChanges)
            Debug.Log("Ghost skin applied: " + skinIndex);

        RefreshVisualCache();
        ApplyGhostVisualStyle();
    }

    private void ApplyGhostVisualStyle()
    {
        if (!applyGhostVisualStyle || ghostRenderers == null)
            return;

        for (int i = 0; i < ghostRenderers.Length; i++)
        {
            Renderer ghostRenderer = ghostRenderers[i];
            if (ghostRenderer == null)
                continue;

            Material[] materials = ghostRenderer.materials;
            for (int j = 0; j < materials.Length; j++)
                ApplyGhostMaterialStyle(materials[j]);
        }
    }

    private void ApplyGhostMaterialStyle(Material material)
    {
        if (material == null)
            return;

        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", ghostTint);

        if (material.HasProperty("_Color"))
            material.SetColor("_Color", ghostTint);

        if (!forceTransparentMaterials)
            return;

        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void SetGhostVisible(bool value)
    {
        if (ghostRenderers != null)
        {
            for (int i = 0; i < ghostRenderers.Length; i++)
            {
                Renderer ghostRenderer = ghostRenderers[i];
                if (ghostRenderer == null)
                    continue;

                bool defaultEnabled;
                if (!rendererDefaultEnabled.TryGetValue(ghostRenderer, out defaultEnabled))
                    defaultEnabled = true;

                ghostRenderer.enabled = value &&
                                        defaultEnabled &&
                                        ghostRenderer.gameObject.activeInHierarchy;
            }
        }

        if (ghostColliders != null)
        {
            for (int i = 0; i < ghostColliders.Length; i++)
            {
                if (ghostColliders[i] != null)
                    ghostColliders[i].enabled = false;
            }
        }
    }
}
