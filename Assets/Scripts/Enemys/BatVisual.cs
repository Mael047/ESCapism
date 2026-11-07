using UnityEngine;

public class BatVisual : MonoBehaviour
{
    [SerializeField] private BatAI ai;

    [Header("Aleteo (subir y bajar)")]
    [SerializeField] private float bobAmplitude = 0.15f;
    [SerializeField] private float bobSpeed = 6f;

    [Header("Inclinación al volar")]
    [SerializeField] private float tiltPerSpeed = 3f;
    [SerializeField] private float maxTilt = 25f;
    [SerializeField] private float tiltSmooth = 8f;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float phase;

    private void Awake()
    {
        if (ai == null) ai = GetComponentInParent<BatAI>();
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
        phase = Random.value * 10f;   // para que varios murciélagos no se muevan sincronizados
    }

    private void Update()
    {
        // Sube y baja
        float bob = Mathf.Sin((Time.time + phase) * bobSpeed) * bobAmplitude;
        transform.localPosition = basePosition + Vector3.up * bob;

        // Se inclina hacia adelante al avanzar y de lado al girar
        Vector3 localVelocity = ai.transform.InverseTransformDirection(ai.Velocity);
        float pitch = Mathf.Clamp(localVelocity.z * tiltPerSpeed, -maxTilt, maxTilt);
        float roll = Mathf.Clamp(-localVelocity.x * tiltPerSpeed, -maxTilt, maxTilt);

        Quaternion target = baseRotation * Quaternion.Euler(pitch, 0f, roll);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, target, tiltSmooth * Time.deltaTime);
    }
}