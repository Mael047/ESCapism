using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TemporaryShopUI : MonoBehaviour
{
    [SerializeField] private TemporaryUpgrades upgrades;
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private TemporaryUpgradeCard cardPrefab;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollText;
    [SerializeField] private Button continueButton;

    public bool IsOpen { get; private set; }

    private readonly List<TemporaryUpgradeCard> cards = new List<TemporaryUpgradeCard>();

    private void Awake()
    {
        panel.SetActive(false);
        if (upgrades == null) upgrades = FindAnyObjectByType<TemporaryUpgrades>();

        rerollButton.onClick.AddListener(Reroll);
        continueButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (upgrades != null) upgrades.Changed += Refresh;
    }

    private void OnDisable()
    {
        if (upgrades != null) upgrades.Changed -= Refresh;
    }

    private void Update()
    {
        // Mientras la tienda está abierta, el cursor siempre visible
        if (IsOpen && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Open()
    {
        IsOpen = true;
        panel.SetActive(true);
        MenuPause.Enter();

        upgrades.BeginShop();
        RebuildCards();
        SelectFirstAvailable();
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        panel.SetActive(false);
        MenuPause.Exit();
    }

    private void RebuildCards()
    {
        IReadOnlyList<TemporaryUpgradeData> offers = upgrades.Offers;

        // Crear las cartas que falten y reutilizar las existentes
        while (cards.Count < offers.Count)
        {
            cards.Add(Instantiate(cardPrefab, cardContainer));
        }

        for (int i = 0; i < cards.Count; i++)
        {
            bool used = i < offers.Count;
            cards[i].gameObject.SetActive(used);
            if (used) cards[i].Setup(offers[i], upgrades, Buy);
        }

        Refresh();
    }

    private void Buy(TemporaryUpgradeData data)
    {
        if (upgrades.TryBuy(data)) EnsureSelection();
    }

    private void Reroll()
    {
        if (!upgrades.TryReroll()) return;

        RebuildCards();
        SelectFirstAvailable();
    }

    private void Refresh()
    {
        if (!IsOpen) return;

        moneyText.text = $"Dinero: {TemporaryUpgrades.FormatUnits(upgrades.NightMoney)}";

        foreach (TemporaryUpgradeCard card in cards)
        {
            if (card.gameObject.activeSelf) card.Refresh();
        }

        rerollText.text = upgrades.HasRerollsLeft
            ? $"Girar ({TemporaryUpgrades.FormatUnits(upgrades.RerollCost)})"
            : "Sin giros";
        rerollButton.interactable = upgrades.CanReroll;
    }

    // ---------- Navegación con mando ----------

    private void SelectFirstAvailable()
    {
        foreach (TemporaryUpgradeCard card in cards)
        {
            if (card.gameObject.activeSelf && card.BuyButton.interactable)
            {
                Select(card.BuyButton.gameObject);
                return;
            }
        }
        Select(continueButton.gameObject);
    }

    private void EnsureSelection()
    {
        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        Selectable selectable = selected != null ? selected.GetComponent<Selectable>() : null;

        if (selectable == null || !selectable.interactable || !selected.activeInHierarchy)
        {
            SelectFirstAvailable();
        }
    }

    private void Select(GameObject target)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
    }
}