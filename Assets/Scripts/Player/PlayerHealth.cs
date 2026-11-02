using UnityEngine;

public class PlayerHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private int maxHealth = 100;

    private int currentHealth;
    private PlayerDash dash;

    private void Awake()
    {
        currentHealth = maxHealth;
        dash = GetComponent<PlayerDash>();
    }

    public void TakeDamage(int amount)
    {
        if(dash!= null && dash.IsInvulnerable)
        {
        
            return;
        }
        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage. Current health: {currentHealth}");
    }
}
