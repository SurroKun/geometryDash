using System.Collections;
using UnityEngine;

public class TrailParticleFade : MonoBehaviour
{
    [Header("Shrink")]
    public bool useShrink = true;
    public float shrinkDelay = 0.15f;
    public float shrinkDuration = 0.2f;

    [Header("Fade")]
    public bool useFade = true;
    public float fadeDelay = 0f;
    public float fadeDuration = 0.3f;

    [Tooltip("Начальная прозрачность (1 = полностью видно)")]
    public float startAlpha = 1f;

    [Tooltip("Конечная прозрачность (0 = полностью исчезает)")]
    public float endAlpha = 0f;

    [Header("Destroy")]
    public float destroyDelayAfterEffects = 0f;

    [Header("Random Size")]
    public float minScaleMultiplier = 0.8f;
    public float maxScaleMultiplier = 1.2f;

    private Vector3 startScale;
    private Renderer[] cachedRenderers;
    private Material[] cachedMaterials;
    private Color[] startColors;

    void Start()
    {
        float randomScale = Random.Range(minScaleMultiplier, maxScaleMultiplier);
        transform.localScale *= randomScale;
        startScale = transform.localScale;

        CacheMaterials();
        ApplyStartAlpha();

        if (Application.isPlaying)
            StartCoroutine(AnimateAndDestroy());
    }

    void CacheMaterials()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);

        if (cachedRenderers == null || cachedRenderers.Length == 0)
            return;

        cachedMaterials = new Material[cachedRenderers.Length];
        startColors = new Color[cachedRenderers.Length];

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            if (cachedRenderers[i] == null)
                continue;

            cachedMaterials[i] = Application.isPlaying
                ? cachedRenderers[i].material
                : cachedRenderers[i].sharedMaterial;

            if (cachedMaterials[i] != null && cachedMaterials[i].HasProperty("_Color"))
                startColors[i] = cachedMaterials[i].color;
            else
                startColors[i] = Color.white;
        }
    }

    void ApplyStartAlpha()
    {
        if (!useFade || cachedMaterials == null)
            return;

        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            if (cachedMaterials[i] == null || !cachedMaterials[i].HasProperty("_Color"))
                continue;

            Color c = cachedMaterials[i].color;
            c.a = startAlpha;
            cachedMaterials[i].color = c;
        }
    }

    IEnumerator AnimateAndDestroy()
    {
        float totalLifetime = GetTotalLifetime();
        float timer = 0f;

        while (timer < totalLifetime)
        {
            timer += Time.deltaTime;

            UpdateShrink(timer);
            UpdateFade(timer);

            yield return null;
        }

        if (destroyDelayAfterEffects > 0f)
            yield return new WaitForSeconds(destroyDelayAfterEffects);

        Destroy(gameObject);
    }

    float GetTotalLifetime()
    {
        float shrinkEnd = useShrink ? shrinkDelay + shrinkDuration : 0f;
        float fadeEnd = useFade ? fadeDelay + fadeDuration : 0f;

        return Mathf.Max(shrinkEnd, fadeEnd);
    }

    void UpdateShrink(float timer)
    {
        if (!useShrink || timer < shrinkDelay)
            return;

        float t = shrinkDuration > 0f
            ? Mathf.Clamp01((timer - shrinkDelay) / shrinkDuration)
            : 1f;

        transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
    }

    void UpdateFade(float timer)
    {
        if (!useFade || timer < fadeDelay)
            return;

        float t = fadeDuration > 0f
            ? Mathf.Clamp01((timer - fadeDelay) / fadeDuration)
            : 1f;

        if (cachedMaterials == null)
            return;

        for (int i = 0; i < cachedMaterials.Length; i++)
        {
            Material mat = cachedMaterials[i];

            if (mat == null || !mat.HasProperty("_Color"))
                continue;

            Color baseColor = startColors[i];

            float alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            baseColor.a = alpha;

            mat.color = baseColor;
        }
    }
}