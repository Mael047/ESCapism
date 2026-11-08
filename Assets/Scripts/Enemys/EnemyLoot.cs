using UnityEngine;

public class EnemyLoot : MonoBehaviour
{
    [SerializeField] private HealthOrb orbPrefab;
    [Range(0f, 1f)][SerializeField] private float dropChance = 1f;
    [SerializeField] private int minOrbs = 1;
    [SerializeField] private int maxOrbs = 1;
    [Tooltip("Desde dónde salen las orbes, relativo al pivote del enemigo")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    private IDeathNotifier deathSource;

    private void Awake()
    {
        deathSource = GetComponent<IDeathNotifier>();
    }

    private void OnEnable()
    {
        if (deathSource != null) deathSource.Died += OnDied;
    }

    private void OnDisable()
    {
        if (deathSource != null) deathSource.Died -= OnDied;
    }

    private void OnDied()
    {
        if (orbPrefab == null || Random.value > dropChance) return;

        Vector3 origin = transform.position + spawnOffset;
        int count = Random.Range(minOrbs, maxOrbs + 1);

        for (int i = 0; i < count; i++)
        {
            Instantiate(orbPrefab, origin, Quaternion.identity);
        }
    }
}