using UnityEngine;
using System;

public class EnemyHealth : MonoBehaviour, IDamageable, IDeathNotifier
{
    [SerializeField] private int maxHealth = 100;

    public event Action<int> Damaged;
    public event Action Died;


    public int CurrentHealth { get; private set; }
    public bool IsDead { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public void TakeDamage(int amount)
    {
        if (IsDead) return;

        CurrentHealth -= amount;

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
