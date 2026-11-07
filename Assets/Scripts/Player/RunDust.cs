using UnityEngine;

// Enciende el polvo cuando el personaje va rápido y está en el suelo.
// Ponlo en el objeto "Player" y arrastra el Particle System "RunDust".
public class RunDust : MonoBehaviour
{
    [SerializeField] private Rigidbody rb;
    [SerializeField] private ParticleSystem dust;

    [Tooltip("Velocidad (m/s) a partir de la cual sale polvo")]
    [SerializeField] private float runSpeed = 3.5f;
    [Tooltip("Partículas por segundo mientras corre")]
    [SerializeField] private float rate = 14f;
    [Tooltip("Si la velocidad vertical supera esto, se considera en el aire")]
    [SerializeField] private float airVerticalSpeed = 0.5f;

    [Header("Lectura en vivo (solo mirar, para calibrar)")]
    [SerializeField] private float currentSpeedDebug;

    private void Awake()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Vector3 v = rb.linearVelocity;
        float horizontal = new Vector2(v.x, v.z).magnitude;
        currentSpeedDebug = horizontal;

        bool grounded = Mathf.Abs(v.y) < airVerticalSpeed;
        var emission = dust.emission;
        emission.rateOverTime = (horizontal > runSpeed && grounded) ? rate : 0f;
    }
}
