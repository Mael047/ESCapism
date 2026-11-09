using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [SerializeField] private UpgradeSettings settings;

    private readonly float[] temporaryBonus = new float[4];

    private Player player;
    private PlayerHealth health;
    private float baseMoveSpeed;

    public float DamageBonus { get; private set; }
    public float FinisherBonus { get; private set; }
    public float SpeedMultiplier { get; private set; } = 1f;
    public float HealthMultiplier { get; private set; } = 1f;
    public event System.Action StatsChanged;

    private void Awake()
    {
        player = GetComponent<Player>();
        health = GetComponent<PlayerHealth>();
        baseMoveSpeed = player.moveSpeed;
    }

    private void Start()
    {
        Recalculate();
    }

    public void Recalculate()
    {
        DamageBonus = GetBonus(UpgradeCategory.Fuerza);
        FinisherBonus = GetBonus(UpgradeCategory.Remate);
        SpeedMultiplier = Mathf.Max(0.1f, 1f + GetBonus(UpgradeCategory.Agilidad));
        HealthMultiplier = Mathf.Max(0.1f, 1f + GetBonus(UpgradeCategory.Resistencia));

        player.moveSpeed = baseMoveSpeed * SpeedMultiplier;
        health.SetMaxHealthMultiplier(HealthMultiplier);

        Debug.Log($"[PlayerStats] Daño +{DamageBonus:P0} | Remate +{FinisherBonus:P0} | " +
                  $"Velocidad x{SpeedMultiplier:0.00} | Vida x{HealthMultiplier:0.00}");
        StatsChanged?.Invoke();
    }

    // Fuerza y Remate se suman, como en la fórmula del documento
    public float GetDamageMultiplier(bool isFinisher)
    {
        return 1f + DamageBonus + (isFinisher ? FinisherBonus : 0f);
    }

    public void AddTemporaryBonus(UpgradeCategory category, float amount)
    {
        temporaryBonus[(int)category] += amount;
        Recalculate();
    }

    private float GetBonus(UpgradeCategory category)
    {
        float permanent = 0f;
        float penalty = 0f;

        if (settings != null)
        {
            permanent = PermanentUpgrades.GetBonus(settings, category);
            penalty = PermanentUpgrades.GetBasePenalty(settings, category);
        }

        // La penalización base se resta; las temporales no se ven afectadas
        return permanent + temporaryBonus[(int)category] - penalty;
    }
}