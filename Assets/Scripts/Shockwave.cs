using UnityEngine;
using System.Collections.Generic;

public class Shockwave : MonoBehaviour
{
    [Header("Expansión")]
    [SerializeField] private float maxRadius = 2.5f;
    [SerializeField] private float expandDuration = 0.25f;
    [SerializeField] private float lifetime = 2f;

    [Header("Daño")]
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private GameObject hitVFX;

    private int damage;
    private float timer;
    private readonly HashSet<IDamageable> alreadyHit = new HashSet<IDamageable>();

    public void Initialize(int damageAmount)
    {
        damage = damageAmount;
    }

    private void Start()
    {
        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        if (timer >= expandDuration) return;

        timer += Time.deltaTime;
        float t = Mathf.Clamp01(timer / expandDuration);

        float radius = maxRadius * (1f - (1f - t) * (1f - t));

        Collider[] hits = Physics.OverlapSphere(transform.position, radius, targetLayers, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target != null && alreadyHit.Add(target))
            {
                target.TakeDamage(damage);
                SpawnHitVFX(hit);
            }
        }
    }

    private void SpawnHitVFX(Collider target)
    {
        if(hitVFX == null) return;

        Vector3 hitPoint = target.ClosestPoint(transform.position);

        Vector3 direction = hitPoint - transform.position;
        direction.y = 0f;
        if(direction.sqrMagnitude < 0.001f) direction = transform.forward;
        
        Instantiate(hitVFX, hitPoint, Quaternion.LookRotation(direction.normalized));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, maxRadius);
    }

}
