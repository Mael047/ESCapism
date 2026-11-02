using UnityEngine;

// Deja huellas de gato que se desvanecen.
// MODO HUESOS (preciso): si asignas los pies, marca cada pisada real, incluso en giros.
// MODO DISTANCIA (respaldo): si dejas los pies vacíos, pone una huella cada cierta distancia.
// Ponlo en el objeto "Player" (el que tiene Rigidbody y Capsule Collider).
public class FootprintTrail : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CapsuleCollider capsule;
    [SerializeField] private Material footprintMaterial; // URP Particles Unlit, Additive
    [SerializeField] private LayerMask groundLayer;

    [Header("Modo huesos (arrastra desde rig)")]
    [SerializeField] private Transform leftFoot;   // foot_ik.L
    [SerializeField] private Transform rightFoot;  // foot_ik.R
    [SerializeField] private Transform leftToe;    // toe_ik.L
    [SerializeField] private Transform rightToe;   // toe_ik.R
    [Tooltip("Altura sobre el suelo por debajo de la cual el pie cuenta como apoyado")]
    [SerializeField] private float contactHeight = 0.05f;
    [Tooltip("Altura que debe superar el pie para considerarse levantado")]
    [SerializeField] private float liftHeight = 0.12f;
    [SerializeField] private float minTimeBetweenPrints = 0.12f;
    [Tooltip("Mueve la huella del talón hacia la punta (metros)")]
    [SerializeField] private float forwardShift = 0.1f;
    [Tooltip("Marca si las huellas salen apuntando hacia atrás")]
    [SerializeField] private bool invertDirection = false;

    [Header("Lectura en vivo (solo mirar, para calibrar)")]
    [SerializeField] private float leftHeightDebug;
    [SerializeField] private float rightHeightDebug;

    [Header("Modo distancia (respaldo)")]
    [SerializeField] private float strideDistance = 0.6f;
    [SerializeField] private float footOffset = 0.12f;
    [SerializeField] private float minSpeed = 0.3f;

    [Header("Huella")]
    [SerializeField] private float size = 0.25f;
    [SerializeField] private float lifetime = 2f;
    [SerializeField] private float fadeTime = 1f;
    [SerializeField] private Color color = new Color(1f, 0.5f, 0.75f, 1f);
    [SerializeField] private float glow = 1.5f;
    [SerializeField] private int poolSize = 24;

    private class Print
    {
        public Transform t;
        public MeshRenderer r;
        public float age = -1f; // -1 = libre
    }

    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private Print[] pool;
    private MaterialPropertyBlock block;
    private float distance;
    private int side = 1;
    private int next;
    private bool leftLifted, rightLifted;
    private float leftLast, rightLast;

    private bool UseBones => leftFoot != null && rightFoot != null;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (capsule == null) capsule = GetComponent<CapsuleCollider>();
        block = new MaterialPropertyBlock();

        Transform holder = new GameObject("Footprints").transform;
        pool = new Print[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(holder);
            quad.transform.localScale = Vector3.one * size;

            MeshRenderer r = quad.GetComponent<MeshRenderer>();
            r.sharedMaterial = footprintMaterial;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;

            quad.SetActive(false);
            pool[i] = new Print { t = quad.transform, r = r };
        }
    }

    private void Update()
    {
        if (UseBones) UpdateBones();
        else UpdateDistance();

        UpdatePrints();
    }

    // ---------- MODO HUESOS ----------
    private void UpdateBones()
    {
        leftHeightDebug = CheckFoot(leftFoot, leftToe, ref leftLifted, ref leftLast);
        rightHeightDebug = CheckFoot(rightFoot, rightToe, ref rightLifted, ref rightLast);
    }

    private float CheckFoot(Transform foot, Transform toe, ref bool lifted, ref float lastTime)
    {
        Vector3 origin = foot.position + Vector3.up * 0.5f;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 3f, groundLayer)) return -1f;

        float h = foot.position.y - hit.point.y;

        if (h > liftHeight)
        {
            lifted = true;
        }
        else if (h < contactHeight && lifted && Time.time - lastTime > minTimeBetweenPrints)
        {
            lifted = false;
            lastTime = Time.time;

            Vector3 dir = toe != null ? toe.position - foot.position : transform.forward;
            if (invertDirection) dir = -dir;
            PlaceAt(hit.point, hit.normal, dir);
        }
        return h;
    }

    // ---------- MODO DISTANCIA ----------
    private void UpdateDistance()
    {
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        float speed = v.magnitude;
        if (speed <= minSpeed) return;

        distance += speed * Time.deltaTime;
        if (distance < strideDistance) return;
        distance = 0f;

        float feetY = capsule.bounds.min.y;
        Vector3 origin = new Vector3(transform.position.x, feetY + 0.5f, transform.position.z)
                         + transform.right * footOffset * side;
        side = -side;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1.5f, groundLayer)) return;
        if (hit.distance > 0.8f) return;

        PlaceAt(hit.point, hit.normal, transform.forward);
    }

    // ---------- COMÚN ----------
    private void PlaceAt(Vector3 point, Vector3 normal, Vector3 direction)
    {
        Vector3 fwd = Vector3.ProjectOnPlane(direction, normal);
        if (fwd.sqrMagnitude < 0.0001f) fwd = Vector3.ProjectOnPlane(transform.forward, normal);
        fwd.Normalize();

        Print p = pool[next];
        next = (next + 1) % pool.Length;

        Vector3 pos = point + normal * 0.01f + (UseBones ? fwd * forwardShift : Vector3.zero);
        p.t.SetPositionAndRotation(pos, Quaternion.LookRotation(-normal, fwd));
        p.age = 0f;
        p.t.gameObject.SetActive(true);
    }

    private void UpdatePrints()
    {
        foreach (Print p in pool)
        {
            if (p.age < 0f) continue;

            p.age += Time.deltaTime;
            if (p.age >= lifetime)
            {
                p.age = -1f;
                p.t.gameObject.SetActive(false);
                continue;
            }

            float alpha = Mathf.Clamp01((lifetime - p.age) / fadeTime);
            block.SetColor(BaseColor, new Color(color.r * glow, color.g * glow, color.b * glow, alpha));
            p.r.SetPropertyBlock(block);
        }
    }
}