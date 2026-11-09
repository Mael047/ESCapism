using System;
using System.Collections.Generic;
using UnityEngine;

public class TemporaryUpgrades : MonoBehaviour
{
    [SerializeField] private UpgradeSettings settings;
    [SerializeField] private PlayerStats playerStats;

    public UpgradeSettings Settings => settings;
    public float NightMoney { get; private set; }
    public int RerollsUsed { get; private set; }
    public IReadOnlyList<TemporaryUpgradeData> Offers => offers;

    public event Action Changed;

    private readonly int[] countPerCategory = new int[4];
    private readonly List<TemporaryUpgradeData> offers = new List<TemporaryUpgradeData>();
    private readonly HashSet<TemporaryUpgradeData> soldThisShop = new HashSet<TemporaryUpgradeData>();

    private void Awake()
    {
        if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
    }

    // ---------- Tienda ----------

    // Se llama al abrir cada tienda: da el dinero y genera ofertas nuevas
    public void BeginShop()
    {
        NightMoney += settings.nightMoneyPerShop;
        RerollsUsed = 0;
        GenerateOffers();
        Changed?.Invoke();
    }

    public int GetCount(UpgradeCategory category) => countPerCategory[(int)category];

    public bool IsCapped(UpgradeCategory category) =>
        GetCount(category) >= settings.maxTemporaryPerCategory;

    public bool IsSold(TemporaryUpgradeData data) => soldThisShop.Contains(data);

    public bool CanBuy(TemporaryUpgradeData data) =>
        !IsSold(data) && !IsCapped(data.category) && NightMoney >= data.price;

    public bool TryBuy(TemporaryUpgradeData data)
    {
        if (!CanBuy(data)) return false;

        NightMoney -= data.price;
        countPerCategory[(int)data.category]++;
        soldThisShop.Add(data);

        if (playerStats != null) playerStats.AddTemporaryBonus(data.category, data.effect);

        Debug.Log($"[Temporales] Compró {data.displayName}. Dinero de noche: {FormatUnits(NightMoney)}");
        Changed?.Invoke();
        return true;
    }

    // ---------- Girar ----------

    public bool HasRerollsLeft => RerollsUsed < settings.maxRerolls;

    public float RerollCost => settings.firstRerollCost + settings.rerollCostIncrease * RerollsUsed;

    public bool CanReroll => HasRerollsLeft && NightMoney >= RerollCost;

    public bool TryReroll()
    {
        if (!CanReroll) return false;

        NightMoney -= RerollCost;   // el costo se calcula antes de contar el giro
        RerollsUsed++;
        GenerateOffers();
        Changed?.Invoke();
        return true;
    }

    // ---------- Ofertas ----------

    private void GenerateOffers()
    {
        offers.Clear();
        soldThisShop.Clear();

        // Categorías que todavía no llegaron al tope
        var available = new List<UpgradeCategory>();
        foreach (UpgradeCategory category in Enum.GetValues(typeof(UpgradeCategory)))
        {
            if (!IsCapped(category)) available.Add(category);
        }
        Shuffle(available);

        var tiers = new List<UpgradeTier> { UpgradeTier.Barata, UpgradeTier.Intermedia, UpgradeTier.Cara };
        int count = Mathf.Min(3, available.Count);
        if (count < 3) Shuffle(tiers);   // si hay menos de 3, los rangos también al azar

        // Categorías ya mezcladas: cada una recibe un rango distinto
        for (int i = 0; i < count; i++)
        {
            TemporaryUpgradeData data = settings.GetTemporary(available[i], tiers[i]);
            if (data != null) offers.Add(data);
        }

        // Mostrar de la más barata a la más cara
        offers.Sort((a, b) => a.price.CompareTo(b.price));
    }

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public static string FormatUnits(float units) => $"{units:0.#} u";
}