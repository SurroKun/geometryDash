using UnityEngine;

public class SkinMenuUI : MonoBehaviour
{
    private const string SkinKey = "SelectedSkin";

    public void SelectSkin(int index)
    {
        if (!GameProgress.IsSkinUnlocked(index))
        {
            Debug.Log("Skin is locked: " + index);
            return;
        }

        GameProgress.SelectSkin(index);

        Debug.Log("Skin selected: " + index);
    }

    public int GetSelectedSkin()
    {
        return PlayerPrefs.GetInt(SkinKey, 0);
    }
}
