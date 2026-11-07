using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(EnemyHealth))]
public class BatAI : MonoBehaviour
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
    [SerializeField] private float windupTime = 0.6f;
    [SerializeField] private float lungeSpeed = 10f;
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

    private Vector3 lungeDirection;
    private float lungeDistanceLeft;

    private int poise;
    private float poiseRegenTimer;

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

        PlayerHealth playerHealth = FindFirstObjectByType<PlayerHealth>();
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
                if (player != null && Vector3.Distance(transform.position, player.position) < detectionRadius)
                {
                    EnterOrbit();
                    break;
                }
                desired = VelocityTowards(spawnPoint, flySpeed * 0.5f);
                break;

            case State.Orbit:
                if (Vector3.Distance(transform.position, player.position) > loseRadius)
                {
                    ChangeState(State.Idle);
                    break;
                }

                orbitAngle += orbitDirection * orbitSpeed * dt;
                Vector3 offset = Quaternion.Euler(0f, orbitAngle, 0f) * Vector3.forward * orbitRadius;
                Vector3 orbitPoint = player.position + offset + Vector3.up * orbitHeight;
                desired = VelocityTowards(orbitPoint, flySpeed) + Separation();

                // ¿Puede atacar? Necesita estar cerca, sin cooldown y con un turno libre
                if (cooldownTimer <= 0f && HorizontalDistanceToPlayer() <= orbitRadius + 1f
                    && EnemyAttackTokens.TryTake())
                {
                    hasToken = true;
                    ChangeState(State.Windup);
                    PlayAnim("Windup");
                }
                break;

            case State.Windup:
                {
                    // Retrocede y sube un poco: aviso para el jugador
                    Vector3 away = transform.position - player.position;
                    away.y = 0f;
                    desired = away.normalized * 1.5f + Vector3.up * 0.8f;

                    if (stateTimer >= windupTime)
                    {
                        Vector3 target = player.position + Vector3.up * targetHeightOffset;
                        lungeDirection = (target - transform.position).normalized;
                        lungeDistanceLeft = Mathf.Min(
                            Vector3.Distance(transform.position, target) + lungeOvershoot,
                            maxLungeDistance);
                        ChangeState(State.Lunge);
                        PlayAnim("Attack");
                    }
                    break;
                }

            case State.Lunge:
                desired = lungeDirection * lungeSpeed;
                lungeDistanceLeft -= lungeSpeed * dt;

                // Se detiene al golpear o al terminar la distancia: nunca sigue derecho
                if (TryHitPlayer() || lungeDistanceLeft <= 0f)
                {
                    rb.linearVelocity = lungeDirection * 1f;   // frena en seco
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

        Vector3 away = transform.position - player.position;
        away.y = 0f;
        away.Normalize();

        if (poise <= 0)
        {
            // Derribo: cae al suelo
            rb.useGravity = true;
            rb.linearVelocity = away * knockbackSpeed * 2f + Vector3.up * 2f;
            ChangeState(State.Knockdown);
            PlayAnim("Knockdown");
            return;
        }

        // Aturdido en el aire: empuje pequeño para que siga a tu alcance
        rb.linearVelocity = away * knockbackSpeed;
        ChangeState(State.HitStun);   // cada golpe reinicia el aturdimiento
        PlayAnim("Hit");
    }

    private void OnDied()
    {
        ReleaseToken();
        ChangeState(State.Dead);
        PlayAnim("Die");

        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        gameObject.layer = LayerMask.NameToLayer("Default");

        Destroy(gameObject, 2f);
    }

    // ---------- Utilidades ----------

    private void ChangeState(State newState)
    {
        state = newState;
        stateTimer = 0f;
    }

    private void PlayAnim(string stateName)
    {
        if (animator == null) return;

        int hash = Animator.StringToHash(stateName);
        if (animator.HasState(0, hash))
        {
            animator.CrossFadeInFixedTime(hash, 0.1f);
        }
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