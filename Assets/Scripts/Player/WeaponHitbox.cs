using UnityEngine;
using System.Collections.Generic;

public class WeaponHitbox : MonoBehaviour
{
    [Header("Forma")]
    [SerializeField] private Vector3 size = new Vector3(1f, 1f, 1f);

    [Header("Detección")]
    [SerializeField] private LayerMask targetLayers;
    [Tooltip("Revisiones por frame. Más revisiones = mayor precisión, pero más consumo de recursos")]
    [SerializeField] private int substeps = 4;

    [Header("VFX")]
    [SerializeField] private GameObject hitVFX;

    private bool isActive;
    private int damage;
    private Vector3 previousPosition;
    private Quaternion previousRotation;
    private AttackData currentAttack;
    private PlayerStats stats;

    private readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();

    private void Awake()
    {
        stats = GetComponentInParent<PlayerStats>();
    }

    public void Activate(AttackData attack)
    {
        isActive = true;
        currentAttack = attack;
        float multiplier = stats != null ? stats.GetDamageMultiplier(attack.isFinisher) : 1f;
        damage = Mathf.RoundToInt(attack.damage * multiplier);
        hitThisSwing.Clear();
        previousPosition = transform.position;
        previousRotation = transform.rotation;
    }

    public void Deactivate()
    {
        isActive = false;
    }   

    private void LateUpdate()
    {
        if (!isActive) return;

        Vector3 halfExtents = Vector3.Scale(size * 0.5f, transform.lossyScale);

        for(int i = 0; i < substeps; i++)
        {
            float t = (float)i / substeps;
            Vector3 position = Vector3.Lerp(previousPosition, transform.position, t);
            Quaternion rotation = Quaternion.Slerp(previousRotation, transform.rotation, t);

            Collider[] hits = Physics.OverlapBox(position, halfExtents, rotation, targetLayers, QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                IDamageable target = hit.GetComponentInParent<IDamageable>();
                if (target != null && hitThisSwing.Add(target))
                {
                    target.TakeDamage(damage);
                    SpawnHitVFX(hit, position);
                    if (currentAttack != null) SFX.Play(currentAttack.hitSound, hit.ClosestPoint(position));

                    if (CombatFeedback.Instance != null && currentAttack != null)
                    {
                        CombatFeedback.Instance.Hit(currentAttack.hitShake, currentAttack.hitStop);
                    }
                }
            }
        }

        previousPosition = transform.position;
        previousRotation = transform.rotation;

    }

    private void SpawnHitVFX(Collider target, Vector3 hitboxPosition)
    {
        if(hitVFX == null) return;

        Vector3 hitPoint = target.ClosestPoint(hitboxPosition);

        Vector3 swingDirection = transform.position - previousPosition;
        if (swingDirection.sqrMagnitude < 0.001f)
        {
            swingDirection = transform.forward;
        }
        Instantiate(hitVFX, hitPoint, Quaternion.LookRotation(swingDirection.normalized));
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = isActive ? Color.red : Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, size);
    }

}
