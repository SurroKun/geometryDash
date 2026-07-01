using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    private const string ObstacleTag = "Obstacle";

    private DeathScript deathScript;

    void Start()
    {
        deathScript = GetComponent<DeathScript>();
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(ObstacleTag))
            return;

        Debug.Log("Dead from obstacle trigger");

        if (deathScript != null)
            deathScript.DieFromCollision();
    }
}
