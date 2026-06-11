using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private DeathScript deathScript;

    void Start()
    {
        deathScript = GetComponent<DeathScript>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Obstacle"))
            return;

        Debug.Log("Dead from obstacle trigger");

        if (deathScript != null)
            deathScript.DieFromCollision();
    }
}