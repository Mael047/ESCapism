using UnityEngine;

// El jefe invoca murciélagos cuando su vida baja de ciertos porcentajes.
// Los invocados cuentan para la oleada (hay que matarlos para terminarla).
[RequireComponent(typeof(EnemyHealth))]
public class BossSummoner : MonoBehaviour
{
    [Tooltip("Porcentajes de vida (0-1) en los que invoca. Ej: 0.66 y 0.33")]
    [SerializeField] private float[] healthThresholds = { 0.66f, 0.33f };
    [SerializeField] private GameObject minionPrefab;
    [SerializeField] private int minionsPerSummon = 2;
    [SerializeField] private float summonRadius = 3f;
    [Tooltip("Opcional: partículas que salen en cada invocado")]
    [SerializeField] private ParticleSystem summonVFX;
    [Tooltip("Opcional: suena una vez en cada invocación")]
    [SerializeField] private SoundData summonSound;

    private EnemyHealth health;
    private WaveManager waveManager;
    private int nextThreshold;

    private void Awake()
    {
        health = GetComponent<EnemyHealth>();
        waveManager = FindAnyObjectByType<WaveManager>();
    }

    private void OnEnable() => health.HealthChanged += OnHealthChanged;
    private void OnDisable() => health.HealthChanged -= OnHealthChanged;

    private void OnHealthChanged(int current, int max)
    {
        if (health.IsDead || minionPrefab == null) return;

        float percent = (float)current / max;

        // Un golpe fuerte puede cruzar dos umbrales a la vez
        while (nextThreshold < healthThresholds.Length && percent <= healthThresholds[nextThreshold])
        {
            nextThreshold++;
            Summon();
        }
    }

    private void Summon()
    {
        SFX.Play(summonSound, transform.position);
        for (int i = 0; i < minionsPerSummon; i++)
        {
            // Repartidos en círculo alrededor del jefe
            float angle = (360f / minionsPerSummon) * i + Random.Range(-20f, 20f);
            Vector3 offset = Quaternion.Euler(0f, angle, 0f) * Vector3.forward * summonRadius;
            Vector3 position = transform.position + offset;

            GameObject minion = Instantiate(minionPrefab, position, Quaternion.LookRotation(offset));

            if (summonVFX != null)
            {
                ParticleSystem vfx = Instantiate(summonVFX, position, Quaternion.identity);
                Destroy(vfx.gameObject, vfx.main.duration + vfx.main.startLifetime.constantMax);
            }

            if (waveManager != null) waveManager.RegisterExternalEnemy(minion);
        }

        Debug.Log($"[Jefe] Invoca {minionsPerSummon} murciélagos");
    }
}
