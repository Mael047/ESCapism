using System;
using System.Collections;
using UnityEngine;

public class WaveManager : MonoBehaviour
{
    [Header("Oleadas")]
    [SerializeField] private WaveData[] waves;
    [SerializeField] private float firstWaveDelay = 3f;
    [SerializeField] private float timeBetweenWaves = 5f;
    [SerializeField] private int maxAliveEnemies = 8;
    [Tooltip("Si muere, la siguiente noche empieza en la oleada donde murió")]
    [SerializeField] private bool resumeFromLastWave = false;

    [Header("Aparición")]
    [SerializeField] private Transform[] spawnPoints;
    [SerializeField] private float minDistanceFromPlayer = 6f;

    [Header("Fin de la noche")]
    [SerializeField] private float deathDelay = 2.5f;
    [SerializeField] private float victoryDelay = 3f;

    [Header("Tiendas")]
    [SerializeField] private PermanentShopUI permanentShop;
    [SerializeField] private TemporaryShopUI temporaryShop;
    [Tooltip("Después de qué oleadas aparece la tienda de temporales")]
    [SerializeField] private int[] temporaryShopAfterWaves = { 2, 4 };
    [Tooltip("Pausa antes de abrir la tienda, para ver morir al último enemigo")]
    [SerializeField] private float shopDelay = 1f;

    [Header("Ánimo")]
    [SerializeField] private MoodSettings moodSettings;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    // Eventos para la UI
    public event Action<int, int, bool> WaveStarted;   // oleada, total, ¿es jefe?
    public event Action<int> WaveCleared;
    public event Action NightCompleted;

    public int CurrentWave { get; private set; }       // empieza en 1
    public int AliveEnemies { get; private set; }

    private PlayerHealth player;
    private int pendingSpawns;
    private bool nightOver;
    public event Action<int> EnemiesRemainingChanged;

    public int TotalWaves => waves.Length;
    public int EnemiesRemaining => AliveEnemies + pendingSpawns;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerHealth>();
        if (player != null) player.Died += OnPlayerDied;

