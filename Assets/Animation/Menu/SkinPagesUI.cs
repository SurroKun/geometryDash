using UnityEngine;
using UnityEngine.UI;

public class SkinPagesUI : MonoBehaviour
{
    [Header("All skin icons in order")]
    public Sprite[] skinIcons;

    [Header("18 visible slot images")]
    public Image[] slotImages;

    [Header("Buttons")]
    public Button leftButton;
    public Button rightButton;
    public Button equipButton;

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
    }

    private void EquipCurrentSkin()
    {
        PlayerPrefs.SetInt(SkinKey, previewSkinIndex);
        PlayerPrefs.Save();

        Debug.Log("Equipped skin: " + previewSkinIndex);
    }

    private void UpdateButtons()
    {
        int maxPage = Mathf.CeilToInt((float)skinIcons.Length / skinsPerPage) - 1;

        leftButton.interactable = currentPage > 0;
        rightButton.interactable = currentPage < maxPage;
    }

    private void UpdateDots()
    {
        for (int i = 0; i < pageDots.Length; i++)
        {
            pageDots[i].color = i == currentPage ? activeDotColor : inactiveDotColor;
        }
    }
}