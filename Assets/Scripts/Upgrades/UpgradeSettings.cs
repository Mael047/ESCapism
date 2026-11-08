using System;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeSettings", menuName = "Upgrades/Upgrade Settings")]
public class UpgradeSettings : ScriptableObject
{
    [Serializable]
    public struct MoodBand
    {
        [Range(0, 100)] public int from;
        public float factor;
    }

    [Header("Economía")]
    [Tooltip("P: pago promedio de un turno del día 1, en centavos (5000 = $50.00)")]
    public int basePay = 5000;
    [Min(1)] public int maxLevel = 5;

    [Header("Mejoras permanentes")]
    public PermanentUpgradeData[] permanents;

    [Header("Estrés y depresión → rendimiento de la mejora afectada")]
    public MoodBand[] moodBands =
    {
        new MoodBand { from = 0,  factor = 1f   },   // Calmada
        new MoodBand { from = 30, factor = 0.8f },   // Tensa
        new MoodBand { from = 60, factor = 0.6f }    // Estresada
    };

    public PermanentUpgradeData Get(UpgradeCategory category)
    {
        foreach (PermanentUpgradeData data in permanents)
        {
            if (data != null && data.category == category) return data;
        }
        Debug.LogError($"[UpgradeSettings] Falta la mejora permanente de {category}");
        return null;
    }

    public float GetMoodFactor(int value)
    {
        float factor = 1f;
        int bestFrom = -1;

        foreach (MoodBand band in moodBands)
        {
            if (value >= band.from && band.from > bestFrom)
            {
                bestFrom = band.from;
                factor = band.factor;
            }
        }
        return factor;
    }
}