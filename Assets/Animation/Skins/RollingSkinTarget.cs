using UnityEngine;

public class RollingSkinTarget : MonoBehaviour
{
    public Transform rotationTarget;

    public Transform GetRotationTarget()
    {
        return rotationTarget != null ? rotationTarget : transform;
    }
}