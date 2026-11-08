using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CombatFeedback : MonoBehaviour
{
    public static CombatFeedback Instance { get; private set; }

    [Header("Temblor de cámara")]
    [SerializeField] private CinemachineImpulseSource impulseSource;

    [Header("Hit stop")]
    [Tooltip("Velocidad del juego durante el congelamiento (0 = totalmente congelado)")]
    [SerializeField] private float hitStopTimeScale = 0.05f;
    [SerializeField] private float maxHitStop = 0.2f;

    private float hitStopEndTime;
    private bool inHitStop;
    private int lastShakeFrame = -1;
    private float lastShakeForce;

    private void Awake()
    {
        Instance = this;
        if (impulseSource == null) impulseSource = GetComponent<CinemachineImpulseSource>();

        // La cámara sigue temblando aunque el juego esté congelado
        CinemachineImpulseManager.Instance.IgnoreTimeScale = true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        Time.timeScale = 1f;
    }

    public void Hit(float shakeForce, float hitStop)
    {
        Shake(shakeForce);
        HitStop(hitStop);
    }

    public void Shake(float force)
    {
        if (force <= 0f || impulseSource == null) return;

        // Si un barrido golpea a varios en el mismo frame, solo tiembla una vez
        if (Time.frameCount == lastShakeFrame && force <= lastShakeForce) return;
        lastShakeFrame = Time.frameCount;
        lastShakeForce = force;

        Vector3 direction = (Vector3.down + Random.insideUnitSphere * 0.5f).normalized;
        impulseSource.GenerateImpulseWithVelocity(direction * force);
    }

    public void HitStop(float duration)
    {
        if (duration <= 0f) return;
        float end = Time.unscaledTime + Mathf.Min(duration, maxHitStop);
        if (end > hitStopEndTime) hitStopEndTime = end;

        if (!inHitStop) StartCoroutine(HitStopRoutine());
    }

    private IEnumerator HitStopRoutine()
    {
        inHitStop = true;
        Time.timeScale = hitStopTimeScale;

        while (Time.unscaledTime < hitStopEndTime)
        {
            yield return null;
        }

        Time.timeScale = 1f;
        inHitStop = false;
    }
}