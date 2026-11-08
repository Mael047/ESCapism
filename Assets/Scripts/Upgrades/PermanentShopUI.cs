using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PermanentShopUI : MonoBehaviour
{
    [SerializeField] private UpgradeSettings settings;
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform cardContainer;
    [SerializeField] private PermanentUpgradeCard cardPrefab;
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private Button continueButton;
    [SerializeField] private PlayerStats playerStats;

    public event Action Closed;
    public bool IsOpen { get; private set; }

    private readonly List<PermanentUpgradeCard> cards = new List<PermanentUpgradeCard>();

    private void Awake()
    {
        panel.SetActive(false);
        continueButton.onClick.AddListener(Close);
        if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
    }

    private void Update()
    {
        // Mientras la tienda está abierta, el cursor siempre visible y libre
        if (IsOpen && Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void Open()
    {
        if (cards.Count == 0) BuildCards();

        IsOpen = true;
        panel.SetActive(true);

        // Pausar el juego y bloquear los controles del jugador
        Time.timeScale = 0f;
        InputSystem.actions.FindActionMap("Player")?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Refresh();
        SelectFirstAvailable();
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        panel.SetActive(false);

        Time.timeScale = 1f;
        InputSystem.actions.FindActionMap("Player")?.Enable();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerStats != null) playerStats.Recalculate();
        Closed?.Invoke();
    }

    private void BuildCards()
    {
        foreach (PermanentUpgradeData data in settings.permanents)
        {
            PermanentUpgradeCard card = Instantiate(cardPrefab, cardContainer);
            card.Setup(data, Buy);
            cards.Add(card);
        }
    }

    private void Buy(UpgradeCategory category)
    {
        if (!PermanentUpgrades.TryBuy(settings, category)) return;

        Refresh();
        EnsureSelection();
    }

    private void Refresh()
    {
        moneyText.text = PermanentUpgrades.FormatMoney(PermanentUpgrades.Data.money);
        foreach (PermanentUpgradeCard card in cards)
        {
            card.Refresh(settings);
        }
    }

    // ---------- Navegación con mando ----------

    private void SelectFirstAvailable()
    {
        foreach (PermanentUpgradeCard card in cards)
        {
            if (card.BuyButton.interactable)
            {
                Select(card.BuyButton.gameObject);
                return;
            }
        }
        Select(continueButton.gameObject);
    }

    // Si la carta seleccionada dejó de estar disponible (MAX o sin dinero), mover la selección
    private void EnsureSelection()
    {
        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        Selectable selectable = selected != null ? selected.GetComponent<Selectable>() : null;

        if (selectable == null || !selectable.interactable) SelectFirstAvailable();
    }

    private void Select(GameObject target)
    {
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(target);
    }
}