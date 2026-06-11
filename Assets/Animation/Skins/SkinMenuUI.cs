using UnityEngine;

public class SkinMenuUI : MonoBehaviour
{
    private const string SkinKey = "SelectedSkin";

    public void SelectSkin(int index)
    {
        PlayerPrefs.SetInt(SkinKey, index);
        PlayerPrefs.Save();

        Debug.Log("Skin selected: " + index);
    }

    public int GetSelectedSkin()
    {
        return PlayerPrefs.GetInt(SkinKey, 0);
    }
}