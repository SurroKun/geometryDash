using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PracticeModeManager : MonoBehaviour
{
    public enum CheckpointSpawnMode
    {
        Timer,
        JumpCount
    }

    [System.Serializable]
    public class PracticeCheckpointData
    {
        public Vector3 position;
        public bool jumpBoostActive;
        public bool gravityInverted;
        public bool speedBoostActive;

        public PracticeCheckpointData(
            Vector3 position,
            bool jumpBoostActive,
            bool gravityInverted,
            bool speedBoostActive
        )
        {
            this.position = position;
            this.jumpBoostActive = jumpBoostActive;
            this.gravityInverted = gravityInverted;
            this.speedBoostActive = speedBoostActive;
        }
    }

    [Header("References")]
    public Transform player;
    public Rigidbody playerRb;
    public PlayerMove playerMove;
    public DeathScript deathScript;
    public GroundTrailFromPoints trail;
    public PlayerDeathEffect deathEffect;
    public PlayerJumpHeightBonus jumpHeightBonus;
    public PlayerSpeedBoostBonus speedBoostBonus;
    public PlayerGravityFlip gravityFlip;
    public PlayerSkinVFXController skinVFXController;
    public SkinAnimatorModeController skinAnimatorModeController;
    public SkinRollVisualController skinRollVisualController;

    [Header("Checkpoint Mode")]
    public CheckpointSpawnMode checkpointSpawnMode = CheckpointSpawnMode.JumpCount;

    [Header("Timer Checkpoints")]
    public float checkpointInterval = 4f;
    public bool allowCheckpointsInAir = true;

    [Header("Jump Count Checkpoints")]
    public int jumpsBeforeCheckpoint = 5;
    public bool saveAfterLandingOnly = true;

    [Header("Checkpoint Settings")]
    public float respawnDelay = 0.35f;
    public float checkpointYOffset = 0.5f;

    [Header("Checkpoint Marker")]
    public GameObject checkpointMarkerPrefab;
    public Vector3 markerOffset = new Vector3(0f, -0.45f, 0f);

    [Header("Settings")]
    public int minCheckpointCount = 1;
    public float minDistanceBetweenCheckpoints = 1f;
    public bool blockCheckpointsInFlightMode = true;

    [Header("Temporary Gravity Bonus")]
    public bool forceNormalGravityAfterTemporaryBonus = true;
    public bool saveActualGravityAfterTemporaryBonus = true;
    public bool useGravityRelativeCheckpointOffset = true;

    private List<PracticeCheckpointData> checkpoints =
        new List<PracticeCheckpointData>();

    private List<GameObject> checkpointMarkers =
        new List<GameObject>();

    private float checkpointTimer = 0f;
    private bool isRespawning = false;

    private bool hasPendingGravityState = false;
    private bool pendingGravityState = false;

    private bool temporaryGravityBonusWasUsed = false;

    private int jumpCounter = 0;
    private bool waitingForLandingCheckpoint = false;
    private bool wasGroundedLastFrame = false;

    void Start()
    {
        if (player == null)
            player = transform;

        if (playerRb == null)
            playerRb = GetComponent<Rigidbody>();

        if (playerMove == null)
            playerMove = GetComponent<PlayerMove>();

        if (deathScript == null)
            deathScript = GetComponent<DeathScript>();

        if (trail == null)
            trail = GetComponentInChildren<GroundTrailFromPoints>(true);

        if (deathEffect == null)
            deathEffect = GetComponent<PlayerDeathEffect>();

        if (jumpHeightBonus == null)
            jumpHeightBonus = GetComponent<PlayerJumpHeightBonus>();

        if (speedBoostBonus == null)
            speedBoostBonus = GetComponent<PlayerSpeedBoostBonus>();

        if (gravityFlip == null)
            gravityFlip = GetComponent<PlayerGravityFlip>();

        if (skinVFXController == null)
            skinVFXController = GetComponent<PlayerSkinVFXController>();

        if (skinAnimatorModeController == null)
            skinAnimatorModeController =
                GetComponentInChildren<SkinAnimatorModeController>(true);

        if (skinRollVisualController == null)
            skinRollVisualController = GetComponent<SkinRollVisualController>();

        checkpoints.Clear();
        checkpointMarkers.Clear();

        isRespawning = false;
        ResetPendingBonusState();
        ResetCheckpointCounters();

        if (DeathMenuUI.PracticeModeActive)
            SaveCheckpoint(player.position, false);
    }

    void Update()
    {
        if (!CanUpdatePracticeCheckpoints())
            return;

        if (ShouldBlockCheckpointsInFlight())
        {
            checkpointTimer = 0f;
            return;
        }

        if (checkpointSpawnMode == CheckpointSpawnMode.Timer)
        {
            HandleTimerCheckpointMode();
        }
        else
        {
            HandleJumpCountCheckpointMode();
        }
    }

    private void HandleTimerCheckpointMode()
    {
        checkpointTimer += Time.deltaTime;

        if (checkpointTimer >= checkpointInterval)
        {
            checkpointTimer = 0f;

            if (allowCheckpointsInAir)
            {
                SaveCheckpoint(player.position, true);
            }
            else
            {
                if (IsGroundedSafe())
                    SaveCheckpoint(player.position, true);
            }
        }
    }

    private void HandleJumpCountCheckpointMode()
    {
        bool groundedNow = IsGroundedSafe();

        if (waitingForLandingCheckpoint)
        {
            if (saveAfterLandingOnly)
            {
                if (groundedNow && !wasGroundedLastFrame)
                {
                    SaveCheckpoint(player.position, true);
                    jumpCounter = 0;
                    waitingForLandingCheckpoint = false;
                }
            }
            else
            {
                SaveCheckpoint(player.position, true);
                jumpCounter = 0;
                waitingForLandingCheckpoint = false;
            }
        }

        wasGroundedLastFrame = groundedNow;
    }

    public void NotifyPlayerJumped()
    {
        if (checkpointSpawnMode != CheckpointSpawnMode.JumpCount)
            return;

        if (!CanUpdatePracticeCheckpoints())
            return;

        if (ShouldBlockCheckpointsInFlight())
            return;

        if (waitingForLandingCheckpoint)
            return;

        jumpCounter++;

        Debug.Log(
            "Practice jump counted: " +
            jumpCounter +
            "/" +
            jumpsBeforeCheckpoint
        );

        if (jumpCounter >= jumpsBeforeCheckpoint)
        {
            waitingForLandingCheckpoint = true;

            if (!saveAfterLandingOnly)
            {
                SaveCheckpoint(player.position, true);
                jumpCounter = 0;
                waitingForLandingCheckpoint = false;
            }
        }
    }

    bool IsGroundedSafe()
    {
        if (playerMove == null)
            return false;

        if (playerMove.IsFlightModeActive())
            return false;

        return playerMove.IsGrounded();
    }

    void SaveCheckpoint(Vector3 position, bool createMarker)
    {
        if (!DeathMenuUI.PracticeModeActive)
            return;

        if (ShouldBlockCheckpointsInFlight())
            return;

        bool jumpBoostState = false;
        bool gravityState = false;
        bool speedBoostState = false;

        if (jumpHeightBonus != null)
            jumpBoostState = jumpHeightBonus.IsBoosted();

        if (gravityFlip != null)
            gravityState = gravityFlip.IsGravityInverted();

        if (speedBoostBonus != null)
            speedBoostState = speedBoostBonus.IsBoosted();

        if (temporaryGravityBonusWasUsed &&
            !saveActualGravityAfterTemporaryBonus &&
            forceNormalGravityAfterTemporaryBonus)
        {
            gravityState = false;
            hasPendingGravityState = false;
            pendingGravityState = false;
        }
        else if (hasPendingGravityState)
        {
            gravityState = pendingGravityState;
            hasPendingGravityState = false;
        }

        Vector3 checkpointPos =
            position + GetCheckpointOffset(gravityState);

        if (checkpoints.Count > 0)
        {
            float dist = Vector3.Distance(
                checkpoints[checkpoints.Count - 1].position,
                checkpointPos
            );

            if (dist < minDistanceBetweenCheckpoints)
                return;
        }

        checkpoints.Add(
            new PracticeCheckpointData(
                checkpointPos,
                jumpBoostState,
                gravityState,
                speedBoostState
            )
        );

        if (createMarker)
        {
            GameObject marker = CreateCheckpointMarker(position);
            checkpointMarkers.Add(marker);
        }
        else
        {
            checkpointMarkers.Add(null);
        }

        Debug.Log(
            "Practice checkpoint saved: " + checkpointPos +
            " | JumpBoost: " + jumpBoostState +
            " | GravityInverted: " + gravityState +
            " | SpeedBoost: " + speedBoostState +
            " | JumpCounter: " + jumpCounter
        );
    }

    GameObject CreateCheckpointMarker(Vector3 basePosition)
    {
        if (checkpointMarkerPrefab == null)
            return null;

        bool gravityState = gravityFlip != null && gravityFlip.IsGravityInverted();
        Vector3 markerPosition = basePosition + GetMarkerOffset(gravityState);

        return Instantiate(
            checkpointMarkerPrefab,
            markerPosition,
            Quaternion.identity
        );
    }

    private Vector3 GetCheckpointOffset(bool gravityInverted)
    {
        if (!useGravityRelativeCheckpointOffset)
            return Vector3.up * checkpointYOffset;

        Vector3 awayFromSurface = gravityInverted ? Vector3.down : Vector3.up;
        return awayFromSurface * checkpointYOffset;
    }

    private Vector3 GetMarkerOffset(bool gravityInverted)
    {
        if (!useGravityRelativeCheckpointOffset)
            return markerOffset;

        return gravityInverted
            ? new Vector3(markerOffset.x, -markerOffset.y, markerOffset.z)
            : markerOffset;
    }

    public void RespawnFromCheckpoint()
    {
        if (!DeathMenuUI.PracticeModeActive) return;
        if (checkpoints.Count == 0) return;
        if (isRespawning) return;
        if (IsRaceFinished()) return;

        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        isRespawning = true;

        yield return new WaitForSeconds(respawnDelay);

        if (IsRaceFinished())
        {
            isRespawning = false;
            yield break;
        }

        PracticeCheckpointData checkpoint =
            checkpoints[checkpoints.Count - 1];

        Vector3 respawnPosition = checkpoint.position;

        if (deathEffect != null)
            deathEffect.RestorePlayer();

        if (playerRb != null)
        {
            playerRb.isKinematic = false;
            playerRb.linearVelocity = Vector3.zero;
            playerRb.angularVelocity = Vector3.zero;
            playerRb.position = respawnPosition;
        }
        else if (player != null)
        {
            player.position = respawnPosition;
        }

        if (playerMove != null)
        {
            playerMove.enabled = true;
            playerMove.ResetAfterRespawn(respawnPosition.x);
        }

        if (gravityFlip != null)
        {
            gravityFlip.SnapGravityState(checkpoint.gravityInverted);
            gravityFlip.IgnoreTriggersAfterRespawn();
        }

        if (jumpHeightBonus != null)
        {
            jumpHeightBonus.SetBoostState(checkpoint.jumpBoostActive);
            jumpHeightBonus.IgnoreTriggersAfterRespawn();
        }

        if (speedBoostBonus != null)
        {
            speedBoostBonus.SetBoostState(checkpoint.speedBoostActive);
            speedBoostBonus.IgnoreTriggersAfterRespawn();
        }

        if (trail != null)
            trail.ResetTrail();

        if (deathScript != null)
            deathScript.ResetDeathState();

        if (skinVFXController != null)
            skinVFXController.ApplyCurrentSkinProfile();

        if (skinAnimatorModeController != null)
            skinAnimatorModeController.ApplyCurrentMode();

        if (skinRollVisualController != null)
            skinRollVisualController.RefreshTarget();

        if (skinRollVisualController != null)
            skinRollVisualController.ResetMotionTracking();

        ResetCheckpointCounters();

        isRespawning = false;
    }

    private bool CanUpdatePracticeCheckpoints()
    {
        if (!DeathMenuUI.PracticeModeActive)
            return false;

        if (isRespawning)
            return false;

        return deathScript == null || !deathScript.IsDead();
    }

    private bool ShouldBlockCheckpointsInFlight()
    {
        return blockCheckpointsInFlightMode &&
               playerMove != null &&
               playerMove.IsFlightModeActive();
    }

    private bool IsRaceFinished()
    {
        return RaceModeManager.ActiveRace != null &&
               RaceModeManager.ActiveRace.IsFinished;
    }

    private void ResetCheckpointCounters()
    {
        checkpointTimer = 0f;
        jumpCounter = 0;
        waitingForLandingCheckpoint = false;
        wasGroundedLastFrame = false;
    }

    private void ResetPendingBonusState()
    {
        hasPendingGravityState = false;
        pendingGravityState = false;
        temporaryGravityBonusWasUsed = false;
    }

    public void RemoveLastCheckpoint()
    {
        if (!DeathMenuUI.PracticeModeActive)
            return;

        if (checkpoints.Count <= minCheckpointCount)
        {
            Debug.Log("Cannot remove last checkpoint.");
            return;
        }

        int lastIndex = checkpoints.Count - 1;

        if (lastIndex < checkpointMarkers.Count &&
            checkpointMarkers[lastIndex] != null)
        {
            Destroy(checkpointMarkers[lastIndex]);
        }

        checkpoints.RemoveAt(lastIndex);

        if (lastIndex < checkpointMarkers.Count)
            checkpointMarkers.RemoveAt(lastIndex);

        PracticeCheckpointData currentCheckpoint =
            checkpoints[checkpoints.Count - 1];

        Debug.Log(
            "Removed last checkpoint. Current checkpoint: " +
            currentCheckpoint.position +
            " | JumpBoost: " + currentCheckpoint.jumpBoostActive +
            " | GravityInverted: " + currentCheckpoint.gravityInverted +
            " | SpeedBoost: " + currentCheckpoint.speedBoostActive
        );
    }

    public int GetCheckpointCount()
    {
        return checkpoints.Count;
    }

    public void NotifyGravityChanged(bool newGravityState)
    {
        if (!DeathMenuUI.PracticeModeActive)
            return;

        if (temporaryGravityBonusWasUsed &&
            forceNormalGravityAfterTemporaryBonus)
        {
            Debug.Log(
                "Gravity change ignored because temporary gravity bonus was used."
            );

            return;
        }

        hasPendingGravityState = true;
        pendingGravityState = newGravityState;

        Debug.Log(
            "Pending gravity stored for next checkpoint: " +
            newGravityState
        );
    }

    public void NotifyTemporaryGravityBonusUsed()
    {
        if (!DeathMenuUI.PracticeModeActive)
            return;

        temporaryGravityBonusWasUsed = true;

        hasPendingGravityState = false;
        pendingGravityState = false;

        Debug.Log(
            "Temporary gravity bonus used. Practice checkpoints will save normal gravity."
        );
    }
}
