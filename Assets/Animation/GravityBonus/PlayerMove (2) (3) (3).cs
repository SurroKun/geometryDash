using UnityEngine;

public class PlayerMove : MonoBehaviour
{
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

        if (anim != null && HasBoolParameter(anim, "isGrounded"))
            anim.SetBool("isGrounded", false);
    }

    void Update()
    {
        if (sideInputBlockTimer > 0f)
            sideInputBlockTimer -= Time.deltaTime;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer -= Time.deltaTime;

        if (jumpBlockAfterDashTimer > 0f)
            jumpBlockAfterDashTimer -= Time.deltaTime;

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

        bool leftHeld = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        bool rightHeld = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);

        float targetX = startX + step * stepSize;
        bool nearLaneCenter = Mathf.Abs(rb.position.x - targetX) <= laneSnapThreshold;

        if (!nearLaneCenter)
            return;

        if (leftHeld == rightHeld)
            return;

        bool invertedNow = sideInputInverted;

        if (!invertedNow)
        {
            if (leftHeld && step < maxSteps)
            {
                step++;
                if (anim != null) anim.SetTrigger("left");
            }
            else if (rightHeld && step > -maxSteps)
            {
                step--;
                if (anim != null) anim.SetTrigger("right");
            }
        }
        else
        {
            if (leftHeld && step > -maxSteps)
            {
                step--;
                if (anim != null) anim.SetTrigger("right");
            }
            else if (rightHeld && step < maxSteps)
            {
                step++;
                if (anim != null) anim.SetTrigger("left");
            }
        }
    }

    void HandleJumpInput()
    {
        bool jumpHeld =
            Input.GetKey(KeyCode.UpArrow) ||
            Input.GetKey(KeyCode.W);

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

        if (jumpBufferCounter > 0f)
        {
            float jumpDirection = 1f;

            if (gravityFlip != null && gravityFlip.IsGravityInverted())
                jumpDirection = -1f;

            if (coyoteTimeCounter > 0f && !hasJumpedThisAir)
            {
                if (cancelSpeedDashBeforeNormalJump &&
                    speedDashBonus != null &&
                    speedDashBonus.IsDashing())
                {
                    speedDashBonus.CancelDashForOtherBonus();
                }

                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    0f,
                    rb.linearVelocity.z
                );

                rb.AddForce(
                    Vector3.up * jumpForce * jumpDirection,
                    ForceMode.Impulse
                );

                jumpBufferCounter = 0f;
                coyoteTimeCounter = 0f;
                hasJumpedThisAir = true;

                NotifyPracticeJump();

                if (anim != null)
                {
                    anim.ResetTrigger("fall");
                    anim.SetTrigger("jump");
                }
            }
            else if (canAirJump)
            {
                if (cancelSpeedDashBeforeAirJump &&
                    speedDashBonus != null &&
                    speedDashBonus.IsDashing())
                {
                    speedDashBonus.CancelDashForOtherBonus();
                }

                rb.linearVelocity = new Vector3(
                    rb.linearVelocity.x,
                    0f,
                    rb.linearVelocity.z
                );

                rb.AddForce(
                    Vector3.up * jumpForce * jumpDirection,
                    ForceMode.Impulse
                );

                jumpBufferCounter = 0f;

                canAirJump = false;
                currentAirJumpObject = null;

                NotifyPracticeJump();

                if (anim != null)
                {
                    anim.ResetTrigger("fall");
                    anim.SetTrigger("jump");
                }
            }
        }
    }

    private void NotifyPracticeJump()
    {
        if (practiceModeManager != null)
            practiceModeManager.NotifyPlayerJumped();
    }

    void HandleDashInput()
    {
        bool dashPressed =
            Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.S);

        bool dashHeld =
            Input.GetKey(KeyCode.DownArrow) ||
            Input.GetKey(KeyCode.S);

        if (dashPressed || dashHeld)
            dashBufferCounter = dashBufferTime;
        else
            dashBufferCounter -= Time.deltaTime;

        if (dashBufferCounter <= 0f)
            return;

        if (dashCooldownTimer > 0f)
            return;

        if (hasDashedInAir)
            return;

        if (isGrounded)
            return;

        float dashDirection = -1f;

        if (gravityFlip != null && gravityFlip.IsGravityInverted())
            dashDirection = 1f;

        if (speedDashBonus != null && speedDashBonus.IsDashing())
            speedDashBonus.CancelDashForOtherBonus();

        rb.linearVelocity = new Vector3(
            rb.linearVelocity.x,
            dashForce * dashDirection,
            rb.linearVelocity.z
        );

        dashCooldownTimer = dashCooldown;

        if (blockJumpRightAfterDash)
            jumpBlockAfterDashTimer = jumpBlockAfterDash;

        if (clearDashBufferAfterDash)
            dashBufferCounter = 0f;

        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;

        hasDashedInAir = true;
        hasJumpedThisAir = true;

        if (anim != null)
        {
            anim.ResetTrigger("jump");
            anim.SetTrigger("fall");
        }
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

        bool leftHeld = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A);
        bool rightHeld = Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D);
        bool upHeld = Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W);
        bool downHeld = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S);

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

        if (useFixedFlightBaseX)
            flightStartX = fixedFlightBaseX;
        else if (snapFlightXToNearestLane)
        {
            float rawStep = (rb.position.x - startX) / stepSize;
            step = Mathf.RoundToInt(rawStep);
            step = Mathf.Clamp(step, -maxSteps, maxSteps);
            flightStartX = startX + step * stepSize;
        }
        else
            flightStartX = rb.position.x;

        if (useFixedFlightBaseY)
            flightStartY = fixedFlightBaseY;
        else
            flightStartY = rb.position.y + flightStartYOffset;

        currentFlightX = flightStartX;
        currentFlightY = flightStartY;

        flightHorizontalStep = 0;
        flightVerticalStep = 0;

        jumpBufferCounter = 0f;
        dashBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        dashCooldownTimer = 0f;
        jumpBlockAfterDashTimer = 0f;

        hasJumpedThisAir = false;
        hasDashedInAir = false;

        canAirJump = false;
        currentAirJumpObject = null;

        isGrounded = false;

        if (speedDashBonus != null)
            speedDashBonus.CancelDashAndRestore();

        if (freezeVelocityOnFlightStart && rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (rb != null)
        {
            Vector3 pos = rb.position;
            rb.position = new Vector3(flightStartX, flightStartY, pos.z);
        }

        ResetModelToStartTransform();

        if (anim != null)
        {
            anim.enabled = true;

            anim.ResetTrigger("left");
            anim.ResetTrigger("right");
            anim.ResetTrigger("jump");
            anim.ResetTrigger("fall");

            if (HasBoolParameter(anim, "isGrounded"))
                anim.SetBool("isGrounded", false);

            anim.Rebind();
            anim.Update(0f);

            ResetModelToStartTransform();

            if (disableAnimatorInFlight)
                anim.enabled = false;
        }

        Debug.Log("Flight Mode ON");
    }

    public void StopFlightMode()
    {
        if (!isFlightModeActive)
            return;

        isFlightModeActive = false;

        jumpBufferCounter = 0f;
        dashBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        dashCooldownTimer = 0f;
        jumpBlockAfterDashTimer = 0f;

        hasJumpedThisAir = false;
        hasDashedInAir = false;

        canAirJump = false;
        currentAirJumpObject = null;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        if (disableAnimatorInFlight && anim != null)
        {
            anim.enabled = true;

            anim.ResetTrigger("left");
            anim.ResetTrigger("right");
            anim.ResetTrigger("jump");
            anim.ResetTrigger("fall");

            if (HasBoolParameter(anim, "isGrounded"))
                anim.SetBool("isGrounded", false);

            anim.Update(0f);
        }

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

        if (collision.gameObject.CompareTag("Ground") ||
            collision.gameObject.CompareTag("Obstacle"))
        {
            isGrounded = true;

            coyoteTimeCounter = coyoteTime;
            canAirJump = false;

            if (resetJumpWhenTouchGround)
                hasJumpedThisAir = false;

            if (resetDashWhenTouchGround)
                hasDashedInAir = false;

            if (anim != null && HasBoolParameter(anim, "isGrounded"))
                anim.SetBool("isGrounded", true);
        }
    }

    void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Ground") ||
            collision.gameObject.CompareTag("Obstacle"))
        {
            isGrounded = false;
            coyoteTimeCounter = 0f;

            if (anim != null && HasBoolParameter(anim, "isGrounded"))
                anim.SetBool("isGrounded", false);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("AirJump"))
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

        if (rb != null)
        {
            Vector3 pos = rb.position;
            pos.x = currentX;
            rb.position = pos;
        }
    }

    public void ResetAfterRespawn(float respawnX)
    {
        StopFlightMode();

        if (disableAnimatorInFlight && anim != null)
            anim.enabled = true;

        ResetModelToStartTransform();

        hasJumpedThisAir = false;
        hasDashedInAir = false;

        canAirJump = false;
        currentAirJumpObject = null;

        jumpBufferCounter = 0f;
        coyoteTimeCounter = 0f;
        dashBufferCounter = 0f;
        dashCooldownTimer = 0f;
        jumpBlockAfterDashTimer = 0f;

        if (speedDashBonus != null)
            speedDashBonus.CancelDashAndRestore();

        isGrounded = false;
        sideInputBlockTimer = sideInputBlockAfterRespawn;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        SnapToLaneByWorldX(respawnX);

        flightStartX = rb.position.x;
        flightStartY = rb.position.y;

        currentFlightX = flightStartX;
        currentFlightY = flightStartY;

        flightHorizontalStep = 0;
        flightVerticalStep = 0;

        if (anim != null)
        {
            anim.ResetTrigger("left");
            anim.ResetTrigger("right");
            anim.ResetTrigger("jump");
            anim.ResetTrigger("fall");

            if (HasBoolParameter(anim, "isGrounded"))
                anim.SetBool("isGrounded", false);
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