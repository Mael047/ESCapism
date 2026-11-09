using System;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeSettings", menuName = "Upgrades/Upgrade Settings")]
public class UpgradeSettings : ScriptableObject
{
    [Serializable]
    public struct MoodBand
    {
        [Range(0, 100)] public int from;
        [Tooltip("Rendimiento del bono de la permanente (0.8 = 80%)")]
        public float factor;
        [Tooltip("Penalización a la estadística base (0.1 = −10%)")]
        public float basePenalty;
    }

    [Header("Economía")]
    [Tooltip("P: pago promedio de un turno del día 1, en centavos (5000 = $50.00)")]
    public int basePay = 5000;
    [Min(1)] public int maxLevel = 5;

    [Header("Mejoras permanentes")]
    public PermanentUpgradeData[] permanents;

    [Header("Estrés y depresión → rendimiento y penalización base")]
    public MoodBand[] moodBands =
    {
        new MoodBand { from = 0,  factor = 1f,   basePenalty = 0f   },   // Calmada
        new MoodBand { from = 30, factor = 0.8f, basePenalty = 0.1f },   // Tensa
        new MoodBand { from = 60, factor = 0.6f, basePenalty = 0.2f }    // Estresada
    };

    [Header("Mejoras temporales")]
    public TemporaryUpgradeData[] temporaries;
    [Tooltip("Dinero de noche que se da al abrir cada tienda (u)")]
    public float nightMoneyPerShop = 6f;
    [Min(1)] public int maxTemporaryPerCategory = 2;

    [Header("Girar (reroll)")]
    public float firstRerollCost = 0.5f;
    public float rerollCostIncrease = 0.5f;
    [Tooltip("Giros permitidos por tienda")]
    [Min(0)] public int maxRerolls = 1;

    public PermanentUpgradeData Get(UpgradeCategory category)
    {
        foreach (PermanentUpgradeData data in permanents)
        {
            if (data != null && data.category == category) return data;
        }
        Debug.LogError($"[UpgradeSettings] Falta la mejora permanente de {category}");
        return null;
    }

    private MoodBand GetBand(int value)
    {
        MoodBand best = new MoodBand { from = -1, factor = 1f, basePenalty = 0f };
        foreach (MoodBand band in moodBands)
        {
            if (value >= band.from && band.from > best.from) best = band;
        }
        return best;
    }

    public float GetMoodFactor(int value) => GetBand(value).factor;

    public float GetMoodBasePenalty(int value) => GetBand(value).basePenalty;

    public TemporaryUpgradeData GetTemporary(UpgradeCategory category, UpgradeTier tier)
    {
        foreach (TemporaryUpgradeData data in temporaries)
        {
            if (data != null && data.category == category && data.tier == tier) return data;
        }
        Debug.LogError($"[UpgradeSettings] Falta la temporal {category} {tier}");
        return null;
    }
}