using System;
using UnityEngine;

[Serializable]
public class GhostRunFrame
{
    public float time;
    public Vector3 position;
    public Quaternion rotation;
    public bool alive;
    public int skinIndex;

    public GhostRunFrame(
        float time,
        Vector3 position,
        Quaternion rotation,
        bool alive,
        int skinIndex
    )
    {
        this.time = time;
        this.position = position;
        this.rotation = rotation;
        this.alive = alive;
        this.skinIndex = skinIndex;
    }
}
