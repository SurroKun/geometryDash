using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class SkinShopData
{
    public CurrencyType currencyType = CurrencyType.Base;
    public int cost = 100;
}

public class SkinPagesUI : MonoBehaviour
{
    [Header("All skin icons in order")]
    public Sprite[] skinIcons;

    [Header("18 visible slot images")]
    public Image[] slotImages;
    public Image[] lockedOverlays;

    [Header("Buttons")]
    public Button leftButton;
    public Button rightButton;
    public Button equipButton;
    public TMP_Text equipButtonText;

    [Header("Shop")]
    public SkinShopData[] skinShopData;
    public CurrencyDisplay currencyDisplay;

    [Header("Page dots")]
    public Image[] pageDots;
    public Color activeDotColor = Color.yellow;
    public Color inactiveDotColor = Color.white;

    [Header("Skin preview")]
    public PlayerSkinSwitcher previewSkinSwitcher;

    private const string SkinKey = "SelectedSkin";

    private int currentPage;
    private int skinsPerPage;
    private int previewSkinIndex = 0;

    private void Start()
    {
        skinsPerPage = slotImages.Length;

        leftButton.onClick.AddListener(PreviousPage);
        rightButton.onClick.AddListener(NextPage);
        equipButton.onClick.AddListener(EquipCurrentSkin);

        previewSkinIndex = PlayerPrefs.GetInt(SkinKey, 0);

        ShowPage(0);

        if (previewSkinSwitcher != null)
            previewSkinSwitcher.ApplySkin(previewSkinIndex);
    }

    public void NextPage()
    {
        int maxPage = Mathf.CeilToInt((float)skinIcons.Length / skinsPerPage) - 1;

        if (currentPage < maxPage)
            ShowPage(currentPage + 1);
    }

    public void PreviousPage()
    {
        if (currentPage > 0)
            ShowPage(currentPage - 1);
    }

    private void ShowPage(int page)
    {
        currentPage = page;

        for (int i = 0; i < slotImages.Length; i++)
        {
            int skinIndex = currentPage * skinsPerPage + i;
            Button slotButton = slotImages[i].GetComponentInParent<Button>();

            if (skinIndex < skinIcons.Length)
            {
                slotImages[i].enabled = true;
                slotImages[i].sprite = skinIcons[skinIndex];
                slotImages[i].color = GameProgress.IsSkinUnlocked(skinIndex)
                    ? Color.white
                    : new Color(0.45f, 0.45f, 0.45f, 1f);

                if (lockedOverlays != null && i < lockedOverlays.Length && lockedOverlays[i] != null)
                    lockedOverlays[i].gameObject.SetActive(!GameProgress.IsSkinUnlocked(skinIndex));

                if (slotButton != null)
                {
                    int capturedIndex = skinIndex;

                    slotButton.onClick.RemoveAllListeners();
                    slotButton.onClick.AddListener(() =>
                    {
                        PreviewSkin(capturedIndex);
                    });
                }
            }
            else
            {
                slotImages[i].enabled = false;

                if (lockedOverlays != null && i < lockedOverlays.Length && lockedOverlays[i] != null)
                    lockedOverlays[i].gameObject.SetActive(false);

                if (slotButton != null)
                    slotButton.onClick.RemoveAllListeners();
            }
        }

        UpdateButtons();
        UpdateDots();
    }

    private void PreviewSkin(int skinIndex)
    {
        previewSkinIndex = skinIndex;

        if (previewSkinSwitcher != null)
            previewSkinSwitcher.ApplySkin(previewSkinIndex);

        Debug.Log("Preview skin: " + previewSkinIndex);
        UpdateEquipButton();
    }

    private void EquipCurrentSkin()
    {
        if (!GameProgress.IsSkinUnlocked(previewSkinIndex))
        {
            SkinShopData data = GetSkinShopData(previewSkinIndex);
            bool bought = GameProgress.BuySkin(previewSkinIndex, data.currencyType, data.cost);

            if (!bought)
            {
                Debug.Log("Not enough currency for skin: " + previewSkinIndex);
                UpdateEquipButton();
                return;
            }
        }

        GameProgress.SelectSkin(previewSkinIndex);

        Debug.Log("Equipped skin: " + previewSkinIndex);
        ShowPage(currentPage);
        UpdateEquipButton();

        if (currencyDisplay != null)
            currencyDisplay.Refresh();
    }

    private void UpdateButtons()
    {
        int maxPage = Mathf.CeilToInt((float)skinIcons.Length / skinsPerPage) - 1;

        leftButton.interactable = currentPage > 0;
        rightButton.interactable = currentPage < maxPage;
        UpdateEquipButton();
    }

    private void UpdateDots()
    {
        for (int i = 0; i < pageDots.Length; i++)
        {
            pageDots[i].color = i == currentPage ? activeDotColor : inactiveDotColor;
        }
    }

    private void UpdateEquipButton()
    {
        if (equipButtonText == null)
            return;

        if (GameProgress.IsSkinUnlocked(previewSkinIndex))
        {
            int selectedSkin = PlayerPrefs.GetInt(SkinKey, 0);
            equipButtonText.text = selectedSkin == previewSkinIndex ? "EQUIPPED" : "EQUIP";
            return;
        }

        SkinShopData data = GetSkinShopData(previewSkinIndex);
        string currencyName = data.currencyType == CurrencyType.Premium ? "P" : "B";
        equipButtonText.text = "BUY " + data.cost + " " + currencyName;
    }

    private SkinShopData GetSkinShopData(int skinIndex)
    {
        if (skinShopData != null && skinIndex >= 0 && skinIndex < skinShopData.Length && skinShopData[skinIndex] != null)
            return skinShopData[skinIndex];

        SkinShopData fallback = new SkinShopData();
        fallback.currencyType = CurrencyType.Base;
        fallback.cost = 100;
        return fallback;
    }
}
