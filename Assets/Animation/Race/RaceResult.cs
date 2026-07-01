using System;

[Serializable]
public class RaceResult
{
    public bool localFinished;
    public bool remoteFinished;
    public float localFinishTime;
    public float remoteFinishTime;

    public bool HasWinner => localFinished || remoteFinished;
    public bool IsComplete => localFinished && remoteFinished;

    public string WinnerLabel
    {
        get
        {
            if (!localFinished && !remoteFinished)
                return "Race";

            if (localFinished && !remoteFinished)
                return "You win";

            if (!localFinished && remoteFinished)
                return "Opponent wins";

            float delta = localFinishTime - remoteFinishTime;
            if (Math.Abs(delta) < 0.01f)
                return "Draw";

            return delta < 0f ? "You win" : "Opponent wins";
        }
    }

    public void Reset()
    {
        localFinished = false;
        remoteFinished = false;
        localFinishTime = 0f;
        remoteFinishTime = 0f;
    }
}
