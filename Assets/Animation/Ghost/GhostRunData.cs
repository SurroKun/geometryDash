using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GhostRunData
{
    public string sceneName;
    public int skinIndex;
    public float duration;
    public List<GhostRunFrame> frames = new List<GhostRunFrame>();

    public bool HasFrames()
    {
        return frames != null && frames.Count > 0;
    }

    public void Clear()
    {
        duration = 0f;

        if (frames == null)
            frames = new List<GhostRunFrame>();
        else
            frames.Clear();
    }
}
