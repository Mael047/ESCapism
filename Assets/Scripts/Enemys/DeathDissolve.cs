using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DeathDissolve : MonoBehaviour
{
    [Header("Tiempos")]
    [Tooltip("Segundos de la animación de muerte antes de congelarse")]
    [SerializeField] private float freezeDelay = 1f;
    [Tooltip("Segundos para pasar de su color normal al color de polvo")]
    [SerializeField] private float toDustDuration = 0.4f;
    [Tooltip("Segundos que tarda en desintegrarse")]
    [SerializeField] private float dissolveDuration = 2f;

    [Header("Material")]
    [SerializeField] private Material dissolveMaterial;
    [Tooltip("Empieza con su textura original y luego cambia al color de polvo")]
    [SerializeField] private bool keepOriginalTexture = true;

    [Header("Partículas (opcional)")]
    [FormerlySerializedAs("dustPrefab")]
    [SerializeField] private ParticleSystem particlesPrefab;
    [SerializeField] private float particleMultiplier = 1f;
    [Tooltip("Si se deja vacío, usa la malla más grande del personaje")]
    [SerializeField] private Renderer meshSource;

    [Header("Al terminar")]
    [Tooltip("Enemigos: sí. Jugador: no (solo se oculta)")]
    [SerializeField] private bool destroyOnFinish = true;

    private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
    private static readonly int DustAmountId = Shader.PropertyToID("_DustAmount");
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    private IDeathNotifier deathSource;
    private Animator animator;
    private Renderer[] renderers;
    private readonly List<Material> materialInstances = new List<Material>();

    private void Awake()
    {
        deathSource = GetComponent<IDeathNotifier>();
        if (deathSource == null)
        {
            Debug.LogError($"[DeathDissolve] {name} necesita un componente de vida con IDeathNotifier");
        }

        animator = GetComponentInChildren<Animator>();

        // Solo mallas: partículas y trails también son Renderers
        var meshes = new List<Renderer>();
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
        {
            if (r is SkinnedMeshRenderer || r is MeshRenderer) meshes.Add(r);
        }
        renderers = meshes.ToArray();

        if (meshSource == null) meshSource = FindLargestMesh();
    }

    private void OnEnable()
    {
        if (deathSource != null) deathSource.Died += OnDied;
    }

    private void OnDisable()
    {
        if (deathSource != null) deathSource.Died -= OnDied;
    }

    private void OnDestroy()
    {
        foreach (Material m in materialInstances)
        {
            if (m != null) Destroy(m);
        }
    }

    private void OnDied()
    {
        StartCoroutine(DeathRoutine());
    }

    private IEnumerator DeathRoutine()
    {
        FreezeInPlace();

        // 1. Animación de muerte
        yield return new WaitForSeconds(freezeDelay);

        // 2. Congelar la pose
        if (animator != null) animator.speed = 0f;

        if (dissolveMaterial == null)
        {
            Finish();
            yield break;
        }

        ApplyDissolveMaterial();

        // 3. Cambiar al color de polvo
        if (keepOriginalTexture)
        {
            yield return AnimateProperty(DustAmountId, toDustDuration);
        }

        // 4. Desintegrarse mientras suelta partículas
        if (particlesPrefab != null && meshSource != null)
        {
            SpawnParticles();
        }

        yield return AnimateProperty(DissolveId, dissolveDuration);

        Finish();
    }

    private void FreezeInPlace()
    {
        // Kinematic antes de apagar los colliders: así no cae a través del suelo
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        foreach (Collider c in GetComponentsInChildren<Collider>())
        {
            c.enabled = false;
        }
    }

    private void ApplyDissolveMaterial()
    {
        foreach (Renderer r in renderers)
        {
            Material[] originals = r.sharedMaterials;
            Material[] replaced = new Material[originals.Length];

            for (int i = 0; i < originals.Length; i++)
            {
                Material instance = new Material(dissolveMaterial);

                if (keepOriginalTexture && originals[i] != null)
                {
                    CopyOriginalLook(originals[i], instance);
                }

                instance.SetFloat(DustAmountId, keepOriginalTexture ? 0f : 1f);
                instance.SetFloat(DissolveId, 0f);

                materialInstances.Add(instance);
                replaced[i] = instance;
            }

            r.sharedMaterials = replaced;
        }
    }

    private void CopyOriginalLook(Material original, Material target)
    {
        Texture texture = null;
        if (original.HasProperty(BaseMapId)) texture = original.GetTexture(BaseMapId);
        if (texture == null && original.HasProperty(MainTexId)) texture = original.GetTexture(MainTexId);
        if (texture != null) target.SetTexture(BaseMapId, texture);

        if (original.HasProperty(BaseColorId))
        {
            target.SetColor(BaseColorId, original.GetColor(BaseColorId));
        }
    }

    private IEnumerator AnimateProperty(int propertyId, float duration)
    {
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            float value = Mathf.Clamp01(timer / duration);

            foreach (Material m in materialInstances)
            {
                m.SetFloat(propertyId, value);
            }
            yield return null;
        }
    }

    private void SpawnParticles()
    {
        ParticleSystem particles = Instantiate(particlesPrefab, meshSource.bounds.center, Quaternion.identity);

        foreach (ParticleSystem ps in particles.GetComponentsInChildren<ParticleSystem>())
        {
            ParticleSystem.MainModule main = ps.main;
            main.duration = dissolveDuration;

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

            if (!Mathf.Approximately(particleMultiplier, 1f))
            {
                ParticleSystem.EmissionModule emission = ps.emission;
                emission.rateOverTimeMultiplier *= particleMultiplier;
                for (int i = 0; i < emission.burstCount; i++)
                {
                    ParticleSystem.Burst burst = emission.GetBurst(i);
                    burst.count = burst.count.constant * particleMultiplier;
                    emission.SetBurst(i, burst);
                }
            }
        }

        particles.Play(true);
    }

    private void Finish()
    {
        foreach (Renderer r in renderers)
        {
            if (r != null) r.enabled = false;
        }

        if (destroyOnFinish) Destroy(gameObject, 0.1f);
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