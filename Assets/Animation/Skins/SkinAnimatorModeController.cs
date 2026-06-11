using UnityEngine;

public class SkinAnimatorModeController : MonoBehaviour
{
    [Header("References")]
    public PlayerSkinSwitcher skinSwitcher;
    public Animator targetAnimator;
    public PlayerSkinVFXController skinVFXController;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;

    void Awake()
    {
        if (targetAnimator == null)
            targetAnimator = GetComponent<Animator>();

        initialLocalPosition = transform.localPosition;
        initialLocalRotation = transform.localRotation;
        initialLocalScale = transform.localScale;
    }

    void Start()
    {
        ApplyCurrentMode();
    }

    public void ApplyCurrentMode()
    {
        if (targetAnimator == null || skinVFXController == null)
            return;

        bool useBaseAnimator = ShouldUseBaseAnimator();

        if (useBaseAnimator)
        {
            if (!targetAnimator.enabled)
            {
                targetAnimator.enabled = true;
                targetAnimator.Update(0f);
            }
        }
        else
        {
            targetAnimator.enabled = false;

            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
            transform.localScale = initialLocalScale;
        }
    }

    public bool ShouldUseBaseAnimator()
    {
        if (skinVFXController == null)
            return true;

        PlayerSkinVFXController.SkinAnimationMode mode = skinVFXController.GetAnimationModeForCurrentSkin();
        return mode == PlayerSkinVFXController.SkinAnimationMode.Default;
    }
}