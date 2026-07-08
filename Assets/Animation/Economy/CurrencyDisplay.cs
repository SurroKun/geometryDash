using TMPro;
using UnityEngine;

public class CurrencyDisplay : MonoBehaviour
{
    [Header("Text")]
    public TMP_Text baseCoinsText;
    public TMP_Text premiumCoinsText;
    public TMP_Text runPremiumCoinsText;

    [Header("Format")]
    public string baseFormat = "{0}";
    public string premiumFormat = "{0}";

    private void OnEnable()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (baseCoinsText != null)
            baseCoinsText.text = string.Format(baseFormat, GameProgress.BaseCoins);

        if (premiumCoinsText != null)
            premiumCoinsText.text = string.Format(premiumFormat, GameProgress.PremiumCoins);

        if (runPremiumCoinsText != null)
        {
            RunCurrencyCollector collector = RunCurrencyCollector.Instance;
            int runPremium = collector != null ? collector.GetCollectedPremiumCount() : 0;
            runPremiumCoinsText.text = runPremium.ToString();
        }
    }
}
