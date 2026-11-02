using UnityEngine;

public class DamageZone : MonoBehaviour
{
    [SerializeField] private int damage = 10;
    [SerializeField] private float interval = 0.5f;

    private float nextDamageTime;

    private void OnTriggerStay(Collider other)
    {
        if (Time.time < nextDamageTime) return;

        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
            nextDamageTime = Time.time + interval;
        }
    }
}