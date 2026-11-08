using System;
using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;

    public event Action Died;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private PlayerDash dash;
    private PlayerCombat combat;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        dash = GetComponent<PlayerDash>();
        combat = GetComponent<PlayerCombat>();
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        if (dash != null && dash.IsInvulnerable)
        {
            Debug.Log("[Player] ¡Esquivado!");
            return;
        }

        CurrentHealth -= amount;
        Debug.Log($"[Player] Recibió {amount} de daño. Vida: {CurrentHealth}");

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        IsDead = true;
        CurrentHealth = 0;

        // Detener todo el control del jugador
        combat.InterruptAttack();
        GetComponent<Player>().enabled = false;
        combat.enabled = false;
        dash.enabled = false;
        GetComponent<Rigidbody>().linearVelocity = Vector3.zero;

        Debug.Log("[Player] Murió");
        Died?.Invoke();
    }
}