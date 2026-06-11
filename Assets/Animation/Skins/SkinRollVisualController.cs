using UnityEngine;

public class SkinRollVisualController : MonoBehaviour
{
    [Header("References")]
    public PlayerSkinSwitcher skinSwitcher;
    public Rigidbody playerRb;
    public PlayerMove playerMove;
    public GroundTrailFromPoints groundTrail;
    public Transform movementReference;

    [Header("Settings")]
    public float rollSpeedMultiplier = 35f;
    public bool onlyWhenGrounded = true;
    public bool useTrailGroundCheck = true;
    public bool ignoreVerticalMovement = true;
    public bool invertDirection = false;
    public bool usePositionDeltaSpeed = true;

    private bool rollingEnabled = false;
    private Transform currentRotationTarget;

    private Vector3 lastReferencePosition;
    private bool hasLastPosition = false;

    void Start()
    {
        if (skinSwitcher == null)
            skinSwitcher = GetComponentInChildren<PlayerSkinSwitcher>(true);

        if (playerRb == null)
            playerRb = GetComponent<Rigidbody>();

        if (playerMove == null)
            playerMove = GetComponent<PlayerMove>();

        if (groundTrail == null)
            groundTrail = GetComponentInChildren<GroundTrailFromPoints>(true);

        if (movementReference == null)
            movementReference = transform;

        RefreshTarget();
        ResetMotionTracking();
    }

    void OnEnable()
    {
        ResetMotionTracking();
    }

    void Update()
    {
        if (!rollingEnabled)
            return;

        if (skinSwitcher == null)
            return;

        if (currentRotationTarget == null)
            RefreshTarget();

        if (currentRotationTarget == null)
            return;

        if (onlyWhenGrounded && !IsGroundedForRoll())
        {
            UpdateLastReferencePosition();
            return;
        }

        float speed = GetRollingSpeed();

        if (speed <= 0.001f)
            return;

        float dir = invertDirection ? -1f : 1f;
        float rotationAmount = speed * rollSpeedMultiplier * Time.deltaTime * dir;

        currentRotationTarget.Rotate(Vector3.right, rotationAmount, Space.Self);
    }

    public void SetMode(bool enableRolling)
    {
        rollingEnabled = enableRolling;
        RefreshTarget();
        ResetMotionTracking();
    }

    public bool IsRollingEnabled()
    {
        return rollingEnabled;
    }

    public void RefreshTarget()
    {
        currentRotationTarget = null;

        if (skinSwitcher == null)
            return;

        GameObject currentSkin = skinSwitcher.GetCurrentSkinObject();
        if (currentSkin == null)
            return;

        RollingSkinTarget marker = currentSkin.GetComponentInChildren<RollingSkinTarget>(true);

        if (marker != null)
            currentRotationTarget = marker.GetRotationTarget();
        else
            currentRotationTarget = currentSkin.transform;
    }

    public void ResetMotionTracking()
    {
        hasLastPosition = false;
        UpdateLastReferencePosition();
    }

    private bool IsGroundedForRoll()
    {
        if (useTrailGroundCheck && groundTrail != null)
            return groundTrail.HasGroundContact();

        if (playerMove != null)
            return playerMove.IsGrounded();

        return true;
    }

    private float GetRollingSpeed()
    {
        if (usePositionDeltaSpeed)
            return GetSpeedFromPositionDelta();

        if (playerRb != null)
            return playerRb.linearVelocity.magnitude;

        return 0f;
    }

    private float GetSpeedFromPositionDelta()
    {
        if (movementReference == null)
            return 0f;

        Vector3 currentPos = movementReference.position;

        if (!hasLastPosition)
        {
            lastReferencePosition = currentPos;
            hasLastPosition = true;
            return 0f;
        }

        Vector3 delta = currentPos - lastReferencePosition;
        lastReferencePosition = currentPos;

        if (ignoreVerticalMovement)
            delta.y = 0f;

        float distance = delta.magnitude;

        if (Time.deltaTime <= 0.0001f)
            return 0f;

        return distance / Time.deltaTime;
    }

    private void UpdateLastReferencePosition()
    {
        if (movementReference == null)
            return;

        lastReferencePosition = movementReference.position;
        hasLastPosition = true;
    }
}