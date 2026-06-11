using UnityEngine;

public class GravityFlipBonus : MonoBehaviour
{
    [Header("Trigger Protection")]
    public float retriggerDelay = 0.15f;

    private float retriggerTimer = 0f;

    void Update()
    {
        if (retriggerTimer > 0f)
            retriggerTimer -= Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (retriggerTimer > 0f)
            return;

        PlayerGravityFlip gravityFlip = other.GetComponent<PlayerGravityFlip>();

        if (gravityFlip == null)
            gravityFlip = other.GetComponentInParent<PlayerGravityFlip>();

        if (gravityFlip == null)
            return;

        if (!gravityFlip.CanTriggerGravity())
            return;

        bool success = gravityFlip.ToggleGravity();

        if (!success)
            return;

        retriggerTimer = retriggerDelay;
    }
}