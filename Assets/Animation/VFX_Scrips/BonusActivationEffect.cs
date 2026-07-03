using UnityEngine;

public class BonusActivationEffect : MonoBehaviour
{
    [Header("Lifetime")]
    public float lifeTime = 1.2f;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }
}