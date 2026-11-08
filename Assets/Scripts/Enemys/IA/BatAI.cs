using System.Diagnostics;
using UnityEngine;


[RequireComponent(typeof(Rigidbody), typeof(EnemyHealth))]
public class BatAI : MonoBehaviour, IAggroable
{
    private enum State { Idle, Orbit, Windup, Lunge, Recover, HitStun, Knockdown, Dead }

    [Header("Detección")]
    [SerializeField] private float detectionRadius = 14f;
    [SerializeField] private float loseRadius = 20f;

    [Header("Órbita (dentro del alcance del martillo)")]
    [SerializeField] private float orbitRadius = 2.5f;
    [SerializeField] private float orbitHeight = 0.6f;     // sobre el pivote del jugador
    [SerializeField] private float orbitSpeed = 40f;       // grados por segundo
    [SerializeField] private float flySpeed = 5f;
    [SerializeField] private float acceleration = 15f;
    [SerializeField] private float turnSpeed = 12f;

    [Header("Separación entre enemigos")]
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private float separationRadius = 1.5f;
    [SerializeField] private float separationStrength = 3f;

    [Header("Ataque")]
    [Tooltip("Segundos entre el inicio de la bajada y el golpe en la animación")]
    [SerializeField] private float lungeDuration = 0.25f;
    [Tooltip("Por seguridad: si el evento no llega, ataca igual tras este tiempo")]
    [SerializeField] private float maxWindupTime = 1.5f;
    [SerializeField] private float maxLungeDistance = 4f;
    [SerializeField] private float lungeOvershoot = 0.5f;
    [SerializeField] private float targetHeightOffset = 0.3f; // a qué altura del jugador apunta
    [SerializeField] private float recoverTime = 0.8f;
    [SerializeField] private float attackCooldown = 2.5f;
    [SerializeField] private int damage = 10;
    [SerializeField] private float hitRadius = 0.7f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Recibir daño")]
    [SerializeField] private float knockbackSpeed = 2f;
    [SerializeField] private float hitStunDuration = 0.5f;

    [Header("Poise (derribo)")]
    [Tooltip("Daño seguido que aguanta antes de caer al suelo")]
    [SerializeField] private int maxPoise = 25;
    [Tooltip("Segundos sin recibir golpes para recuperar el poise")]
    [SerializeField] private float poiseRegenDelay = 2f;
    [SerializeField] private float knockdownDuration = 2f;

    [Header("Animación (opcional)")]
    [SerializeField] private Animator animator;

    [Header("VFX")]
    [SerializeField] private ParticleSystem chargeVFX;
    [SerializeField] private ParticleSystem attackVFX;

    private Rigidbody rb;
    private EnemyHealth health;
    private Transform player;
    private Vector3 spawnPoint;
    private readonly Collider[] neighbors = new Collider[8];

    private State state = State.Idle;
    private float stateTimer;
    private float cooldownTimer;
    private float orbitAngle;
    private float orbitDirection = 1f;
    private bool hasToken;

    private bool aggroLocked;
    private Vector3 lungeDirection;
    private float lungeDistanceLeft;

    private int poise;
    private float poiseRegenTimer;
    private float currentLungeSpeed;
    private bool lungeRequested;

    public Vector3 Velocity => rb.linearVelocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        health = GetComponent<EnemyHealth>();
        spawnPoint = transform.position;
        poise = maxPoise;

        // Cada murciélago orbita en una dirección y con un cooldown inicial distinto
        orbitDirection = Random.value < 0.5f ? 1f : -1f;
        cooldownTimer = Random.Range(0.5f, attackCooldown);

