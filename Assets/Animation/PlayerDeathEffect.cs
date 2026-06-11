using UnityEngine;
using System.Collections;

public class PlayerDeathEffect : MonoBehaviour
{
    [Header("References")]
    public GameObject aliveModel;
    public Transform animatedModelRoot;
    public Animator aliveAnimator;
    public Transform fragmentsRoot;
    public Rigidbody playerRb;
    public SkinAnimatorModeController skinAnimatorModeController;

    [Header("Movement Inheritance")]
    public float inheritedVelocityMultiplier = 0.8f;
    public float minStoredVelocity = 0.05f;

    [Header("Explosion Settings")]
    public float forwardForce = 1.2f;
    public float sideForce = 0.5f;
    public float upwardForce = 0.15f;
    public float randomTorque = 1.0f;

    [Header("Cleanup Settings")]
    public float shrinkDelay = 0.8f;
    public float shrinkDuration = 0.5f;
    public bool destroyFragmentsAfterShrink = false;

    [Header("Skin Override")]
    public Transform overrideFragmentsRoot;
    public Material overrideFragmentsMaterial;
    public Material[] overrideFragmentsMaterials;

    private Rigidbody[] partBodies;
    private Collider[] partColliders;
    private Transform[] partTransforms;
    private Renderer[] partRenderers;

    private Vector3[] initialLocalPositions;
    private Quaternion[] initialLocalRotations;
    private Vector3[] initialLocalScales;

    private Material[] originalMaterials;

    private Vector3 aliveModelInitialLocalPosition;
    private Quaternion aliveModelInitialLocalRotation;
    private Vector3 aliveModelInitialLocalScale;

    private Vector3 animatedModelInitialLocalPosition;
    private Quaternion animatedModelInitialLocalRotation;
    private Vector3 animatedModelInitialLocalScale;

    private bool exploded = false;
    private Vector3 lastStoredVelocity;

    private Transform activeFragmentsRoot;

    void Start()
    {
        if (playerRb == null)
            playerRb = GetComponent<Rigidbody>();

        if (aliveAnimator == null && aliveModel != null)
            aliveAnimator = aliveModel.GetComponentInChildren<Animator>(true);

        if (animatedModelRoot == null && aliveAnimator != null)
            animatedModelRoot = aliveAnimator.transform;

        if (skinAnimatorModeController == null)
            skinAnimatorModeController = GetComponentInChildren<SkinAnimatorModeController>(true);

        if (aliveModel != null)
        {
            Transform aliveT = aliveModel.transform;
            aliveModelInitialLocalPosition = aliveT.localPosition;
            aliveModelInitialLocalRotation = aliveT.localRotation;
            aliveModelInitialLocalScale = aliveT.localScale;
        }

        if (animatedModelRoot != null)
        {
            animatedModelInitialLocalPosition = animatedModelRoot.localPosition;
            animatedModelInitialLocalRotation = animatedModelRoot.localRotation;
            animatedModelInitialLocalScale = animatedModelRoot.localScale;
        }

        RebuildFragmentsCache();
        DeactivateFragmentsRoot();
    }

    void FixedUpdate()
    {
        if (playerRb == null || exploded)
            return;

        Vector3 currentVelocity = playerRb.linearVelocity;

        if (currentVelocity.sqrMagnitude > minStoredVelocity * minStoredVelocity)
            lastStoredVelocity = currentVelocity;
    }

