using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMove : MonoBehaviour
{
    private const string GroundTag = "Ground";
    private const string ObstacleTag = "Obstacle";
    private const string AirJumpTag = "AirJump";

    private const string LeftTrigger = "left";
    private const string RightTrigger = "right";
    private const string JumpTrigger = "jump";
    private const string FallTrigger = "fall";
    private const string GroundedBool = "isGrounded";

    [Header("Move")]
    public float forwardSpeed = 6.3f;
    public float stepSize = 2f;
    public int maxSteps = 1;
    public float laneChangeSpeed = 10f;
    public float laneSnapThreshold = 0.05f;

    [Header("Jump / Dash")]
    public float jumpForce = 10.1f;
    public float dashForce = 15f;
    public float jumpBufferTime = 0.15f;
    public float coyoteTime = 0.3f;
    public float dashBufferTime = 0.15f;
    public float fall = 1.55f;

    [Header("Dash Cooldown")]
    public float dashCooldown = 0.22f;
    public bool blockJumpRightAfterDash = true;
    public float jumpBlockAfterDash = 0.08f;

    [Header("Dash Fix")]
    public bool resetDashWhenTouchGround = true;
    public bool resetJumpWhenTouchGround = true;
    public bool clearDashBufferAfterDash = true;

    [Header("Bonus Compatibility")]
    public bool cancelSpeedDashBeforeNormalJump = true;
    public bool cancelSpeedDashBeforeAirJump = true;

    [Header("Flight Mode")]
    public float flightHorizontalStepSize = 2f;
    public float flightVerticalStepSize = 2f;
    public int flightMaxHorizontalSteps = 1;
    public int flightMaxVerticalSteps = 1;
    public float flightMoveSpeed = 10f;
    public bool freezeVelocityOnFlightStart = true;

    [Header("Flight Mode Start")]
    public bool useFixedFlightBaseX = true;
    public float fixedFlightBaseX = 0f;
    public bool snapFlightXToNearestLane = true;
    public bool useFixedFlightBaseY = true;
    public float fixedFlightBaseY = 1.1f;
    public float flightStartYOffset = 1.1f;

    [Header("Flight Animation")]
    public bool disableAnimatorInFlight = true;
    public Transform animatedModelRoot;

    [Header("Respawn")]
    public float sideInputBlockAfterRespawn = 0.12f;

    [Header("References")]
    public Animator anim;

    private PlayerGravityFlip gravityFlip;
    private PlayerJumpSpeedDashBonus speedDashBonus;
    private PracticeModeManager practiceModeManager;
    private Rigidbody rb;

    private bool canAirJump = false;

    private float startX;
    private int step = 0;
    private float currentX;

    private bool hasJumpedThisAir = false;
    private bool hasDashedInAir = false;
    private bool isGrounded = false;

    private float jumpBufferCounter;
    private float coyoteTimeCounter;
    private float dashBufferCounter;
    private float dashCooldownTimer = 0f;
    private float jumpBlockAfterDashTimer = 0f;

    private GameObject currentAirJumpObject;
    private float sideInputBlockTimer = 0f;

    private bool sideInputInverted = false;

    private bool isFlightModeActive = false;
    private float flightStartX;
    private float flightStartY;
    private int flightHorizontalStep = 0;
    private int flightVerticalStep = 0;
    private float currentFlightX;
    private float currentFlightY;

    private Vector3 modelStartLocalPosition;
    private Quaternion modelStartLocalRotation;
    private Vector3 modelStartLocalScale;
    private bool hasModelStartTransform = false;

    void Start()
    {
        gravityFlip = GetComponent<PlayerGravityFlip>();
        speedDashBonus = GetComponent<PlayerJumpSpeedDashBonus>();
        practiceModeManager = GetComponent<PracticeModeManager>();
        rb = GetComponent<Rigidbody>();

        if (rb == null)
        {
            Debug.LogError("PlayerMove requires a Rigidbody.");
            enabled = false;
            return;
        }

        startX = rb.position.x;
        currentX = startX;

        flightStartX = rb.position.x;
        flightStartY = rb.position.y;

        currentFlightX = flightStartX;
        currentFlightY = flightStartY;

        if (anim == null)
            anim = GetComponentInChildren<Animator>();

        if (animatedModelRoot == null && anim != null)
            animatedModelRoot = anim.transform;

        CacheModelStartTransform();

        SetAnimatorGrounded(false);
    }

    void Update()
    {
        UpdateTimers();

        if (isFlightModeActive)
        {
            HandleFlightInput();
            return;
        }

        HandleLaneInput();
        HandleJumpInput();
        HandleDashInput();
        HandleBetterFall();
    }

    void FixedUpdate()
    {
        if (isFlightModeActive)
        {
            FixedFlightMove();
            return;
        }

        Vector3 pos = rb.position;
        float targetX = startX + step * stepSize;

        currentX = Mathf.MoveTowards(
            pos.x,
            targetX,
            laneChangeSpeed * Time.fixedDeltaTime
        );

        rb.MovePosition(new Vector3(
            currentX,
            pos.y,
            pos.z - forwardSpeed * Time.fixedDeltaTime
        ));
    }

    public void SetSideInputInverted(bool value)
    {
        sideInputInverted = value;
    }

    void HandleLaneInput()
    {
        if (sideInputBlockTimer > 0f)
            return;

        bool leftHeld = IsLeftHeld();
        bool rightHeld = IsRightHeld();

        if (!IsNearLaneCenter())
            return;

        if (leftHeld == rightHeld)
            return;

        bool invertedNow = sideInputInverted;

        if (!invertedNow)
        {
            if (leftHeld && step < maxSteps)
                MoveLaneLeft();
            else if (rightHeld && step > -maxSteps)
                MoveLaneRight();
        }
        else
        {
            if (leftHeld && step > -maxSteps)
                MoveLaneRight();
            else if (rightHeld && step < maxSteps)
                MoveLaneLeft();
        }
    }

    void HandleJumpInput()
    {
        bool jumpHeld = IsJumpHeld();

        if (blockJumpRightAfterDash && jumpBlockAfterDashTimer > 0f)
        {
            jumpBufferCounter = 0f;
            return;
        }

        if (jumpHeld)
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        if (jumpHeld &&
            speedDashBonus != null &&
            speedDashBonus.TryUseSpeedDash())
        {
            jumpBufferCounter = 0f;
            return;
        }

        coyoteTimeCounter -= Time.deltaTime;

        if (jumpBufferCounter <= 0f)
            return;

        if (CanGroundJump())
        {
            if (cancelSpeedDashBeforeNormalJump)
                CancelSpeedDashIfActive();

            PerformJump();

            coyoteTimeCounter = 0f;
            hasJumpedThisAir = true;
        }
        else if (canAirJump)
        {
            if (cancelSpeedDashBeforeAirJump)
                CancelSpeedDashIfActive();

            PerformJump();
            ClearAirJump();
        }
    }

    private void NotifyPracticeJump()
    {
        if (practiceModeManager != null)
            practiceModeManager.NotifyPlayerJumped();
    }

    private bool CanGroundJump()
    {
        return coyoteTimeCounter > 0f && !hasJumpedThisAir;
    }

    private void PerformJump()
    {
        SetVerticalVelocity(0f);
        rb.AddForce(
            Vector3.up * jumpForce * GetGravityDirection(),
            ForceMode.Impulse
        );

        jumpBufferCounter = 0f;
        NotifyPracticeJump();
        PlayJumpAnimation();
    }

    private void ClearAirJump()
    {
        canAirJump = false;
        currentAirJumpObject = null;
    }

    private void CancelSpeedDashIfActive()
    {
        if (speedDashBonus != null && speedDashBonus.IsDashing())
            speedDashBonus.CancelDashForOtherBonus();
    }

    private float GetGravityDirection()
    {
        if (gravityFlip != null && gravityFlip.IsGravityInverted())
            return -1f;

        return 1f;
    }

    private void SetVerticalVelocity(float velocityY)
    {
        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            velocityY,
            rb.linearVelocity.z
        );
    }

    private void PlayJumpAnimation()
    {
        if (anim == null)
            return;

        anim.ResetTrigger(FallTrigger);
        anim.SetTrigger(JumpTrigger);
    }

    private void PlayDashAnimation()
    {
        if (anim == null)
            return;

        anim.ResetTrigger(JumpTrigger);
        anim.SetTrigger(FallTrigger);
    }

    private bool CanDash()
    {
        if (dashBufferCounter <= 0f)
            return false;

        if (dashCooldownTimer > 0f)
            return false;

        if (hasDashedInAir)
            return false;

        return !isGrounded;
    }

    void HandleDashInput()
    {
        bool dashPressed = IsDashPressed();

        bool dashHeld = IsDashHeld();

        if (dashPressed || dashHeld)
            dashBufferCounter = dashBufferTime;
        else
            dashBufferCounter -= Time.deltaTime;

        if (!CanDash())
            return;

        CancelSpeedDashIfActive();

        SetVerticalVelocity(dashForce * -GetGravityDirection());

        dashCooldownTimer = dashCooldown;

        if (blockJumpRightAfterDash)
            jumpBlockAfterDashTimer = jumpBlockAfterDash;

        if (clearDashBufferAfterDash)
            dashBufferCounter = 0f;

        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;

        hasDashedInAir = true;
        hasJumpedThisAir = true;

        PlayDashAnimation();
    }

    void HandleBetterFall()
    {
        if (gravityFlip != null && gravityFlip.IsGravityInverted())
        {
            if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity +=
                    Vector3.up *
                    Mathf.Abs(Physics.gravity.y) *
                    fall *
                    Time.deltaTime;
            }
        }
        else
        {
            if (rb.linearVelocity.y < 0f)
            {
                rb.linearVelocity +=
                    Vector3.up *
                    Physics.gravity.y *
                    fall *
                    Time.deltaTime;
            }
        }
    }

    void HandleFlightInput()
    {
        if (sideInputBlockTimer > 0f)
            return;

        bool leftHeld = IsLeftHeld();
        bool rightHeld = IsRightHeld();
        bool upHeld = IsJumpHeld();
        bool downHeld = IsDashHeld();

        float targetX = flightStartX + flightHorizontalStep * flightHorizontalStepSize;
        float targetY = flightStartY + flightVerticalStep * flightVerticalStepSize;

        bool nearX = Mathf.Abs(rb.position.x - targetX) <= laneSnapThreshold;
        bool nearY = Mathf.Abs(rb.position.y - targetY) <= laneSnapThreshold;

        if (nearX && leftHeld != rightHeld)
        {
            if (leftHeld && flightHorizontalStep < flightMaxHorizontalSteps)
                flightHorizontalStep++;
            else if (rightHeld && flightHorizontalStep > -flightMaxHorizontalSteps)
                flightHorizontalStep--;
        }

        if (nearY && upHeld != downHeld)
        {
            if (upHeld && flightVerticalStep < flightMaxVerticalSteps)
                flightVerticalStep++;
            else if (downHeld && flightVerticalStep > -flightMaxVerticalSteps)
                flightVerticalStep--;
        }
    }

    void FixedFlightMove()
    {
        Vector3 pos = rb.position;

        float targetX = flightStartX + flightHorizontalStep * flightHorizontalStepSize;
        float targetY = flightStartY + flightVerticalStep * flightVerticalStepSize;

        currentFlightX = Mathf.MoveTowards(
            pos.x,
            targetX,
            flightMoveSpeed * Time.fixedDeltaTime
        );

        currentFlightY = Mathf.MoveTowards(
            pos.y,
            targetY,
            flightMoveSpeed * Time.fixedDeltaTime
        );

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rb.MovePosition(new Vector3(
            currentFlightX,
            currentFlightY,
            pos.z - forwardSpeed * Time.fixedDeltaTime
        ));
    }

    public void ToggleFlightMode()
    {
        if (isFlightModeActive)
            StopFlightMode();
        else
            StartFlightMode();
    }

    public void StartFlightMode()
    {
        if (isFlightModeActive)
            return;

        isFlightModeActive = true;

        SetFlightStartPosition();
        ResetFlightSteps();
        ResetJumpDashState();

        isGrounded = false;

        if (speedDashBonus != null)
            speedDashBonus.CancelDashAndRestore();

        if (freezeVelocityOnFlightStart)
            ZeroRigidbodyMotion();

        MoveToFlightStartPosition();
        ResetModelToStartTransform();
        EnterFlightAnimationState();

        Debug.Log("Flight Mode ON");
    }

    public void StopFlightMode()
    {
        if (!isFlightModeActive)
            return;

        isFlightModeActive = false;

        ResetJumpDashState();

        ZeroRigidbodyMotion();

        ExitFlightAnimationState();
        ResetModelToStartTransform();

        SnapToLaneByWorldX(rb.position.x);

        Debug.Log("Flight Mode OFF");
    }

    public bool IsFlightModeActive()
    {
        return isFlightModeActive;
    }

    void OnCollisionStay(Collision collision)
    {
        if (isFlightModeActive)
            return;

        if (IsGroundCollision(collision))
        {
            isGrounded = true;

            coyoteTimeCounter = coyoteTime;
            canAirJump = false;

            if (resetJumpWhenTouchGround)
                hasJumpedThisAir = false;

            if (resetDashWhenTouchGround)
                hasDashedInAir = false;

            SetAnimatorGrounded(true);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (IsGroundCollision(collision))
        {
            isGrounded = false;
            coyoteTimeCounter = 0f;

            SetAnimatorGrounded(false);
        }
    }

    private bool IsGroundCollision(Collision collision)
    {
        return collision.gameObject.CompareTag(GroundTag) ||
               collision.gameObject.CompareTag(ObstacleTag);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(AirJumpTag))
        {
            canAirJump = true;
            currentAirJumpObject = other.gameObject;
        }
    }

    public bool IsGrounded()
    {
        return isGrounded;
    }

    public void SnapToLaneByWorldX(float worldX)
    {
        float rawStep = (worldX - startX) / stepSize;

        step = Mathf.RoundToInt(rawStep);
        step = Mathf.Clamp(step, -maxSteps, maxSteps);

        currentX = startX + step * stepSize;

        Vector3 pos = rb.position;
        pos.x = currentX;
        rb.position = pos;
    }

    public void ResetAfterRespawn(float respawnX)
    {
        StopFlightMode();

        if (disableAnimatorInFlight && anim != null)
            anim.enabled = true;

        ResetModelToStartTransform();

        ResetJumpDashState();

        if (speedDashBonus != null)
            speedDashBonus.CancelDashAndRestore();

        isGrounded = false;
        sideInputBlockTimer = sideInputBlockAfterRespawn;

        ZeroRigidbodyMotion();

        SnapToLaneByWorldX(respawnX);

        flightStartX = rb.position.x;
        flightStartY = rb.position.y;

        currentFlightX = flightStartX;
        currentFlightY = flightStartY;

        ResetFlightSteps();

        if (anim != null)
        {
            ResetAnimatorTriggers();
            SetAnimatorGrounded(false);
        }
    }

    private void CacheModelStartTransform()
    {
        if (animatedModelRoot == null)
            return;

        modelStartLocalPosition = animatedModelRoot.localPosition;
        modelStartLocalRotation = animatedModelRoot.localRotation;
        modelStartLocalScale = animatedModelRoot.localScale;

        hasModelStartTransform = true;
    }

    private void ResetModelToStartTransform()
    {
        if (!hasModelStartTransform)
            return;

        if (animatedModelRoot == null)
            return;

        animatedModelRoot.localPosition = modelStartLocalPosition;
        animatedModelRoot.localRotation = modelStartLocalRotation;
        animatedModelRoot.localScale = modelStartLocalScale;
    }

    private void SetFlightStartPosition()
    {
        flightStartX = GetFlightStartX();
        flightStartY = GetFlightStartY();

        currentFlightX = flightStartX;
        currentFlightY = flightStartY;
    }

    private float GetFlightStartX()
    {
        if (useFixedFlightBaseX)
            return fixedFlightBaseX;

        if (!snapFlightXToNearestLane)
            return rb.position.x;

        float rawStep = (rb.position.x - startX) / stepSize;
        step = Mathf.RoundToInt(rawStep);
        step = Mathf.Clamp(step, -maxSteps, maxSteps);

        return startX + step * stepSize;
    }

    private float GetFlightStartY()
    {
        if (useFixedFlightBaseY)
            return fixedFlightBaseY;

        return rb.position.y + flightStartYOffset;
    }

    private void MoveToFlightStartPosition()
    {
        Vector3 pos = rb.position;
        rb.position = new Vector3(flightStartX, flightStartY, pos.z);
    }

    private void EnterFlightAnimationState()
    {
        if (anim == null)
            return;

        anim.enabled = true;

        ResetAnimatorTriggers();
        SetAnimatorGrounded(false);

        anim.Rebind();
        anim.Update(0f);

        ResetModelToStartTransform();

        if (disableAnimatorInFlight)
            anim.enabled = false;
    }

    private void ExitFlightAnimationState()
    {
        if (!disableAnimatorInFlight || anim == null)
            return;

        anim.enabled = true;

        ResetAnimatorTriggers();
        SetAnimatorGrounded(false);

        anim.Update(0f);
    }

    private bool IsLeftHeld()
    {
        return GameInput.IsKeyHeld(KeyCode.LeftArrow) || GameInput.IsKeyHeld(KeyCode.A);
    }

    private bool IsRightHeld()
    {
        return GameInput.IsKeyHeld(KeyCode.RightArrow) || GameInput.IsKeyHeld(KeyCode.D);
    }

    private bool IsJumpHeld()
    {
        return GameInput.IsKeyHeld(KeyCode.UpArrow) || GameInput.IsKeyHeld(KeyCode.W);
    }

    private bool IsDashHeld()
    {
        return GameInput.IsKeyHeld(KeyCode.DownArrow) || GameInput.IsKeyHeld(KeyCode.S);
    }

    private bool IsDashPressed()
    {
        return GameInput.WasKeyPressedThisFrame(KeyCode.DownArrow) ||
               GameInput.WasKeyPressedThisFrame(KeyCode.S);
    }

    private void UpdateTimers()
    {
        TickTimer(ref sideInputBlockTimer);
        TickTimer(ref dashCooldownTimer);
        TickTimer(ref jumpBlockAfterDashTimer);
    }

    private void TickTimer(ref float timer)
    {
        if (timer > 0f)
            timer -= Time.deltaTime;
    }

    private bool IsNearLaneCenter()
    {
        float targetX = startX + step * stepSize;
        return Mathf.Abs(rb.position.x - targetX) <= laneSnapThreshold;
    }

    private void MoveLaneLeft()
    {
        step++;
        SetAnimatorTrigger(LeftTrigger);
    }

    private void MoveLaneRight()
    {
        step--;
        SetAnimatorTrigger(RightTrigger);
    }

    private void SetAnimatorTrigger(string triggerName)
    {
        if (anim != null)
            anim.SetTrigger(triggerName);
    }

    private void ResetJumpDashState()
    {
        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        dashBufferCounter = 0f;
        dashCooldownTimer = 0f;
        jumpBlockAfterDashTimer = 0f;

        hasJumpedThisAir = false;
        hasDashedInAir = false;

        canAirJump = false;
        currentAirJumpObject = null;
    }

    private void ResetFlightSteps()
    {
        flightHorizontalStep = 0;
        flightVerticalStep = 0;
    }

    private void ZeroRigidbodyMotion()
    {
        if (rb == null)
            return;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    private void ResetAnimatorTriggers()
    {
        if (anim == null)
            return;

        anim.ResetTrigger(LeftTrigger);
        anim.ResetTrigger(RightTrigger);
        anim.ResetTrigger(JumpTrigger);
        anim.ResetTrigger(FallTrigger);
    }

    private void SetAnimatorGrounded(bool value)
    {
        if (anim != null && HasBoolParameter(anim, GroundedBool))
            anim.SetBool(GroundedBool, value);
    }

    private bool HasBoolParameter(
        Animator animator,
        string paramName
    )
    {
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName &&
                param.type == AnimatorControllerParameterType.Bool)
            {
                return true;
            }
        }

        return false;
    }
}
