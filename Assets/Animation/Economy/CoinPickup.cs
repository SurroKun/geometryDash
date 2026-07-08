using UnityEngine;
using System.Collections;

public class CoinPickup : MonoBehaviour
{
    [Header("Coin")]
    public CurrencyType currencyType = CurrencyType.Base;
    public int value = 1;
    public string coinId = "";

    [Header("Level")]
    public int levelIndex = -1;
    public int firstLevelBuildIndex = 2;

    [Header("Animation")]
    public bool rotate = false;
    public Vector3 rotationSpeed = new Vector3(0f, 180f, 0f);
    public bool bob = true;
    public float bobHeight = 0.12f;
    public float bobSpeed = 2f;

    [Header("Collect")]
    public GameObject collectEffectPrefab;
    public bool destroyAfterCollect = false;
    public bool fadeOnCollect = true;
    public float fadeDuration = 0.15f;
    public bool debugLog = false;

    private Vector3 startLocalPosition;
    private bool collected = false;
    private Renderer[] renderers;
    private Collider[] colliders;

    private void Awake()
    {
        startLocalPosition = transform.localPosition;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        if (levelIndex < 0)
            levelIndex = UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex - firstLevelBuildIndex;

        if (string.IsNullOrEmpty(coinId))
            coinId = gameObject.name + "_" + transform.GetSiblingIndex();
    }

    private void Start()
    {
        if (currencyType == CurrencyType.Base && GameProgress.IsBaseCoinCollected(levelIndex, coinId))
            gameObject.SetActive(false);
    }

    private void Update()
    {
        if (rotate)
            transform.Rotate(rotationSpeed * Time.deltaTime, Space.Self);

        if (bob)
        {
            Vector3 position = startLocalPosition;
            position.y += Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.localPosition = position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryCollect(collision.collider);
    }

    private void TryCollect(Collider other)
    {
        if (collected)
            return;

        if (other == null)
            return;

        if (!IsPlayer(other))
        {
            if (debugLog)
                Debug.Log("Coin touched by non-player: " + other.name);

            return;
        }

        Collect();
    }

    private bool IsPlayer(Collider other)
    {
        if (other.CompareTag("Player"))
            return true;

        if (other.GetComponent<PlayerMove>() != null)
            return true;

        if (other.GetComponentInParent<PlayerMove>() != null)
            return true;

        return false;
    }

    private void Collect()
    {
        collected = true;

        if (debugLog)
            Debug.Log("Collected coin: " + currencyType + " | " + coinId);

        if (currencyType == CurrencyType.Base)
        {
            GameProgress.AddBaseCoins(value);
            GameProgress.MarkBaseCoinCollected(levelIndex, coinId);
        }
        else
        {
            RunCurrencyCollector collector = RunCurrencyCollector.Instance;
            if (collector != null)
                collector.CollectPremiumCoin(coinId);
        }

        PlayCollectEffect();

        if (fadeOnCollect)
            StartCoroutine(FadeAndHideRoutine());
        else if (destroyAfterCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void PlayCollectEffect()
    {
        if (collectEffectPrefab == null)
            return;

        Instantiate(collectEffectPrefab, transform.position, transform.rotation);
    }

    private IEnumerator FadeAndHideRoutine()
    {
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(0f);

        if (destroyAfterCollect)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    private void SetAlpha(float alpha)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];

            if (targetRenderer == null)
                continue;

            Material[] materials = targetRenderer.materials;

            for (int j = 0; j < materials.Length; j++)
            {
                if (materials[j] == null || !materials[j].HasProperty("_Color"))
                    continue;

                Color color = materials[j].color;
                color.a = alpha;
                materials[j].color = color;
            }
        }
    }
}