    public void Explode()
    {
        if (exploded)
            return;

        if (!EnsureFragmentsReady())
            return;

        exploded = true;
        StopAllCoroutines();

        Vector3 inheritedVelocity = lastStoredVelocity * inheritedVelocityMultiplier;

        if (aliveModel != null)
            aliveModel.SetActive(false);

        activeFragmentsRoot.gameObject.SetActive(true);

        ApplyOverrideMaterialIfNeeded();

        for (int i = 0; i < partBodies.Length; i++)
        {
            Rigidbody part = partBodies[i];
            if (part == null)
                continue;

            Transform t = part.transform;
            t.SetParent(activeFragmentsRoot, true);

            part.isKinematic = false;
            part.useGravity = true;
            part.linearVelocity = inheritedVelocity;
            part.angularVelocity = Vector3.zero;

            Vector3 randomOffset = new Vector3(
                Random.Range(-sideForce, sideForce),
                Random.Range(-upwardForce * 0.3f, upwardForce),
                Random.Range(-0.3f, 0.3f)
            );

            Vector3 finalImpulse = transform.forward * forwardForce + randomOffset;

            part.AddForce(finalImpulse, ForceMode.Impulse);
            part.AddTorque(Random.insideUnitSphere * randomTorque, ForceMode.Impulse);
        }

        for (int i = 0; i < partColliders.Length; i++)
        {
            if (partColliders[i] != null)
                partColliders[i].enabled = true;
        }

        StartCoroutine(ShrinkAndCleanup());
    }

    public void RestorePlayer()
    {
        StopAllCoroutines();
        exploded = false;

        if (aliveModel != null)
        {
            aliveModel.SetActive(true);

            Transform aliveT = aliveModel.transform;
            aliveT.localPosition = aliveModelInitialLocalPosition;
            aliveT.localRotation = aliveModelInitialLocalRotation;
            aliveT.localScale = aliveModelInitialLocalScale;
        }

        if (animatedModelRoot != null)
        {
            animatedModelRoot.localPosition = animatedModelInitialLocalPosition;
            animatedModelRoot.localRotation = animatedModelInitialLocalRotation;
            animatedModelRoot.localScale = animatedModelInitialLocalScale;
        }

        if (aliveAnimator != null)
        {
            aliveAnimator.ResetTrigger("left");
            aliveAnimator.ResetTrigger("right");
            aliveAnimator.ResetTrigger("jump");
            aliveAnimator.ResetTrigger("fall");

            if (HasBoolParameter(aliveAnimator, "isGrounded"))
                aliveAnimator.SetBool("isGrounded", false);

            bool shouldUseBaseAnimator = true;

            if (skinAnimatorModeController != null)
                shouldUseBaseAnimator = skinAnimatorModeController.ShouldUseBaseAnimator();

            if (shouldUseBaseAnimator)
            {
                aliveAnimator.enabled = false;
                aliveAnimator.enabled = true;
                aliveAnimator.Update(0f);
            }
            else
            {
                aliveAnimator.enabled = false;
            }
        }

        if (!EnsureFragmentsReady())
            return;

        RestoreOriginalMaterials();

        for (int i = 0; i < partBodies.Length; i++)
        {
            Rigidbody part = partBodies[i];
            Transform t = partTransforms[i];

            if (part == null || t == null)
                continue;

            t.SetParent(activeFragmentsRoot, false);
            t.localPosition = initialLocalPositions[i];
            t.localRotation = initialLocalRotations[i];
            t.localScale = initialLocalScales[i];

            part.isKinematic = true;
            part.useGravity = false;
            part.linearVelocity = Vector3.zero;
            part.angularVelocity = Vector3.zero;
        }

        for (int i = 0; i < partColliders.Length; i++)
        {
            if (partColliders[i] != null)
                partColliders[i].enabled = false;
        }

        activeFragmentsRoot.gameObject.SetActive(false);
    }

