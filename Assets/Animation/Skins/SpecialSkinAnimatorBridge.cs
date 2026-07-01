using UnityEngine;

public class SpecialSkinAnimatorBridge : MonoBehaviour
{
    [Header("References")]
    public Animator specialAnimator;
    public PlayerMove playerMove;
    public Rigidbody playerRb;

    [Header("Optional Params")]
    public string groundedBool = "isGrounded";
    public string moveFloat = "moveSpeed";
    public string verticalFloat = "verticalSpeed";
    public string jumpTrigger = "jump";
    public string fallBool = "isFalling";

    [Header("Lane / Dash Triggers")]
    public string leftTrigger = "left";
    public string rightTrigger = "right";
    public string dashTrigger = "fall";

    private bool lastGrounded;
    private bool lastLeftPressed;
    private bool lastRightPressed;
    private bool lastDashPressed;

    void Start()
    {
        if (specialAnimator == null)
            specialAnimator = GetComponentInChildren<Animator>(true);

        if (playerMove == null)
            playerMove = GetComponentInParent<PlayerMove>();

        if (playerRb == null)
            playerRb = GetComponentInParent<Rigidbody>();

        if (playerMove != null)
            lastGrounded = playerMove.IsGrounded();
    }

    void Update()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (specialAnimator == null || playerMove == null)
            return;

        bool grounded = playerMove.IsGrounded();
        float moveSpeed = 0f;
        float verticalSpeed = 0f;

        if (playerRb != null)
        {
            moveSpeed = Mathf.Abs(playerRb.linearVelocity.z);
            verticalSpeed = playerRb.linearVelocity.y;
        }

        SetBoolIfExists(groundedBool, grounded);
        SetFloatIfExists(moveFloat, moveSpeed);
        SetFloatIfExists(verticalFloat, verticalSpeed);
        SetBoolIfExists(fallBool, !grounded && verticalSpeed < -0.05f);

        // Jump trigger
        if (lastGrounded && !grounded && verticalSpeed > 0.05f)
            SetTriggerIfExists(jumpTrigger);

        bool leftPressed = GameInput.IsKeyHeld(KeyCode.A) ||
                           GameInput.IsKeyHeld(KeyCode.LeftArrow);
        bool rightPressed = GameInput.IsKeyHeld(KeyCode.D) ||
                            GameInput.IsKeyHeld(KeyCode.RightArrow);
        bool dashPressed = GameInput.IsKeyHeld(KeyCode.S) ||
                           GameInput.IsKeyHeld(KeyCode.DownArrow);

        if (leftPressed && !lastLeftPressed)
            SetTriggerIfExists(leftTrigger);

        if (rightPressed && !lastRightPressed)
            SetTriggerIfExists(rightTrigger);

        if (dashPressed && !lastDashPressed)
            SetTriggerIfExists(dashTrigger);

        lastGrounded = grounded;
        lastLeftPressed = leftPressed;
        lastRightPressed = rightPressed;
        lastDashPressed = dashPressed;
    }

    void SetBoolIfExists(string paramName, bool value)
    {
        if (HasParameter(paramName, AnimatorControllerParameterType.Bool))
            specialAnimator.SetBool(paramName, value);
    }

    void SetFloatIfExists(string paramName, float value)
    {
        if (HasParameter(paramName, AnimatorControllerParameterType.Float))
            specialAnimator.SetFloat(paramName, value);
    }

    void SetTriggerIfExists(string paramName)
    {
        if (HasParameter(paramName, AnimatorControllerParameterType.Trigger))
            specialAnimator.SetTrigger(paramName);
    }

    bool HasParameter(string paramName, AnimatorControllerParameterType type)
    {
        if (specialAnimator == null)
            return false;

        foreach (var param in specialAnimator.parameters)
        {
            if (param.name == paramName && param.type == type)
                return true;
        }

        return false;
    }
}