        PlayerHealth playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerHealth != null) player = playerHealth.transform;
    }

    private void OnEnable()
    {
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    private void OnDisable()
    {
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
        ReleaseToken();
    }

    private void FixedUpdate()
    {
        if (state == State.Dead) return; 
        if (player == null && state != State.Idle) ChangeState(State.Idle);

        float dt = Time.fixedDeltaTime;
        stateTimer += dt;
        if (cooldownTimer > 0f) cooldownTimer -= dt;
        UpdatePoise(dt);

        Vector3 desired = Vector3.zero;
        bool controlVelocity = true;

        switch (state)
        {
            case State.Idle:
                if (player != null && (aggroLocked || Vector3.Distance(transform.position, player.position) < detectionRadius))
                {
                    EnterOrbit();
                    break;
                }
                desired = VelocityTowards(spawnPoint, flySpeed * 0.5f);
                break;

            case State.Orbit:
                if (!aggroLocked && Vector3.Distance(transform.position, player.position) > loseRadius)
                {
                    ChangeState(State.Idle);
                    break;
                }

                orbitAngle += orbitDirection * orbitSpeed * dt;
                Vector3 offset = Quaternion.Euler(0f, orbitAngle, 0f) * Vector3.forward * orbitRadius;
                Vector3 orbitPoint = player.position + offset + Vector3.up * orbitHeight;
                desired = VelocityTowards(orbitPoint, flySpeed) + Separation();

                // ¿Puede atacar? Necesita estar cerca, sin cooldown y con un turno libre
                if (cooldownTimer <= 0f && HorizontalDistanceToPlayer() <= orbitRadius + 1f && EnemyAttackTokens.TryTake())
                {
                    hasToken = true;
                    lungeRequested = false;
                    ChangeState(State.Windup);
                    PlayAnim("Attack");
                    if (chargeVFX != null) chargeVFX.Play(true);
                }
                break;

            case State.Windup:
                {
                    // Solo retrocede: la subida la hace la animación
                    Vector3 away = transform.position - player.position;
                    away.y = 0f;
                    desired = away.normalized * 1f;

                    if (lungeRequested || stateTimer >= maxWindupTime)
                    {
                        StartLunge();
                    }
                    break;
                }

            case State.Lunge:
                desired = lungeDirection * currentLungeSpeed;
                lungeDistanceLeft -= currentLungeSpeed * dt;

                // Se detiene al golpear o al terminar la distancia: nunca sigue derecho
                if (TryHitPlayer() || lungeDistanceLeft <= 0f)
                {
                    rb.linearVelocity = lungeDirection * 1f;   // frena en seco
                    StopVFX(attackVFX);
                    ChangeState(State.Recover);
                    controlVelocity = false;
                }
                break;

            case State.Recover:
                // Se queda expuesto: ventana para castigarlo
                if (stateTimer >= recoverTime)
                {
                    FinishAttack();
                    EnterOrbit();
                }
                break;

            case State.HitStun:
                if (stateTimer >= hitStunDuration)
                {
                    EnterOrbit();
                }
                break;

            case State.Knockdown:
                controlVelocity = false;   // la gravedad lo lleva al suelo

                if (stateTimer >= knockdownDuration)
                {
                    rb.useGravity = false;
                    poise = maxPoise;
                    ReturnToIdle();
                    EnterOrbit();          // vuelve a subir volando
                }
                break;
        }

        if (controlVelocity)
        {
            rb.linearVelocity = state == State.Lunge
                ? desired
                : Vector3.MoveTowards(rb.linearVelocity, desired, acceleration * dt);
        }

        UpdateRotation();
    }

    // ---------- Movimiento ----------

    private void EnterOrbit()
    {
        // Empieza a orbitar desde el ángulo en el que ya está, sin saltos
        Vector3 relative = transform.position - player.position;
        orbitAngle = Mathf.Atan2(relative.x, relative.z) * Mathf.Rad2Deg;
        ChangeState(State.Orbit);
    }

    private Vector3 VelocityTowards(Vector3 point, float speed)
    {
        Vector3 toPoint = point - transform.position;
        float distance = toPoint.magnitude;
        if (distance < 0.1f) return Vector3.zero;
        return toPoint / distance * Mathf.Min(speed, distance * 2f);
    }

    private Vector3 Separation()
    {
        Vector3 push = Vector3.zero;
        int count = Physics.OverlapSphereNonAlloc(transform.position, separationRadius, neighbors, enemyLayer);

        for (int i = 0; i < count; i++)
        {
            if (neighbors[i].attachedRigidbody == rb) continue;

            Vector3 away = transform.position - neighbors[i].transform.position;
            float distance = away.magnitude;
            if (distance > 0.01f)
            {
                push += away / distance * (1f - distance / separationRadius);
            }
        }
        return push * separationStrength;
    }

    private float HorizontalDistanceToPlayer()
    {
        Vector3 delta = player.position - transform.position;
        delta.y = 0f;
        return delta.magnitude;
    }

    private void UpdateRotation()
    {
        Vector3 look;
        if (state == State.Lunge) look = lungeDirection;
        else if (state == State.Idle || player == null) look = rb.linearVelocity;
        else look = player.position - transform.position;

        look.y = 0f;
        if (look.sqrMagnitude < 0.01f) return;

        Quaternion target = Quaternion.LookRotation(look);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, target, turnSpeed * Time.fixedDeltaTime));
    }

    // ---------- Ataque ----------

    private bool TryHitPlayer()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, hitRadius, playerLayer);
        foreach (Collider hit in hits)
        {
            IDamageable target = hit.GetComponentInParent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);
                return true;
            }
        }
        return false;
    }

    private void FinishAttack()
    {
        ReleaseToken();
        cooldownTimer = attackCooldown * Random.Range(0.8f, 1.3f);
    }

    private void ReleaseToken()
    {
        if (!hasToken) return;
        hasToken = false;
        EnemyAttackTokens.Release();
    }

    // ---------- Recibir daño ----------

    private void UpdatePoise(float dt)
    {
        if (poise >= maxPoise) return;

        poiseRegenTimer -= dt;
        if (poiseRegenTimer <= 0f) poise = maxPoise;
    }

    private void OnDamaged(int amount)
    {
        if (state == State.Dead) return;

        poise -= amount;
        poiseRegenTimer = poiseRegenDelay;

        // En el suelo sigue recibiendo daño, pero no se levanta
        if (state == State.Knockdown) return;

        // Cualquier golpe interrumpe su ataque
        if (hasToken) FinishAttack();
        StopVFX(chargeVFX);
        StopVFX(attackVFX);

        Vector3 away = transform.position - player.position;
        away.y = 0f;
        away.Normalize();

        if (poise <= 0)
        {
            // Derribo: cae al suelo
            rb.useGravity = true;
            rb.linearVelocity = away * knockbackSpeed * 2f + Vector3.up * 2f;
            ChangeState(State.Knockdown);
            if (!PlayAnim("Knockdown")) ReturnToIdle();
            return;
        }

        // Aturdido en el aire: empuje pequeño para que siga a tu alcance
        rb.linearVelocity = away * knockbackSpeed;
        ChangeState(State.HitStun);   // cada golpe reinicia el aturdimiento
        if (!PlayAnim("Hit")) ReturnToIdle();
    }

    private void OnDied()
    {
        ReleaseToken();
        StopVFX(chargeVFX);
        StopVFX(attackVFX);
        ChangeState(State.Dead);
        PlayAnim("Die");

        // Se queda quieto donde murió
        rb.useGravity = false;
        if (!rb.isKinematic) rb.linearVelocity = Vector3.zero;
        gameObject.layer = LayerMask.NameToLayer("Default");

        BatVisual visual = GetComponentInChildren<BatVisual>();
        if (visual != null) visual.enabled = false;
    }

    // ---------- Utilidades ----------

    private void ChangeState(State newState)
    {
        state = newState;
        stateTimer = 0f;
    }

    private bool PlayAnim(string stateName)
    {
        if (animator == null) return false;

        int hash = Animator.StringToHash(stateName);
        if (!animator.HasState(0, hash)) return false;

        animator.CrossFadeInFixedTime(hash, 0.1f);
        return true;
    }

    public void OnLungeEvent()
    {
        if (state == State.Windup) lungeRequested = true;
    }

    private void StartLunge()
    {
        Vector3 target = player.position + Vector3.up * targetHeightOffset;
        lungeDirection = (target - transform.position).normalized;

        float maxDistance = Mathf.Max(maxLungeDistance, orbitRadius + 2.5f);
        lungeDistanceLeft = Mathf.Min(
            Vector3.Distance(transform.position, target) + lungeOvershoot,
            maxDistance);

        // Velocidad calculada para llegar justo cuando la animación golpea
        currentLungeSpeed = lungeDistanceLeft / lungeDuration;

        ChangeState(State.Lunge);

        StopVFX(chargeVFX);
        if (attackVFX != null) attackVFX.Play(true);
    }

    private void ReturnToIdle()
    {
        if (animator == null) return;
        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Idle")) return;
        animator.CrossFadeInFixedTime("Idle", 0.1f);
    }

    public void ForceAggro()
    {
        aggroLocked = true;
    }

    private void StopVFX(ParticleSystem vfx)
    {
        // Deja de emitir, pero las partículas vivas terminan su recorrido
        if (vfx != null) vfx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, hitRadius);

        if (player != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(player.position + Vector3.up * orbitHeight, orbitRadius);
        }
    }
}