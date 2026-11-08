using System.Collections;
using UnityEngine;

[RequireComponent(typeof(EnemyHealth))]
public class EnemyDeathDissolve : MonoBehaviour
{
    [Header("Tiempos")]
    [Tooltip("Segundos de la animación de muerte antes de empezar a deshacerse")]
    [SerializeField] private float dissolveDelay = 0.8f;
    [Tooltip("Segundos que tarda el cuerpo en deshacerse por completo")]
    [SerializeField] private float dissolveDuration = 1.2f;
    [Tooltip("Escala del cuerpo durante la disolución (eje X: 0 a 1)")]
    [SerializeField] private AnimationCurve shrinkCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    [Header("Polvo")]
    [SerializeField] private ParticleSystem dustPrefab;
    [Tooltip("Multiplica la cantidad de partículas. Útil para enemigos grandes")]
    [SerializeField] private float particleMultiplier = 1f;

    [Header("Malla (opcional)")]
    [Tooltip("Si se deja vacío, usa la malla más grande del enemigo")]
    [SerializeField] private Renderer meshSource;

    private EnemyHealth health;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        if (meshSource == null) meshSource = FindLargestMesh();
    }

    private void OnEnable() => health.Died += OnDied;
    private void OnDisable() => health.Died -= OnDied;

    private void OnDied()
    {
        StartCoroutine(DissolveRoutine());
    }

    private IEnumerator DissolveRoutine()
    {
        // 1. Dejar ver la reacción y la animación de muerte
        yield return new WaitForSeconds(dissolveDelay);

        // Ya no se le puede golpear
        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }

        // 2. El polvo sale mientras el cuerpo se encoge
        if (dustPrefab != null && meshSource != null)
        {
            SpawnDust();
        }

        Vector3 startScale = transform.localScale;
        float timer = 0f;

        while (timer < dissolveDuration)
        {
            timer += Time.deltaTime;
            float t = Mathf.Clamp01(timer / dissolveDuration);
            transform.localScale = startScale * shrinkCurve.Evaluate(t);
            yield return null;
        }

        // 3. El cuerpo desaparece; el polvo sigue flotando por su cuenta
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is SkinnedMeshRenderer || r is MeshRenderer) r.enabled = false;
        }

        Destroy(gameObject, 0.1f);
    }

    private void SpawnDust()
    {
        ParticleSystem dust = Instantiate(dustPrefab, meshSource.bounds.center, Quaternion.identity);

        foreach (ParticleSystem ps in dust.GetComponentsInChildren<ParticleSystem>())
        {
            // Emitir durante toda la disolución
            ParticleSystem.MainModule main = ps.main;
            main.duration = dissolveDuration;

            // Nacer sobre la malla del enemigo
            ParticleSystem.ShapeModule shape = ps.shape;
            if (meshSource is SkinnedMeshRenderer skinned)
            {
                shape.shapeType = ParticleSystemShapeType.SkinnedMeshRenderer;
                shape.skinnedMeshRenderer = skinned;
            }
            else if (meshSource is MeshRenderer meshRenderer)
            {
                shape.shapeType = ParticleSystemShapeType.MeshRenderer;
                shape.meshRenderer = meshRenderer;
            }

            ScaleEmission(ps);
        }

        dust.Play(true);
    }

    private void ScaleEmission(ParticleSystem ps)
    {
        if (Mathf.Approximately(particleMultiplier, 1f)) return;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTimeMultiplier *= particleMultiplier;

        for (int i = 0; i < emission.burstCount; i++)
        {
            ParticleSystem.Burst burst = emission.GetBurst(i);
            burst.count = burst.count.constant * particleMultiplier;
            emission.SetBurst(i, burst);
        }
    }

    private Renderer FindLargestMesh()
    {
        Renderer largest = null;
        float largestSize = 0f;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (!(r is SkinnedMeshRenderer || r is MeshRenderer)) continue;

            float size = r.bounds.size.sqrMagnitude;
            if (size > largestSize)
            {
                largestSize = size;
                largest = r;
            }
        }
        return largest;
    }
}