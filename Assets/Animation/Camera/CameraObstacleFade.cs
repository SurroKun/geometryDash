using System.Collections.Generic;
using UnityEngine;

public class CameraObstacleFade : MonoBehaviour
{
    private class FadeData
    {
        public Material[] originalMaterials;
        public Material[] fadeMaterials;
        public UnityEngine.Rendering.ShadowCastingMode originalShadowMode;
        public float alpha = 1f;
        public bool isBlocking;
    }

    [Header("References")]
    public Transform target;

    [Header("Detection")]
    public LayerMask obstacleLayers = ~0;
    public float sphereRadius = 0.6f;
    public float targetPadding = 0.5f;
    public bool includeTriggerColliders = true;

    [Header("Fade")]
    [Range(0.05f, 1f)]
    public float transparentAlpha = 0.25f;
    public float fadeToTransparentSpeed = 8f;
    public float fadeToOpaqueSpeed = 10f;
    public bool disableShadowsWhileTransparent = true;

    private readonly Dictionary<Renderer, FadeData> fadeObjects =
        new Dictionary<Renderer, FadeData>();

    private readonly List<Renderer> renderersToRemove = new List<Renderer>();

    private void LateUpdate()
    {
        if (target == null)
            return;

        ClearBlockingFlags();
        FindObstacles();
        UpdateFadeObjects();
    }

    private void ClearBlockingFlags()
    {
        foreach (var pair in fadeObjects)
            pair.Value.isBlocking = false;
    }

    private void FindObstacles()
    {
        Vector3 start = transform.position;
        Vector3 end = target.position;
        Vector3 direction = end - start;

        float distance = direction.magnitude - targetPadding;

        if (distance <= 0f)
            return;

        direction.Normalize();

        QueryTriggerInteraction triggerMode = includeTriggerColliders
            ? QueryTriggerInteraction.Collide
            : QueryTriggerInteraction.Ignore;

        RaycastHit[] hits = Physics.SphereCastAll(
            start,
            sphereRadius,
            direction,
            distance,
            obstacleLayers,
            triggerMode
        );

        for (int i = 0; i < hits.Length; i++)
        {
            Renderer renderer =
                hits[i].collider.GetComponentInParent<Renderer>();

            if (renderer == null)
                continue;

            RegisterRenderer(renderer);
            fadeObjects[renderer].isBlocking = true;
        }
    }

    private void RegisterRenderer(Renderer renderer)
    {
        if (fadeObjects.ContainsKey(renderer))
            return;

        Material[] originalMaterials = renderer.sharedMaterials;
        Material[] fadeMaterials = CreateFadeMaterials(originalMaterials);

        fadeObjects.Add(
            renderer,
            new FadeData
            {
                originalMaterials = originalMaterials,
                fadeMaterials = fadeMaterials,
                originalShadowMode = renderer.shadowCastingMode,
                alpha = 1f,
                isBlocking = true
            }
        );

        renderer.materials = fadeMaterials;
    }

    private void UpdateFadeObjects()
    {
        renderersToRemove.Clear();

        foreach (var pair in fadeObjects)
        {
            Renderer renderer = pair.Key;
            FadeData data = pair.Value;

            if (renderer == null)
            {
                renderersToRemove.Add(renderer);
                continue;
            }

            float targetAlpha = data.isBlocking
                ? transparentAlpha
                : 1f;

            float speed = data.isBlocking
                ? fadeToTransparentSpeed
                : fadeToOpaqueSpeed;

            data.alpha = Mathf.MoveTowards(
                data.alpha,
                targetAlpha,
                speed * Time.deltaTime
            );

            ApplyAlpha(data.fadeMaterials, data.alpha);

            if (disableShadowsWhileTransparent)
            {
                renderer.shadowCastingMode = data.alpha < 0.95f
                    ? UnityEngine.Rendering.ShadowCastingMode.Off
                    : data.originalShadowMode;
            }

            if (!data.isBlocking && data.alpha >= 0.999f)
            {
                renderer.materials = data.originalMaterials;
                renderer.shadowCastingMode = data.originalShadowMode;
                renderersToRemove.Add(renderer);
            }
            else
            {
                renderer.materials = data.fadeMaterials;
            }
        }

        for (int i = 0; i < renderersToRemove.Count; i++)
            fadeObjects.Remove(renderersToRemove[i]);
    }

    private Material[] CreateFadeMaterials(Material[] sourceMaterials)
    {
        Material[] result = new Material[sourceMaterials.Length];

        for (int i = 0; i < sourceMaterials.Length; i++)
        {
            Material material = new Material(sourceMaterials[i]);

            SetupURPTransparentMaterial(material);
            SetMaterialAlpha(material, 1f);

            result[i] = material;
        }

        return result;
    }

    private void ApplyAlpha(Material[] materials, float alpha)
    {
        for (int i = 0; i < materials.Length; i++)
            SetMaterialAlpha(materials[i], alpha);
    }

    private void SetupURPTransparentMaterial(Material material)
    {
        material.SetFloat("_Surface", 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_AlphaClip", 0f);
        material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);

        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");

        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void SetMaterialAlpha(Material material, float alpha)
    {
        if (material.HasProperty("_BaseColor"))
        {
            Color color = material.GetColor("_BaseColor");
            color.a = alpha;
            material.SetColor("_BaseColor", color);
        }

        if (material.HasProperty("_Color"))
        {
            Color color = material.GetColor("_Color");
            color.a = alpha;
            material.SetColor("_Color", color);
        }
    }
}