using UnityEngine;

public class HealthOrb : MonoBehaviour
{
    [Header("Curación")]
    [SerializeField] private int minHeal = 10;
    [SerializeField] private int maxHeal = 30;

    [Header("Al aparecer")]
    [SerializeField] private float popSpeed = 5f;
    [SerializeField] private float gravity = 12f;
    [SerializeField] private float hoverHeight = 0.8f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Flotar")]
    [SerializeField] private float bobAmplitude = 0.12f;
    [SerializeField] private float bobSpeed = 3f;

    [Header("Atracción hacia el jugador")]
    [Tooltip("Segundos antes de poder ser atraída (para que se vea salir del enemigo)")]
    [SerializeField] private float attractDelay = 0.6f;
    [SerializeField] private float attractRadius = 4f;
    [SerializeField] private float maxSpeed = 14f;
    [SerializeField] private float acceleration = 35f;
    [SerializeField] private float collectDistance = 0.6f;
    [SerializeField] private float playerHeightOffset = 0.5f;
    [Tooltip("Si el jugador tiene la vida llena, la orbe espera en vez de desperdiciarse")]
    [SerializeField] private bool waitIfFullHealth = true;

    [Header("Vida útil")]
    [SerializeField] private float lifetime = 20f;
    [SerializeField] private float blinkTime = 3f;

    [Header("Visual")]
    [Tooltip("Hijo que contiene el aspecto de la orbe (se escala según la curación)")]
    [SerializeField] private Transform visual;
    [SerializeField] private float minScale = 0.7f;
    [SerializeField] private float maxScale = 1.3f;
    [SerializeField] private GameObject collectVFX;

    private int healAmount;
    private PlayerHealth player;
    private Vector3 velocity;
    private float hoverY;
    private bool landed;
    private bool attracting;
    private float timer;
    private float bobPhase;

    private void Awake()
    {
        player = FindAnyObjectByType<PlayerHealth>();

        // Curación aleatoria; la orbe es más grande cuanto más cura
        healAmount = Random.Range(minHeal, maxHeal + 1);
        float t = Mathf.InverseLerp(minHeal, maxHeal, healAmount);
        if (visual != null) visual.localScale *= Mathf.Lerp(minScale, maxScale, t);

        // Sale disparada hacia arriba en una dirección aleatoria
        Vector2 side = Random.insideUnitCircle * 0.6f;
        velocity = new Vector3(side.x, 1f, side.y).normalized * popSpeed;

        // Calcular a qué altura debe flotar
        Vector3 origin = transform.position + Vector3.up * 0.5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 50f, groundLayer))
        {
            hoverY = hit.point.y + hoverHeight;
        }
        else
        {
            hoverY = transform.position.y - 1f;
        }

        bobPhase = Random.value * 10f;
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= lifetime)
        {
            Destroy(gameObject);
            return;
        }

        UpdateBlink();

        if (attracting || CanAttract())
        {
            attracting = true;
            MoveToPlayer();
        }
        else
        {
            FallAndHover();
        }
    }

    private bool CanAttract()
    {
        if (timer < attractDelay || player == null || player.IsDead) return false;
        if (waitIfFullHealth && player.CurrentHealth >= player.MaxHealth) return false;

        return Vector3.Distance(transform.position, player.transform.position) <= attractRadius;
    }

    private void MoveToPlayer()
    {
        if (player == null || player.IsDead)
        {
            attracting = false;
            return;
        }

        Vector3 target = player.transform.position + Vector3.up * playerHeightOffset;
        Vector3 toTarget = target - transform.position;
        float distance = toTarget.magnitude;

        if (distance <= collectDistance)
        {
            Collect();
            return;
        }

        // Gira poco a poco hacia el jugador: vuela en curva, no en línea recta
        Vector3 desired = toTarget / distance * maxSpeed;
        velocity = Vector3.MoveTowards(velocity, desired, acceleration * Time.deltaTime);
        transform.position += velocity * Time.deltaTime;
    }

    private void FallAndHover()
    {
        if (!landed)
        {
            velocity.y -= gravity * Time.deltaTime;

            // Frenar el desplazamiento horizontal
            float drag = Mathf.Clamp01(2f * Time.deltaTime);
            velocity.x *= 1f - drag;
            velocity.z *= 1f - drag;

            transform.position += velocity * Time.deltaTime;

            if (velocity.y < 0f && transform.position.y <= hoverY)
            {
                landed = true;
                velocity = Vector3.zero;
            }
            return;
        }

        // Flotar con un vaivén suave
        Vector3 position = transform.position;
        position.y = hoverY + Mathf.Sin((Time.time + bobPhase) * bobSpeed) * bobAmplitude;
        transform.position = position;
    }

    private void UpdateBlink()
    {
        if (visual == null) return;

        // Parpadea en los últimos segundos antes de desaparecer
        bool blinking = timer > lifetime - blinkTime && !attracting;
        bool visible = !blinking || Mathf.Repeat(timer * 8f, 1f) > 0.4f;

        if (visual.gameObject.activeSelf != visible) visual.gameObject.SetActive(visible);
    }

    private void Collect()
    {
        player.Heal(healAmount);

        if (collectVFX != null)
        {
            GameObject vfx = Instantiate(collectVFX, player.transform);
            vfx.transform.localPosition = Vector3.zero;
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, attractRadius);
    }
}