    public void RebuildFragmentsCache()
    {
        if (fragmentsRoot != null)
            fragmentsRoot.gameObject.SetActive(false);

        if (overrideFragmentsRoot != null)
            overrideFragmentsRoot.gameObject.SetActive(false);

        activeFragmentsRoot = overrideFragmentsRoot != null ? overrideFragmentsRoot : fragmentsRoot;

        if (activeFragmentsRoot == null)
        {
            Debug.LogWarning("PlayerDeathEffect: fragmentsRoot не назначен.");
            partBodies = new Rigidbody[0];
            partColliders = new Collider[0];
            partTransforms = new Transform[0];
            partRenderers = new Renderer[0];
            initialLocalPositions = new Vector3[0];
            initialLocalRotations = new Quaternion[0];
            initialLocalScales = new Vector3[0];
            originalMaterials = new Material[0];
            return;
        }

        partBodies = activeFragmentsRoot.GetComponentsInChildren<Rigidbody>(true);
        partColliders = activeFragmentsRoot.GetComponentsInChildren<Collider>(true);
        partRenderers = activeFragmentsRoot.GetComponentsInChildren<Renderer>(true);

        partTransforms = new Transform[partBodies.Length];
        initialLocalPositions = new Vector3[partBodies.Length];
        initialLocalRotations = new Quaternion[partBodies.Length];
        initialLocalScales = new Vector3[partBodies.Length];

        for (int i = 0; i < partBodies.Length; i++)
        {
            if (partBodies[i] == null)
                continue;

            Transform t = partBodies[i].transform;
            partTransforms[i] = t;
            initialLocalPositions[i] = t.localPosition;
            initialLocalRotations[i] = t.localRotation;
            initialLocalScales[i] = t.localScale;
        }

        originalMaterials = new Material[partRenderers.Length];

        for (int i = 0; i < partRenderers.Length; i++)
        {
            if (partRenderers[i] != null)
                originalMaterials[i] = Application.isPlaying
                    ? partRenderers[i].material
                    : partRenderers[i].sharedMaterial;
        }

        for (int i = 0; i < partBodies.Length; i++)
        {
            Rigidbody part = partBodies[i];
            if (part == null)
                continue;

            part.isKinematic = true;
            part.useGravity = false;
            part.linearVelocity = Vector3.zero;
            part.angularVelocity = Vector3.zero;
            part.collisionDetectionMode = CollisionDetectionMode.Continuous;
        }

        for (int i = 0; i < partColliders.Length; i++)
        {
            if (partColliders[i] != null)
                partColliders[i].enabled = false;
        }
    }

    public void SetOverrideFragmentsRoot(Transform newRoot)
    {
        if (fragmentsRoot != null)
            fragmentsRoot.gameObject.SetActive(false);

        if (overrideFragmentsRoot != null)
            overrideFragmentsRoot.gameObject.SetActive(false);

        overrideFragmentsRoot = newRoot;

        RebuildFragmentsCache();
        DeactivateFragmentsRoot();
    }

    public void SetOverrideMaterial(Material newMaterial)
    {
        overrideFragmentsMaterial = newMaterial;
        overrideFragmentsMaterials = null;
        ApplyOverrideMaterialIfNeeded();
    }

    public void SetOverrideMaterials(Material[] newMaterials)
    {
        overrideFragmentsMaterials = newMaterials;
        overrideFragmentsMaterial = null;
        ApplyOverrideMaterialIfNeeded();
    }

    public void ClearOverrideMaterial()
    {
        overrideFragmentsMaterial = null;
        overrideFragmentsMaterials = null;
        RestoreOriginalMaterials();
    }

    private bool EnsureFragmentsReady()
    {
        if (activeFragmentsRoot == null)
            RebuildFragmentsCache();

        return activeFragmentsRoot != null;
    }

    private void ApplyOverrideMaterialIfNeeded()
    {
        if (partRenderers == null || partRenderers.Length == 0)
            return;

        if (overrideFragmentsMaterials != null && overrideFragmentsMaterials.Length > 0)
        {
            ApplyRandomMaterialsPerRenderer();
            return;
        }

        if (overrideFragmentsMaterial == null)
        {
            RestoreOriginalMaterials();
            return;
        }

        for (int i = 0; i < partRenderers.Length; i++)
        {
            if (partRenderers[i] != null)
                partRenderers[i].material = overrideFragmentsMaterial;
        }
    }

