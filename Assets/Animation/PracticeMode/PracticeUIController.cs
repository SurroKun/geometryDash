using UnityEngine;

public class PracticeUIController : MonoBehaviour
{
    public GameObject removeCheckpointButton;

    void Update()
    {
        if (removeCheckpointButton == null) return;

        removeCheckpointButton.SetActive(DeathMenuUI.PracticeModeActive);
    }
}