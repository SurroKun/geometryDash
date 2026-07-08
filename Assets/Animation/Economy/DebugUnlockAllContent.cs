using UnityEngine;

public class DebugUnlockAllContent : MonoBehaviour
{
    [Header("Debug")]
    public bool unlockEverything = false;
    public bool applyOnStart = true;

    private void Start()
    {
        if (applyOnStart)
            Apply();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
            return;

        Apply();
    }

    public void Apply()
    {
        GameProgress.UnlockAllContent = unlockEverything;
        Debug.Log("Unlock all content: " + unlockEverything);
    }
}
