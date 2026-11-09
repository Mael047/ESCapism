using UnityEngine;

[CreateAssetMenu(fileName = "MoodSettings", menuName = "Upgrades/Mood Settings")]
public class MoodSettings : ScriptableObject
{
    [Header("Por cliente: estrés")]
    public int changeShortStress = 8;
    public int changeOverStress = 4;
    [Range(0f, 1f)] public float lowPatienceThreshold = 0.3f;
    public int lowPatienceStress = 5;
    [Range(0f, 1f)] public float highPatienceThreshold = 0.6f;
    public int highPatienceRelief = 5;

    [Header("Por cliente: depresión")]
    public int angryCustomerDepression = 15;
    public int streakBrokenDepression = 5;
    public int perfectCustomerRelief = 5;

    [Header("Racha activa (por nivel, al cerrar cada cliente)")]
    public int streakReliefPerLevel = 2;

    [Header("Al cerrar el turno")]
    public int noAngryCustomersRelief = 10;
    [Tooltip("Un día es bueno si ambos valores quedan por debajo de este número")]
    public int goodDayThreshold = 40;
    public int twoGoodDaysRelief = 20;
    public int threeGoodDaysRelief = 35;

    [Header("Noche")]
    [Tooltip("Castigo máximo a cada valor por noche")]
    public int maxNightPenalty = 20;

    // Castigo según la fracción de oleadas que faltaron por completar
    public int GetNightPenalty(int wavesCleared, int totalWaves)
    {
        if (totalWaves <= 0) return 0;
        float missing = 1f - Mathf.Clamp01((float)wavesCleared / totalWaves);
        return Mathf.RoundToInt(maxNightPenalty * missing);
    }
}