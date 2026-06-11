using UnityEngine;

public class DeathScript : MonoBehaviour
{
    [Header("Death Settings")]
    public float minSpeedToSurvive = 4f;
    public float deathDelay = 0.15f;

    [Header("UI")]
    public DeathMenuUI deathMenuUI;

    [Header("Practice")]
    public PracticeModeManager practiceModeManager;

    private Rigidbody rb;
    private float lastZ;
    private float slowTimer = 0f;
    private bool isDead = false;

    private PlayerMove playerMove;
    private PlayerDeathEffect deathEffect;
    private GroundTrailFromPoints trail;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerMove = GetComponent<PlayerMove>();
        deathEffect = GetComponent<PlayerDeathEffect>();
        trail = GetComponentInChildren<GroundTrailFromPoints>();

        if (practiceModeManager == null)
            practiceModeManager = GetComponent<PracticeModeManager>();

        if (rb != null)
            lastZ = rb.position.z;
    }

    void FixedUpdate()
    {
        if (isDead) return;
        if (rb == null) return;

        float currentZ = rb.position.z;
        float speed = Mathf.Abs((currentZ - lastZ) / Time.fixedDeltaTime);
        lastZ = currentZ;

        if (speed < minSpeedToSurvive)
        {
            slowTimer += Time.fixedDeltaTime;

            if (slowTimer >= deathDelay)
                DieFromSpeedLoss();
        }
        else
        {
            slowTimer = 0f;
        }
    }

    public void DieFromSpeedLoss()
    {
        if (isDead) return;

        isDead = true;
        Debug.Log("Dead from speed loss");
        KillPlayer();
    }

    public void DieFromCollision()
    {
        if (isDead) return;

        isDead = true;
        slowTimer = 0f;

        Debug.Log("Dead from collision");
        KillPlayer();
    }

    void KillPlayer()
    {
        if (trail != null)
            trail.StopTrail();

        if (playerMove != null)
            playerMove.enabled = false;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (deathEffect != null)
            deathEffect.Explode();

        if (DeathMenuUI.PracticeModeActive)
        {
            if (practiceModeManager != null)
                practiceModeManager.RespawnFromCheckpoint();
        }
        else
        {
            if (deathMenuUI != null)
                deathMenuUI.ShowDeathMenu();
        }
    }

    public bool IsDead()
    {
        return isDead;
    }

    public void ResetDeathState()
    {
        isDead = false;
        slowTimer = 0f;

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            lastZ = rb.position.z;
        }
    }
}