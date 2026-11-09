using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TemporaryUpgradeCard : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text tierText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Button buyButton;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Colores por rango")]
    [SerializeField] private Color cheapColor = new Color(0.6f, 0.85f, 1f);
    [SerializeField] private Color midColor = new Color(0.8f, 0.6f, 1f);
    [SerializeField] private Color expensiveColor = new Color(1f, 0.8f, 0.3f);

    [Header("Colores de precio")]
    [SerializeField] private Color affordableColor = Color.white;
    [SerializeField] private Color tooExpensiveColor = new Color(1f, 0.35f, 0.35f);

    private TemporaryUpgradeData data;
    private TemporaryUpgrades upgrades;

    public Button BuyButton => buyButton;

    public void Setup(TemporaryUpgradeData cardData, TemporaryUpgrades source, Action<TemporaryUpgradeData> onBuy)
    {
        data = cardData;
        upgrades = source;

        Sprite sprite = data.GetIcon(upgrades.Settings);
        icon.sprite = sprite;
        icon.enabled = sprite != null;

        nameText.text = data.displayName;
        tierText.text = data.tier.ToString();
        tierText.color = GetTierColor(data.tier);
        effectText.text = data.GetEffectLine(upgrades.Settings);

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => onBuy?.Invoke(data));
    }

    public void Refresh()
    {
        if (data == null) return;

        int count = upgrades.GetCount(data.category);
        int max = upgrades.Settings.maxTemporaryPerCategory;
        countText.text = $"{count}/{max} esta noche";

        if (upgrades.IsSold(data))
        {
            priceText.text = "COMPRADA";
            priceText.color = affordableColor;
            buyButton.interactable = false;
            canvasGroup.alpha = 0.5f;
            return;
        }

        bool canAfford = upgrades.NightMoney >= data.price;
        priceText.text = TemporaryUpgrades.FormatUnits(data.price);
        priceText.color = canAfford ? affordableColor : tooExpensiveColor;
        buyButton.interactable = upgrades.CanBuy(data);
        canvasGroup.alpha = 1f;
    }

    private Color GetTierColor(UpgradeTier tier)
    {
        switch (tier)
        {
            case UpgradeTier.Barata: return cheapColor;
            case UpgradeTier.Intermedia: return midColor;
            default: return expensiveColor;
        }
    }
}