    private void ApplyRandomMaterialsPerRenderer()
    {
        Material[] validMaterials = GetValidOverrideMaterials();

        if (validMaterials == null || validMaterials.Length == 0)
        {
            RestoreOriginalMaterials();
            return;
        }

        for (int i = 0; i < partRenderers.Length; i++)
        {
            if (partRenderers[i] != null)
                partRenderers[i].material = validMaterials[Random.Range(0, validMaterials.Length)];
        }
    }

    private Material[] GetValidOverrideMaterials()
    {
        if (overrideFragmentsMaterials == null || overrideFragmentsMaterials.Length == 0)
            return null;

        int validCount = 0;

        for (int i = 0; i < overrideFragmentsMaterials.Length; i++)
        {
            if (overrideFragmentsMaterials[i] != null)
                validCount++;
        }

        if (validCount == 0)
            return null;

        Material[] validMaterials = new Material[validCount];
        int index = 0;

        for (int i = 0; i < overrideFragmentsMaterials.Length; i++)
        {
            if (overrideFragmentsMaterials[i] != null)
            {
                validMaterials[index] = overrideFragmentsMaterials[i];
                index++;
            }
        }

        return validMaterials;
    }

    private void RestoreOriginalMaterials()
    {
        if (partRenderers == null || originalMaterials == null)
            return;

        int count = Mathf.Min(partRenderers.Length, originalMaterials.Length);

        for (int i = 0; i < count; i++)
        {
            if (partRenderers[i] != null && originalMaterials[i] != null)
                partRenderers[i].material = originalMaterials[i];
        }
    }

    private void DeactivateFragmentsRoot()
    {
        if (activeFragmentsRoot != null)
            activeFragmentsRoot.gameObject.SetActive(false);
    }

    private IEnumerator ShrinkAndCleanup()
    {
        yield return new WaitForSeconds(shrinkDelay);

        Vector3[] startScales = new Vector3[partTransforms.Length];

        for (int i = 0; i < partTransforms.Length; i++)
        {
            if (partTransforms[i] != null)
                startScales[i] = partTransforms[i].localScale;
        }

        float timer = 0f;

        while (timer < shrinkDuration)
        {
            timer += Time.deltaTime;
            float t = timer / shrinkDuration;

            for (int i = 0; i < partTransforms.Length; i++)
            {
                if (partTransforms[i] != null)
                    partTransforms[i].localScale = Vector3.Lerp(startScales[i], Vector3.zero, t);
            }

            yield return null;
        }

        if (destroyFragmentsAfterShrink && !DeathMenuUI.PracticeModeActive)
        {
            for (int i = 0; i < partTransforms.Length; i++)
            {
                if (partTransforms[i] != null)
                    Destroy(partTransforms[i].gameObject);
            }
        }
        else
        {
            RestoreOriginalMaterials();

            for (int i = 0; i < partBodies.Length; i++)
            {
                Rigidbody part = partBodies[i];
                Transform t = partTransforms[i];

                if (part == null || t == null)
                    continue;

                part.isKinematic = true;
                part.useGravity = false;
                part.linearVelocity = Vector3.zero;
                part.angularVelocity = Vector3.zero;

                t.SetParent(activeFragmentsRoot, false);
                t.localPosition = initialLocalPositions[i];
                t.localRotation = initialLocalRotations[i];
                t.localScale = initialLocalScales[i];
            }

            for (int i = 0; i < partColliders.Length; i++)
            {
                if (partColliders[i] != null)
                    partColliders[i].enabled = false;
            }

            if (activeFragmentsRoot != null)
                activeFragmentsRoot.gameObject.SetActive(false);
        }
    }

    private bool HasBoolParameter(Animator animator, string paramName)
    {
        foreach (var param in animator.parameters)
        {
            if (param.name == paramName && param.type == AnimatorControllerParameterType.Bool)
                return true;
        }

        return false;
    }
}