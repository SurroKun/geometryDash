using System.Collections.Generic;
using UnityEngine;

public class TileManager : MonoBehaviour
{
    public GameObject tilePrefab;
    public Transform player;

    public float tileLength = 730f;

    private List<GameObject> tiles = new List<GameObject>();

    void Start()
    {
        SpawnTile(0);
        SpawnTile(-tileLength);
    }

    void Update()
    {
        if (tiles.Count < 2) return;

        GameObject nextTile = tiles[1];

        // 🟢 СПАВН: когда игрок ЗАШЁЛ на второй тайл
        if (player.position.z <= nextTile.transform.position.z + tileLength)
        {
            SpawnNextTile();
        }

        // 🔴 УДАЛЕНИЕ: когда игрок прошёл половину второго тайла
        if (player.position.z <= nextTile.transform.position.z + tileLength / 2)
        {
            DeleteOldTile();
        }
    }

    void SpawnTile(float zPos)
    {
        GameObject tile = Instantiate(tilePrefab);
        tile.transform.position = new Vector3(0, 0, zPos);
        tiles.Add(tile);
    }

    void SpawnNextTile()
    {
        // защита от двойного спавна
        if (tiles.Count >= 3) return;

        GameObject lastTile = tiles[tiles.Count - 1];

        float newZ = lastTile.transform.position.z - tileLength;

        Debug.Log("СПАВН тайла на Z: " + newZ);

        SpawnTile(newZ);
    }

    void DeleteOldTile()
    {
        if (tiles.Count <= 2) return;

        Debug.Log("УДАЛЕНИЕ тайла");

        Destroy(tiles[0]);
        tiles.RemoveAt(0);
    }
}