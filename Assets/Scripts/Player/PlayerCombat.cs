using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static Unity.Burst.Intrinsics.X86;

public class PlayerCombat : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;
    ///
    [Header("Combo")]
    [SerializeField] private AttackData[] combo;
    [SerializeField] private string locomotionState = "Locomotion";
    [SerializeField] private float returnTransition = 0.15f;
    //
    [Header("Estado de Combate")]
    [SerializeField] private float combatDuration = 5f; // Segundos para pasar de IdleCombat a Idle
    [SerializeField] private float combatBlendTime = 0.3f;
    //
    [Header("Referencia al Hitbox del arma")]
    [SerializeField] private WeaponHitbox[] hitboxes;
    //
    [Header("VFX")]
    [SerializeField] private TrailRenderer weaponTrail;
    //
    [Header("Onda Expansiva")]
    [SerializeField] private Shockwave shockwavePrefab;
    [SerializeField] private Transform slamPoint;
    [SerializeField] private LayerMask groundLayer;
    //
    [Header("Ataque de dash")]
    [SerializeField] private AttackData dashAttack;
    [SerializeField] private int dashAttackComboIndex = 1;

    [Header("Frenar al chocar con enemigos")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float stopDistance = 0.6f;   // distancia desde el jugador al borde del enemigo
    [SerializeField] private float checkRadius = 0.4f;
    [SerializeField] private float checkHeight = 0.8f;    // altura del pecho
    [SerializeField] private float aimRange = 3f;
    [SerializeField] private float aimAngle = 60f;   // cono a cada lado

    private AttackData currentAttack;
    private static readonly int AttackSpeedHash = Animator.StringToHash("AttackSpeed");

    private static readonly int CombatHash = Animator.StringToHash("Combat");

    private InputAction attackAction;
    private int comboIndex = -1;
    private float attackTimer;
    private bool nextQueued;
    private float combatTimer;
    private Player player;
    private PlayerDash dash;
    public Vector3 AttackDirection { get; private set; }
    private PlayerHealth health;
    private PlayerStats stats;



    public bool IsAttacking => comboIndex >= 0;
    private bool swingSoundPlayed;
    public bool InCombat => combatTimer > 0f;
    public Vector3 AttackVelocity { get; private set; }

    private AttackData CurrentAttack => currentAttack;

    private void Awake()
    {
        player = GetComponent<Player>();
        attackAction = InputSystem.actions.FindAction("Attack");
        dash = GetComponent<PlayerDash>();
        health = GetComponent<PlayerHealth>();
        stats = GetComponent<PlayerStats>();
    }

    private void OnEnable() => attackAction.Enable();
    private void OnDisable() => attackAction.Disable();

    private void Update()
    {
        bool attackPressed = attackAction.WasPressedThisFrame() && !health.IsHurt;

        if (dash.IsDashing)
        {
            if (attackPressed) dash.QueueAttack();
        }
        else
        {
            if (attackPressed)
            {
                if (!IsAttacking) StartAttack(0);
                else nextQueued = true;
            }

            if(IsAttacking) UpdateAttack();
        }
        if(!IsAttacking && combatTimer > 0f)
        {
            combatTimer -= Time.deltaTime;
        }

        //1 en combate, 0 fuera de combate
        float combatTarget = InCombat ? 1f : 0f;
        animator.SetFloat(CombatHash, combatTarget, combatBlendTime, Time.deltaTime);
    }

    private void UpdateAttack()
    {
        attackTimer += Time.deltaTime;
        float t = attackTimer / CurrentAttack.Duration;

        float speed = CurrentAttack.moveCurve.Evaluate(t) * CurrentAttack.moveSpeed;
        if (speed > 0f && EnemyAhead(AttackDirection)) speed = 0f;
        AttackVelocity = AttackDirection * speed;

        bool hasNext = comboIndex < combo.Length - 1;
        if(nextQueued && hasNext && t >= CurrentAttack.cancelPoint)
        {
            StartAttack(comboIndex + 1);
            return;
        }

        if(t >= CurrentAttack.exitPoint)
        {
            EndCombo();
        }
    }

    private void StartAttack(int index)
    {
        StartAttack(combo[index], index);
    }
    private void StartAttack(AttackData data, int index)
    {
        comboIndex = index;
        currentAttack = data;
        swingSoundPlayed = false;
        attackTimer = 0f;
        nextQueued = false;
        DeactivateAllHitboxes();

        Vector3 inputDirection = player.GetInputDirection();
        AttackDirection = inputDirection.sqrMagnitude > 0.01f ? inputDirection.normalized : transform.forward;
        AttackDirection = AimAssist(AttackDirection);
        animator.SetFloat(AttackSpeedHash, data.animationSpeed);
        animator.CrossFadeInFixedTime(CurrentAttack.stateName, CurrentAttack.transitionDuration);
        EnterCombat();
    }
    public void StartDashAttack()
    {
        if(dashAttack == null) return;
        StartAttack(dashAttack, dashAttackComboIndex);
    }

    private void EndCombo()
    {
        ResetCombo();
        animator.CrossFadeInFixedTime(locomotionState, returnTransition);
    }

    //Reinicia el contador de combate jeje
    public void InterruptAttack()
    {
        ResetCombo();
        // Aquí irá la animación de recibir golpe
    }

    private void ResetCombo()
    {
        comboIndex = -1;
        attackTimer = 0f;
        nextQueued = false;
        AttackVelocity = Vector3.zero;
        if(weaponTrail != null) weaponTrail.emitting = false;
        DeactivateAllHitboxes();
    }

    public void EnterCombat()
    {
        combatTimer = combatDuration;
    }

    public void HitboxOn(int index)
    {
        if (!IsAttacking) return;
        hitboxes[index].Activate(CurrentAttack);

        // Un solo sonido de swing por golpe, aunque haya varios hitboxes
        if (!swingSoundPlayed)
        {
            swingSoundPlayed = true;
            SFX.Play(CurrentAttack.swingSound, transform.position);
        }
    }

    public void HitboxOff(int index)
    {
        hitboxes[index].Deactivate();
    }

    private void DeactivateAllHitboxes()
    {
        foreach (WeaponHitbox hitbox in hitboxes)
        {
            hitbox.Deactivate();
        }
    }

    public void TrailOn()
    {
        if (!IsAttacking) return;
        weaponTrail.emitting = true;
    }

    public void TrailOff()
    {
        if (!IsAttacking) return;
        weaponTrail.emitting = false;
    }

    public void SpawnShockwave()
    {

        if (!IsAttacking || shockwavePrefab == null) return;

        Vector3 origin = slamPoint.position + Vector3.up;
        Vector3 spawnPoint = slamPoint.position;

        if(Physics.Raycast(origin, Vector3.down, out RaycastHit groundHit, 3f, groundLayer))
        {
            spawnPoint = groundHit.point;
        }
        Shockwave wave = Instantiate(shockwavePrefab, spawnPoint, Quaternion.identity);
        SFX.Play(CurrentAttack.shockwaveSound, spawnPoint);
        float multiplier = stats != null ? stats.GetDamageMultiplier(CurrentAttack.isFinisher) : 1f;
        wave.Initialize(Mathf.RoundToInt(CurrentAttack.shockwaveDamage * multiplier));
        if (CombatFeedback.Instance != null)
        {
            CombatFeedback.Instance.Shake(CurrentAttack.shockwaveShake);
        }

    }

    private bool EnemyAhead(Vector3 direction)
    {
        Vector3 origin = transform.position + Vector3.up * checkHeight;
        return Physics.SphereCast(origin, checkRadius, direction, out _, stopDistance,
                                  enemyLayer, QueryTriggerInteraction.Ignore);
    }

    private Vector3 AimAssist(Vector3 direction)
    {
        Collider best = null;
        float bestScore = float.MaxValue;

        foreach (Collider enemy in Physics.OverlapSphere(transform.position, aimRange, enemyLayer, QueryTriggerInteraction.Ignore))
        {
            Vector3 toEnemy = enemy.transform.position - transform.position;
            toEnemy.y = 0f;
            float angle = Vector3.Angle(direction, toEnemy);
            if (angle > aimAngle) continue;

            // Prioriza los que están más centrados y más cerca
            float score = angle + toEnemy.magnitude * 10f;
            if (score < bestScore) { bestScore = score; best = enemy; }
        }

        if (best == null) return direction;
        Vector3 result = best.transform.position - transform.position;
        result.y = 0f;
        return result.normalized;
    }
}
