using UnityEngine;

public class PlayerHealth : MonoBehaviour
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
            // El jugador es invulnerable durante el dash, no recibe daño
            return;
        }
        currentHealth -= amount;
        Debug.Log($"Player took {amount} damage. Current health: {currentHealth}");
    }
}
