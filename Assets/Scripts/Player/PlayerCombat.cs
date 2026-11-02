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

    private static readonly int CombatHash = Animator.StringToHash("Combat");

    private InputAction attackAction;
    private int comboIndex = -1;
    private float attackTimer;
    private bool nextQueued;
    private float combatTimer;
    private Player player;
    public Vector3 AttackDirection { get; private set; }


    public bool IsAttacking => comboIndex >= 0;
    public bool InCombat => combatTimer > 0f;
    public Vector3 AttackVelocity { get; private set; }

    private AttackData CurrentAttack => combo[comboIndex];

    private void Awake()
    {
        player = GetComponent<Player>();
        attackAction = InputSystem.actions.FindAction("Attack");
    }

    private void OnEnable() => attackAction.Enable();
    private void OnDisable() => attackAction.Disable();

    private void Update()
    {
        if(attackAction.WasPressedThisFrame())
        {
            if (!IsAttacking) StartAttack(0);
            else nextQueued = true;
        }

        if(IsAttacking)
        {
            UpdateAttack();
        }
        else if(combatTimer > 0f)
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
        comboIndex = index;
        attackTimer = 0f;
        nextQueued = false;
        DeactivateAllHitboxes();
        Vector3 inputDirection = player.GetInputDirection();
        AttackDirection = inputDirection.sqrMagnitude > 0.01f ? inputDirection.normalized : transform.forward;
        animator.CrossFadeInFixedTime(CurrentAttack.stateName, CurrentAttack.transitionDuration);
        EnterCombat();
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
        hitboxes[index].Activate(CurrentAttack.damage);
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
        wave.Initialize(CurrentAttack.shockwaveDamage);

    }

}
