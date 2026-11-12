using System;
using UnityEngine;
using Unity.Cinemachine;

public class PlayerHealth : MonoBehaviour, IDamageable, IDeathNotifier
{
    [SerializeField] private int maxHealth = 100;

    [Header("Reacción al daño")]
    [SerializeField] private float hurtDuration = 0.45f;
    [Tooltip("Segundos invulnerable después de recibir un golpe")]
    [SerializeField] private float postHitInvulnerability = 1f;
    [SerializeField] private float hurtKnockback = 4f;

    [Header("Feedback")]
    [SerializeField] private float hurtShake = 0.35f;
    [SerializeField] private float hurtHitStop = 0.05f;

    [Header("Animación")]
    [SerializeField] private Animator animator;
    [SerializeField] private string hurtState = "Hurt";
    [SerializeField] private string deathState = "Death";
    [SerializeField] private string locomotionState = "Locomotion";

    [Header("Reaparecer")]
    [SerializeField] private float respawnInvulnerability = 2f;

    public event Action Revived;

    private DeathDissolve deathDissolve;

    public event Action<int> Damaged;
    public event Action Died;

    public int CurrentHealth { get; private set; }
    public int MaxHealth { get; private set; }
    public bool IsDead { get; private set; }
    public bool IsHurt { get; private set; }
    public float InvulnerableUntil { get; private set; }
    public Vector3 KnockbackVelocity { get; private set; }

    private PlayerDash dash;
    private PlayerCombat combat;
    private float hurtTimer;
    public event Action HealthChanged;
    public event Action<int> Healed;

    private void Awake()
    {
        MaxHealth = maxHealth;
        CurrentHealth = MaxHealth;
        dash = GetComponent<PlayerDash>();
        combat = GetComponent<PlayerCombat>();
        deathDissolve = GetComponent<DeathDissolve>();
    }

    private void Update()
    {
        if (!IsHurt) return;

        hurtTimer -= Time.deltaTime;

        // El empuje se va frenando durante la reacción
        float t = Mathf.Clamp01(hurtTimer / hurtDuration);
        KnockbackVelocity = -transform.forward * hurtKnockback * t;

        if (hurtTimer <= 0f)
        {
            IsHurt = false;
            KnockbackVelocity = Vector3.zero;
            animator.CrossFadeInFixedTime(locomotionState, 0.15f);
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        if (dash != null && dash.IsInvulnerable)
        {
            Debug.Log("[Player] ¡Esquivado!");
            return;
        }

        // Invulnerabilidad después de un golpe
        if (Time.time < InvulnerableUntil) return;

        CurrentHealth -= amount;
        Debug.Log($"[Player] Recibió {amount} de daño. Vida: {CurrentHealth}");
        Damaged?.Invoke(amount);
        HealthChanged?.Invoke();

        if (CurrentHealth <= 0)
        {
            Die();
        }
        else
        {
            Hurt();
        }
    }

    private void Hurt()
    {
        combat.InterruptAttack();
        dash.InterruptDash();

        IsHurt = true;
        hurtTimer = hurtDuration;
        InvulnerableUntil = Time.time + postHitInvulnerability;

        animator.CrossFadeInFixedTime(hurtState, 0.05f);

        if (CombatFeedback.Instance != null)
        {
            CombatFeedback.Instance.Hit(hurtShake, hurtHitStop);
        }
    }

    private void Die()
    {
        IsDead = true;
        IsHurt = false;
        CurrentHealth = 0;
        HealthChanged?.Invoke();
        KnockbackVelocity = Vector3.zero;

        // Detener todo el control del jugador
        combat.InterruptAttack();
        dash.InterruptDash();
        GetComponent<Player>().enabled = false;
        combat.enabled = false;
        dash.enabled = false;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        animator.CrossFadeInFixedTime(deathState, 0.1f);

        if (CombatFeedback.Instance != null)
        {
            CombatFeedback.Instance.Hit(hurtShake * 1.5f, 0.15f);
        }

        Debug.Log("[Player] Murió");
        Died?.Invoke();
    }

    // Sube la vida actual en la misma cantidad que la máxima
    public void SetMaxHealthMultiplier(float multiplier)
    {
        int oldMax = MaxHealth;
        MaxHealth = Mathf.RoundToInt(maxHealth * multiplier);

        if (!IsDead)
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth + (MaxHealth - oldMax), 1, MaxHealth);
        }
        HealthChanged?.Invoke();
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0) return;

        int before = CurrentHealth;
        CurrentHealth = Mathf.Min(CurrentHealth + amount, MaxHealth);

        int healed = CurrentHealth - before;
        if (healed <= 0) return;

        Debug.Log($"[Player] Curado {healed}. Vida: {CurrentHealth}");
        Healed?.Invoke(healed);
        HealthChanged?.Invoke();
    }

    public void Revive(Vector3 position, Quaternion rotation)
    {
        if (!IsDead) return;

        // Deshacer la desintegración (materiales, colliders y física)
        if (deathDissolve != null) deathDissolve.ResetVisuals();

        // Teletransportar al punto de reaparición
        Rigidbody rb = GetComponent<Rigidbody>();
        Vector3 delta = position - transform.position;
        rb.linearVelocity = Vector3.zero;
        rb.position = position;
        rb.rotation = rotation;
        transform.SetPositionAndRotation(position, rotation);

        // Que la cámara salte con ella en vez de deslizarse por el mapa
        CinemachineCore.OnTargetObjectWarped(transform, delta);

        // Restaurar estado
        IsDead = false;
        IsHurt = false;
        KnockbackVelocity = Vector3.zero;
        CurrentHealth = MaxHealth;
        InvulnerableUntil = Time.time + respawnInvulnerability;

        // Devolver el control
        GetComponent<Player>().enabled = true;
        combat.enabled = true;
        dash.enabled = true;

        animator.speed = 1f;
        animator.CrossFadeInFixedTime(locomotionState, 0f);

        HealthChanged?.Invoke();
        Revived?.Invoke();
    }
}