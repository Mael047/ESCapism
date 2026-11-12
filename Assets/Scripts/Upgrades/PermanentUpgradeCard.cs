using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PermanentUpgradeCard : MonoBehaviour
{
    [SerializeField] private Image backGround;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text effectText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Colores")]
    [SerializeField] private Color affordableColor = Color.green;
    [SerializeField] private Color tooExpensiveColor = new Color(1f, 0.35f, 0.35f);

    private UpgradeCategory category;

    public Button BuyButton => buyButton;

    public void Setup(PermanentUpgradeData data, Action<UpgradeCategory> onBuy)
    {
        category = data.category;

        backGround.sprite = data.backGround;
        backGround.enabled = data.backGround != null;
        icon.sprite = data.icon;
        icon.enabled = data.icon != null;
        nameText.text = data.displayName;
        effectText.text = data.GetEffectLine();

        buyButton.onClick.RemoveAllListeners();
        buyButton.onClick.AddListener(() => onBuy?.Invoke(category));
    }

    public void Refresh(UpgradeSettings settings)
    {
        int level = PermanentUpgrades.GetLevel(category);

        // Nivel máximo: gris con "MAX"
        if (level >= settings.maxLevel)
        {
            levelText.text = $"Nivel {level}/{settings.maxLevel}";
            priceText.text = "MAX";
            priceText.color = affordableColor;
            buyButton.interactable = false;
            canvasGroup.alpha = 0.5f;
            return;
        }

        int price = PermanentUpgrades.GetPrice(settings, category);
        bool canAfford = PermanentUpgrades.Data.money >= price;

        levelText.text = $"Nivel {level}";
        priceText.text = PermanentUpgrades.FormatMoney(price);
        priceText.color = canAfford ? affordableColor : tooExpensiveColor;   // rojo si no alcanza
        buyButton.interactable = canAfford;
        canvasGroup.alpha = 0.7f;
    }
}