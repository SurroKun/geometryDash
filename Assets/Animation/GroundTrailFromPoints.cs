using UnityEngine;

public class GroundTrailFromPoints : MonoBehaviour
{
    [Header("References")]
    public Transform[] spawnPoints;
    public GameObject trailPrefab;

    [Header("Skin Override")]
    public Material overrideTrailMaterial;
    public Material[] overrideTrailMaterials;

    [Header("Spawn Timing")]
    public float minSpawnTime = 0.02f;
    public float maxSpawnTime = 0.08f;

    [Header("Ground Check")]
    public bool spawnOnlyWhenGrounded = true;
    public float minGroundNormalY = 0.4f;

    private float timer;
    private float currentSpawnDelay;

    private int groundContacts = 0;
    private bool isStopped = false;

    private Collider playerCol;

    void Start()
    {
        playerCol = GetComponent<Collider>();
        ResetTrail();
    }

    void Update()
    {
        if (isStopped || !enabled || !gameObject.activeInHierarchy)
            return;

        if (trailPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        if (spawnOnlyWhenGrounded && groundContacts <= 0)
        {
            timer = 0f;
            return;
        }

        timer += Time.deltaTime;

        if (timer >= currentSpawnDelay)
        {
            timer = 0f;
            SetNewDelay();
            SpawnFromPoints();
        }
    }

    void SetNewDelay()
    {
        currentSpawnDelay = Random.Range(minSpawnTime, maxSpawnTime);
    }

    void SpawnFromPoints()
    {
        if (isStopped || trailPrefab == null)
            return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            Transform point = spawnPoints[i];

            if (point == null)
                continue;

            GameObject particle = Instantiate(trailPrefab, point.position, point.rotation);

            ApplyOverrideMaterial(particle);
            IgnorePlayerCollision(particle);
        }
    }

    void ApplyOverrideMaterial(GameObject particle)
    {
        if (particle == null)
            return;

        Material materialToUse = GetMaterialForThisParticle();

        if (materialToUse == null)
            return;

        Renderer[] renderers = particle.GetComponentsInChildren<Renderer>(true);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                renderers[i].material = materialToUse;
        }
    }

    Material GetMaterialForThisParticle()
    {
        if (overrideTrailMaterials != null && overrideTrailMaterials.Length > 0)
        {
            int validCount = 0;

            for (int i = 0; i < overrideTrailMaterials.Length; i++)
            {
                if (overrideTrailMaterials[i] != null)
                    validCount++;
            }

            if (validCount > 0)
            {
                Material[] validMaterials = new Material[validCount];
                int index = 0;

                for (int i = 0; i < overrideTrailMaterials.Length; i++)
                {
                    if (overrideTrailMaterials[i] != null)
                    {
                        validMaterials[index] = overrideTrailMaterials[i];
                        index++;
                    }
                }

                return validMaterials[Random.Range(0, validMaterials.Length)];
            }
        }

        return overrideTrailMaterial;
    }

    void IgnorePlayerCollision(GameObject particle)
    {
        if (particle == null || playerCol == null)
            return;

        Collider[] particleCols = particle.GetComponentsInChildren<Collider>(true);

        for (int i = 0; i < particleCols.Length; i++)
        {
            if (particleCols[i] != null)
                Physics.IgnoreCollision(playerCol, particleCols[i]);
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (isStopped)
            return;

        bool hasGroundLikeContact = false;

        for (int i = 0; i < collision.contacts.Length; i++)
        {
            if (collision.contacts[i].normal.y >= minGroundNormalY)
            {
                hasGroundLikeContact = true;
                break;
            }
        }

        groundContacts = hasGroundLikeContact ? 1 : 0;
    }

    void OnCollisionExit(Collision collision)
    {
        groundContacts = 0;
    }

    public void StopTrail()
    {
        isStopped = true;
        groundContacts = 0;
        timer = 0f;
        enabled = false;
    }

    public void ResetTrail()
    {
        isStopped = false;
        groundContacts = 0;
        timer = 0f;
        enabled = true;
        SetNewDelay();
    }

    public void SetTrailPrefab(GameObject newTrailPrefab)
    {
        trailPrefab = newTrailPrefab;
    }

    public void SetSpawnPoints(Transform[] newSpawnPoints)
    {
        spawnPoints = newSpawnPoints;
    }

    public void SetSpawnOnlyWhenGrounded(bool value)
    {
        spawnOnlyWhenGrounded = value;
    }

    public void SetOverrideMaterial(Material newMaterial)
    {
        overrideTrailMaterial = newMaterial;
        overrideTrailMaterials = null;
    }

    public void SetOverrideMaterials(Material[] newMaterials)
    {
        overrideTrailMaterials = newMaterials;
        overrideTrailMaterial = null;
    }

    public void ClearOverrideMaterial()
    {
        overrideTrailMaterial = null;
        overrideTrailMaterials = null;
    }

    public bool HasGroundContact()
    {
        return groundContacts > 0;
    }

    void OnDisable()
    {
        groundContacts = 0;
        timer = 0f;
    }
}