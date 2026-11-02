using UnityEngine;

public class TestDummy : MonoBehaviour, IDamageable
{
    [SerializeField] private int health = 30;

    public void TakeDamage(int amount)
    {
        health -= amount;
        Debug.Log($"{name} recibió {amount} de daño. Vida: {health}");

        if (health <= 0)
        {
            Destroy(gameObject);
        }
    }
}