using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour, IDamageable, IDeathNotifier
{
    [SerializeField] private int maxHealth = 100;

    public event Action<int> Damaged;
    public event Action Died;
    public event Action<int, int> HealthChanged;   // vida actual, vida máxima


    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }
    public int MaxHealth => maxHealth;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);

        if(CurrentHealth <= 0)
        {
            IsDead = true;
            Died?.Invoke();
        }
        else
        {
            Damaged?.Invoke(amount);
        }
    }

}
