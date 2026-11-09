using System;
using UnityEngine;

public class MoodTracker : MonoBehaviour
{
    [SerializeField] private MoodSettings settings;
    [SerializeField] private CustomerManager customerManager;
    [SerializeField] private ShiftManager shiftManager;
    [SerializeField] private bool logChanges = true;

    public event Action Changed;

    private int streak;
    private int angryCustomers;

    private static RunData Data => PermanentUpgrades.Data;

    private void OnEnable()
    {
        if (customerManager != null) customerManager.OnClienteTerminado += OnCustomerFinished;
        if (shiftManager != null) shiftManager.OnJornadaTerminada += OnShiftFinished;
    }

    private void OnDisable()
    {
        if (customerManager != null) customerManager.OnClienteTerminado -= OnCustomerFinished;
        if (shiftManager != null) shiftManager.OnJornadaTerminada -= OnShiftFinished;
    }

    // ---------- Por cliente ----------

    private void OnCustomerFinished(CustomerResult r)
    {
        int stress = 0;
        int depression = 0;
        int difference = r.cambioEntregado - r.cambioEsperado;

        if (r.seFueEnojado)
        {
            // Enojado: solo cuenta como enojado (no se castiga además por cambio o paciencia)
            depression += settings.angryCustomerDepression;
            angryCustomers++;
        }
        else
        {
            if (difference < 0) stress += settings.changeShortStress;
            else if (difference > 0) stress += settings.changeOverStress;

            if (r.paciencia01 < settings.lowPatienceThreshold) stress += settings.lowPatienceStress;
            else if (r.paciencia01 >= settings.highPatienceThreshold) stress -= settings.highPatienceRelief;
        }

        // Racha: misma definición de "perfecto" que ShiftManager
        bool perfect = !r.seFueEnojado && difference == 0;
        if (perfect)
        {
            depression -= settings.perfectCustomerRelief;
            streak++;
        }
        else
        {
            if (streak > 0) depression += settings.streakBrokenDepression;
            streak = 0;
        }

        int level = GetStreakLevel();
        if (level > 0)
        {
            stress -= level * settings.streakReliefPerLevel;
            depression -= level * settings.streakReliefPerLevel;
        }

        Apply(stress, depression, perfect ? "Cliente perfecto" : "Cliente");
    }

    private int GetStreakLevel()
    {
        if (shiftManager == null) return 0;
        int perLevel = Mathf.Max(1, shiftManager.clientesPorNivel);
        return Mathf.Min(streak / perLevel, shiftManager.nivelMaximo);
    }

    // ---------- Al cerrar el turno ----------

    private void OnShiftFinished(ShiftResult result)
    {
        if (angryCustomers == 0)
        {
            int relief = settings.noAngryCustomersRelief;
            Apply(-relief, -relief, "Turno sin clientes enojados");
        }

        // Bono por días buenos seguidos
        bool goodDay = Data.stress < settings.goodDayThreshold &&
                       Data.depression < settings.goodDayThreshold;

        if (goodDay)
        {
            Data.goodDaysStreak++;

            if (Data.goodDaysStreak >= 3)
                Apply(-settings.threeGoodDaysRelief, -settings.threeGoodDaysRelief, "3+ días buenos");
            else if (Data.goodDaysStreak == 2)
                Apply(-settings.twoGoodDaysRelief, -settings.twoGoodDaysRelief, "2 días buenos");
        }
        else
        {
            Data.goodDaysStreak = 0;
        }

        Debug.Log($"[Ánimo] Turno cerrado. Estrés {Data.stress} | Depresión {Data.depression} | " +
                  $"Días buenos seguidos {Data.goodDaysStreak}");
    }

    // ---------- Aplicar ----------

    private void Apply(int stress, int depression, string reason)
    {
        if (stress == 0 && depression == 0) return;

        Data.stress = Mathf.Clamp(Data.stress + stress, 0, 100);
        Data.depression = Mathf.Clamp(Data.depression + depression, 0, 100);

        if (logChanges)
        {
            Debug.Log($"[Ánimo] {reason}: estrés {stress:+0;-0}, depresión {depression:+0;-0} → " +
                      $"Estrés {Data.stress} | Depresión {Data.depression}");
        }

        Changed?.Invoke();
    }
}