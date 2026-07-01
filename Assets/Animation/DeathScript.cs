using UnityEngine;

public class DeathScript : MonoBehaviour
{
    private const string SpeedLossDeathMessage = "Dead from speed loss";
    private const string CollisionDeathMessage = "Dead from collision";

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
        if (isDead)
            return;

        if (rb == null)
            return;

        float speed = CalculateForwardSpeed();

        if (speed < minSpeedToSurvive)
            TickSlowDeathTimer();
        else
            ResetSlowDeathTimer();
    }

    public void DieFromSpeedLoss()
    {
        if (isDead)
            return;

        Die(SpeedLossDeathMessage, false);
    }

    public void DieFromCollision()
    {
        if (isDead)
            return;

        Die(CollisionDeathMessage, true);
    }

    private void Die(string logMessage, bool resetSlowTimer)
    {
        isDead = true;

        if (resetSlowTimer)
            ResetSlowDeathTimer();

        Debug.Log(logMessage);
        KillPlayer();
    }

    private float CalculateForwardSpeed()
    {
        float currentZ = rb.position.z;
        float speed = Mathf.Abs((currentZ - lastZ) / Time.fixedDeltaTime);
        lastZ = currentZ;

        return speed;
    }

    private void TickSlowDeathTimer()
    {
        slowTimer += Time.fixedDeltaTime;

        if (slowTimer >= deathDelay)
            DieFromSpeedLoss();
    }

    private void ResetSlowDeathTimer()
    {
        slowTimer = 0f;
    }

    private void KillPlayer()
    {
        if (trail != null)
            trail.StopTrail();

        if (playerMove != null)
            playerMove.enabled = false;

        FreezeRigidbody();

        if (deathEffect != null)
            deathEffect.Explode();

        HandleDeathFlow();
    }

    private void FreezeRigidbody()
    {
        if (rb == null)
            return;

        ZeroRigidbodyMotion();
        rb.isKinematic = true;
    }

    private void UnfreezeRigidbody()
    {
        if (rb == null)
            return;

        rb.isKinematic = false;
        ZeroRigidbodyMotion();
        lastZ = rb.position.z;
    }

    private void ZeroRigidbodyMotion()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void HandleDeathFlow()
    {
        if (DeathMenuUI.PracticeModeActive)
        {
            if (practiceModeManager != null)
                practiceModeManager.RespawnFromCheckpoint();

            return;
        }

        if (deathMenuUI != null)
            deathMenuUI.ShowDeathMenu();
    }

    public bool IsDead()
    {
        return isDead;
    }

    public void ResetDeathState()
    {
        isDead = false;
        ResetSlowDeathTimer();
        UnfreezeRigidbody();
    }
}
