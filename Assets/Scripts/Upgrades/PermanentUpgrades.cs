using UnityEngine;

public static class PermanentUpgrades
{
    private static RunData testData;
    // Con la recarga de dominio desactivada, los static sobreviven entre sesiones de Play
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnPlay()
    {
        testData = null;
    }

    // Los datos de la partida. Sin GameManager (probando la Noche directamente) usa datos de prueba
    public static RunData Data
    {
        get
        {
            if (GameManager.Instance != null) return GameManager.Instance.Data;
            if (testData == null) testData = new RunData { money = 20000};   // $200 para probar
            return testData;
        }
    }

    public static int GetLevel(UpgradeCategory category)
    {
        return Data.permanentLevels[(int)category];
    }

    public static bool IsMaxed(UpgradeSettings settings, UpgradeCategory category)
    {
        return GetLevel(category) >= settings.maxLevel;
    }

    // Precio del siguiente nivel: P × multiplicador × nivel
    public static int GetPrice(UpgradeSettings settings, UpgradeCategory category)
    {
        PermanentUpgradeData data = settings.Get(category);
        int nextLevel = GetLevel(category) + 1;
        return Mathf.RoundToInt(settings.basePay * data.priceMultiplier * nextLevel);
    }

    public static bool CanBuy(UpgradeSettings settings, UpgradeCategory category)
    {
        return !IsMaxed(settings, category) && Data.money >= GetPrice(settings, category);
    }

    public static bool TryBuy(UpgradeSettings settings, UpgradeCategory category)
    {
        if (!CanBuy(settings, category)) return false;

        Data.money -= GetPrice(settings, category);   // el precio se calcula antes de subir el nivel
        Data.permanentLevels[(int)category]++;
        return true;
    }

    // Bono total de la categoría, ya reducido por el estrés o la depresión
    public static float GetBonus(UpgradeSettings settings, UpgradeCategory category)
    {
        PermanentUpgradeData data = settings.Get(category);
        float raw = GetLevel(category) * data.effectPerLevel;
        return raw * GetMoodFactor(settings, data.affectedBy);
    }

    public static float GetMoodFactor(UpgradeSettings settings, MoodStat stat)
    {
        switch (stat)
        {
            case MoodStat.Estres: return settings.GetMoodFactor(Data.stress);
            case MoodStat.Depresion: return settings.GetMoodFactor(Data.depression);
            default: return 1f;
        }
    }

    public static string FormatMoney(int cents) => $"${cents / 100f:0.00}";

    // Penalización a la estadística base de una categoría, según el estado que la afecta
    public static float GetBasePenalty(UpgradeSettings settings, UpgradeCategory category)
    {
        PermanentUpgradeData data = settings.Get(category);
        if (data == null) return 0f;

        switch (data.affectedBy)
        {
            case MoodStat.Estres: return settings.GetMoodBasePenalty(Data.stress);
            case MoodStat.Depresion: return settings.GetMoodBasePenalty(Data.depression);
            default: return 0f;
        }
    }
}