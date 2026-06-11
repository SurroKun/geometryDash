using System.Collections.Generic;
using UnityEngine;

public class SegmentAI : MonoBehaviour
{
    public Transform player;
    public SegmentLibrary library;

    public float spawnDistance = 50f;
    public float segmentLength = 10f;

    private float nextZ;

    private Queue<GameObject> activeSegments = new Queue<GameObject>();

    private float timeAlive;

    private GameObject lastSegment;

    void Start()
    {
        nextZ = player.position.z - spawnDistance;
    }

    void Update()
    {
        timeAlive += Time.deltaTime;

        SpawnLoop();
    }

    void SpawnLoop()
    {
        while (nextZ > player.position.z - spawnDistance)
        {
            SpawnSegment();
            nextZ -= segmentLength;
        }
    }

    void SpawnSegment()
    {
        int difficulty = GetDifficulty();

        GameObject prefab = ChooseSegment(difficulty);

        GameObject seg = Instantiate(prefab);
        seg.transform.position = new Vector3(0, 0, nextZ);

        activeSegments.Enqueue(seg);

        if (activeSegments.Count > 8)
        {
            GameObject old = activeSegments.Dequeue();
            Destroy(old);
        }
    }

    GameObject ChooseSegment(int difficulty)
    {
        GameObject[] pool;

        if (difficulty == 1) pool = library.easySegments;
        else if (difficulty == 2) pool = library.mediumSegments;
        else pool = library.hardSegments;

        GameObject chosen;

        int tries = 0;

        do
        {
            chosen = pool[Random.Range(0, pool.Length)];
            tries++;
        }
        while (tries < 5 && chosen == lastSegment);

        lastSegment = chosen;

        return chosen;
    }

    int GetDifficulty()
    {
        if (timeAlive < 10f) return 1;
        if (timeAlive < 20f) return 2;
        return 3;
    }
}