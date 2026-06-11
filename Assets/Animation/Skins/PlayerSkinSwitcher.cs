using UnityEngine;

public class PlayerSkinSwitcher : MonoBehaviour
{
    [Header("Skins")]
    public GameObject[] skins;

    [Header("Optional")]
    public bool applyOnStart = true;

    [Header("Links")]
    public PlayerSkinVFXController skinVFXController;
    public SkinAnimatorModeController skinAnimatorModeController;

    private const string SkinKey = "SelectedSkin";

    private int currentSkinIndex = 0;
    private GameObject currentSkinObject;

    void Start()
    {
        if (applyOnStart)
        {
            int savedSkin = PlayerPrefs.GetInt(SkinKey, 0);
            ApplySkin(savedSkin);
        }
    }

    public void ApplySkin(int index)
    {
        if (skins == null || skins.Length == 0)
        {
            currentSkinIndex = 0;
            currentSkinObject = null;
            NotifyControllers();
            return;
        }

        if (index < 0 || index >= skins.Length)
            index = 0;

        currentSkinIndex = index;

        for (int i = 0; i < skins.Length; i++)
        {
            if (skins[i] != null)
                skins[i].SetActive(i == currentSkinIndex);
        }

        currentSkinObject = GetSkinObjectByIndex(currentSkinIndex);
        NotifyControllers();
    }

    public void ReloadSavedSkin()
    {
        ApplySkin(PlayerPrefs.GetInt(SkinKey, 0));
    }

    public int GetCurrentSkinIndex()
    {
        return currentSkinIndex;
    }

    public GameObject GetCurrentSkinObject()
    {
        if (currentSkinObject == null)
            currentSkinObject = GetSkinObjectByIndex(currentSkinIndex);

        return currentSkinObject;
    }

    public Transform GetCurrentSkinTransform()
    {
        GameObject skinObj = GetCurrentSkinObject();
        return skinObj != null ? skinObj.transform : null;
    }

    public bool IsSkinActive(int index)
    {
        return currentSkinIndex == index;
    }

    private GameObject GetSkinObjectByIndex(int index)
    {
        if (skins == null || skins.Length == 0)
            return null;

        if (index < 0 || index >= skins.Length)
            return null;

        return skins[index];
    }

    private void NotifyControllers()
    {
        if (skinVFXController != null)
            skinVFXController.ApplyCurrentSkinProfile();

        if (skinAnimatorModeController != null)
            skinAnimatorModeController.ApplyCurrentMode();
    }
}