        StartCoroutine(RunNight(GetStartIndex()));
    }

    private void OnDestroy()
    {
        if (player != null) player.Died -= OnPlayerDied;
    }

    private int GetStartIndex()
    {
        if (!resumeFromLastWave || GameManager.Instance == null) return 0;

        int reached = GameManager.Instance.Data.nightWaveReached;
        return Mathf.Clamp(reached - 1, 0, waves.Length - 1);
    }

    // ---------- Flujo de la noche ----------

    private IEnumerator RunNight(int startIndex)
    {
        yield return null;

        if (permanentShop != null)
        {
            permanentShop.Open();
            while (permanentShop.IsOpen) yield return null;
        }

        yield return new WaitForSeconds(firstWaveDelay);

        for (int i = startIndex; i < waves.Length; i++)
        {
            yield return RunWave(i);
            if (nightOver) yield break;

            WaveCleared?.Invoke(CurrentWave);
            Debug.Log($"[WaveManager] Oleada {CurrentWave} completada");
            if (i < waves.Length - 1 && ShouldOpenTemporaryShop(CurrentWave))
            {
                yield return new WaitForSeconds(shopDelay);
                temporaryShop.Open();
                while (temporaryShop.IsOpen) yield return null;
            }

            if (i < waves.Length - 1)
            {
                yield return new WaitForSeconds(timeBetweenWaves);
            }
        }

        // Todas las oleadas completadas
        nightOver = true;
        NightCompleted?.Invoke();
        Debug.Log("[WaveManager] ¡Noche completada!");

        yield return new WaitForSeconds(victoryDelay);
        EndNight(playerDied: false);
    }

    private IEnumerator RunWave(int index)
    {
        WaveData wave = waves[index];
        CurrentWave = index + 1;
        pendingSpawns = wave.TotalEnemies;
        NotifyEnemies();

        if (GameManager.Instance != null)
        {
            RunData data = GameManager.Instance.Data;
            data.nightWaveReached = Mathf.Max(data.nightWaveReached, CurrentWave);
        }

        WaveStarted?.Invoke(CurrentWave, waves.Length, wave.isBossWave);
        Debug.Log($"[WaveManager] Oleada {CurrentWave}/{waves.Length}{(wave.isBossWave ? " - ¡JEFE!" : "")}");

        foreach (WaveData.SpawnGroup group in wave.groups)
        {
            for (int i = 0; i < group.count; i++)
            {
                // Esperar si ya hay demasiados enemigos vivos
                while (AliveEnemies >= maxAliveEnemies && !nightOver) yield return null;
                if (nightOver) yield break;

                Spawn(group.enemyPrefab);
                pendingSpawns--;
                NotifyEnemies();
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        // Esperar a que mueran todos
        while (AliveEnemies > 0 && !nightOver) yield return null;
    }

    // ---------- Enemigos ----------

    private void Spawn(GameObject prefab)
    {
        Transform point = PickSpawnPoint();
        GameObject enemy = Instantiate(prefab, point.position, point.rotation);

        EnemyHealth health = enemy.GetComponentInChildren<EnemyHealth>();
        if (health == null)
        {
            Debug.LogWarning($"[WaveManager] {prefab.name} no tiene EnemyHealth y no contará para la oleada");
            return;
        }

        AliveEnemies++;
        health.Died += OnEnemyDied;

        foreach (IAggroable aggroable in enemy.GetComponentsInChildren<IAggroable>())
        {
            aggroable.ForceAggro();
        }
    }

    private void OnEnemyDied()
    {
        AliveEnemies = Mathf.Max(0, AliveEnemies - 1);
        NotifyEnemies();
    }

    private Transform PickSpawnPoint()
    {
        // Preferir puntos que no estén encima del jugador
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Transform candidate = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
            if (player == null ||
                Vector3.Distance(candidate.position, player.transform.position) >= minDistanceFromPlayer)
            {
                return candidate;
            }
        }
        return spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
    }

    // ---------- Fin de la noche ----------

    private void OnPlayerDied()
    {
        if (nightOver) return;
        nightOver = true;
        StartCoroutine(EndAfterDelay());
    }

    private IEnumerator EndAfterDelay()
    {
        yield return new WaitForSeconds(deathDelay);   // tiempo para ver la muerte
        EndNight(playerDied: true);
    }

    private void EndNight(bool playerDied)
    {
        if (playerDied && moodSettings != null)
        {
            int wavesCleared = Mathf.Max(0, CurrentWave - 1);
            int penalty = moodSettings.GetNightPenalty(wavesCleared, waves.Length);

            RunData data = PermanentUpgrades.Data;
            data.stress = Mathf.Clamp(data.stress + penalty, 0, 100);
            data.depression = Mathf.Clamp(data.depression + penalty, 0, 100);

            Debug.Log($"[Ánimo] Noche perdida en la oleada {CurrentWave}: +{penalty} a ambos → " +
                      $"Estrés {data.stress} | Depresión {data.depression}");
        }
        if (GameManager.Instance != null)
        {
            GameManager.Instance.EndNight(playerDied);
        }
        else
        {
            Debug.Log($"[WaveManager] Fin de la noche (murió: {playerDied}). No hay GameManager: entra desde la escena de Día para el cambio de escena.");
        }
    }

    private void NotifyEnemies() => EnemiesRemainingChanged?.Invoke(EnemiesRemaining);

    // ---------- Debug ----------

    private void OnGUI()
    {
        if (!showDebug) return;

        GUI.Label(new Rect(10, 80, 500, 25),
            $"Oleada: {CurrentWave}/{waves.Length}  |  Vivos: {AliveEnemies}  |  Por aparecer: {pendingSpawns}");

        if (player != null)
        {
            GUI.Label(new Rect(10, 100, 500, 25), $"Vida jugador: {player.CurrentHealth}/{player.MaxHealth}");
        }
    }

    private bool ShouldOpenTemporaryShop(int wave)
    {
        return temporaryShop != null && Array.IndexOf(temporaryShopAfterWaves, wave) >= 0;
    }
}