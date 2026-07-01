using System;
using UnityEngine;

[Serializable]
public class RacePlayerSnapshot
{
    public float time;
    public Vector3 position;
    public Quaternion rotation;
    public bool alive;
    public bool finished;
    public int skinIndex;

    public RacePlayerSnapshot()
    {
    }

    public RacePlayerSnapshot(
        float time,
        Vector3 position,
        Quaternion rotation,
        bool alive,
        bool finished,
        int skinIndex
    )
    {
        this.time = time;
        this.position = position;
        this.rotation = rotation;
        this.alive = alive;
        this.finished = finished;
        this.skinIndex = skinIndex;
    }

    public static RacePlayerSnapshot FromGhostFrame(GhostRunFrame frame)
    {
        if (frame == null)
            return null;

        return new RacePlayerSnapshot(
            frame.time,
            frame.position,
            frame.rotation,
            frame.alive,
            false,
            frame.skinIndex
        );
    }

    public static RacePlayerSnapshot Lerp(RacePlayerSnapshot a, RacePlayerSnapshot b, float t)
    {
        if (a == null)
            return b != null ? b.Clone() : null;

        if (b == null)
            return a.Clone();

        t = Mathf.Clamp01(t);

        return new RacePlayerSnapshot(
            Mathf.Lerp(a.time, b.time, t),
            Vector3.Lerp(a.position, b.position, t),
            Quaternion.Slerp(a.rotation, b.rotation, t),
            a.alive || b.alive,
            a.finished || b.finished,
            t < 0.5f ? a.skinIndex : b.skinIndex
        );
    }

    public GhostRunFrame ToGhostFrame()
    {
        return new GhostRunFrame(
            time,
            position,
            rotation,
            alive,
            skinIndex
        );
    }

    public RacePlayerSnapshot Clone()
    {
        return new RacePlayerSnapshot(
            time,
            position,
            rotation,
            alive,
            finished,
            skinIndex
        );
    }
}
