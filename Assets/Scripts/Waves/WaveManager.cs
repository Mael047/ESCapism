using System;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Vidas")]
    [SerializeField] private int livesPerNight = 2;
    [Tooltip("Segundos tras morir antes de reaparecer (deja ver la desintegración)")]
    [SerializeField] private float respawnDelay = 5f;
    [Tooltip("Segundos tras reaparecer antes de que empiece de nuevo la oleada")]
    [SerializeField] private float restartDelay = 2f;
    [Tooltip("Opcional: si se deja vacío, reaparece donde empezó la noche")]
    [SerializeField] private Transform playerRespawnPoint;




    [Header("Debug")]
    [SerializeField] private bool showDebug = true;

    // Eventos para la UI
    public event Action<int, int, bool> WaveStarted;   // oleada, total, ¿es jefe?
    public event Action<int> WaveCleared;
    public event Action NightCompleted;
    public event Action<EnemyHealth, string> BossSpawned;   // vida del jefe, nombre

    public int CurrentWave { get; private set; }       // empieza en 1
    public int AliveEnemies { get; private set; }

    private PlayerHealth player;
    private int pendingSpawns;
    private bool nightOver;
    public event Action<int> EnemiesRemainingChanged;

    public int TotalWaves => waves.Length;
    public int EnemiesRemaining => AliveEnemies + pendingSpawns;

    public event Action<int, int> LivesChanged;   // vidas restantes, vidas totales
    public event Action<int> WaveRestarted;

    public int LivesRemaining { get; private set; }
    public int LivesPerNight => livesPerNight;

    private bool waveFailed;
    private bool WaveInterrupted => nightOver || waveFailed;
    private readonly List<GameObject> activeEnemies = new List<GameObject>();
    private Vector3 respawnPosition;
    private Quaternion respawnRotation;

    private void Start()
    {
        player = FindAnyObjectByType<PlayerHealth>();
        if (player != null) player.Died += OnPlayerDied;

        LivesRemaining = livesPerNight;
        LivesChanged?.Invoke(LivesRemaining, livesPerNight);

        if (player != null)
        {
            respawnPosition = player.transform.position;
            respawnRotation = player.transform.rotation;
        }

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

        for (int i = startIndex; i < waves.Length;)
        {
            yield return RunWave(i);
            if (nightOver) yield break;

            // Murió con vidas de sobra: reaparece y repite esta misma oleada
            if (waveFailed)
            {
                yield return RespawnRoutine();
                continue;
            }

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

            i++;   // solo avanza si la oleada se completó
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
                while (AliveEnemies >= maxAliveEnemies && !WaveInterrupted) yield return null;
                if (WaveInterrupted) yield break;

                Spawn(group.enemyPrefab);
                pendingSpawns--;
                NotifyEnemies();
                yield return new WaitForSeconds(wave.spawnInterval);
            }
        }

        // Esperar a que mueran todos
        while (AliveEnemies > 0 && !WaveInterrupted) yield return null;
    }

    // ---------- Enemigos ----------

    private void Spawn(GameObject prefab)
    {
        Transform point = PickSpawnPoint();
        GameObject enemy = Instantiate(prefab, point.position, point.rotation);
        Register(enemy);
    }

    // Para enemigos que no salen de la oleada (ej. los que invoca el jefe)
    public void RegisterExternalEnemy(GameObject enemy)
    {
        if (WaveInterrupted)
        {
            Destroy(enemy);
            return;
        }
        Register(enemy);
        NotifyEnemies();
    }

    private void Register(GameObject enemy)
    {
        EnemyHealth health = enemy.GetComponentInChildren<EnemyHealth>();
        if (health == null)
        {
            Debug.LogWarning($"[WaveManager] {enemy.name} no tiene EnemyHealth y no contará para la oleada");
            return;
        }

        AliveEnemies++;
        activeEnemies.Add(enemy);
        health.Died += () => OnEnemyDied(enemy);

        foreach (IAggroable aggroable in enemy.GetComponentsInChildren<IAggroable>())
        {
            aggroable.ForceAggro();
        }

        BossMarker boss = enemy.GetComponentInChildren<BossMarker>();
        if (boss != null) BossSpawned?.Invoke(health, boss.DisplayName);
    }

    private void OnEnemyDied(GameObject enemy)
    {
        activeEnemies.Remove(enemy);
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
        if (nightOver || waveFailed) return;

        LivesRemaining--;
        LivesChanged?.Invoke(LivesRemaining, livesPerNight);

        if (LivesRemaining <= 0)
        {
            // Segunda muerte: termina la noche
            nightOver = true;
            StartCoroutine(EndAfterDelay());
        }
        else
        {
            // Primera muerte: RunNight reinicia la oleada
            waveFailed = true;
            Debug.Log($"[WaveManager] Murió en la oleada {CurrentWave}. Vidas restantes: {LivesRemaining}");
        }
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

    private IEnumerator RespawnRoutine()
    {
        // Dejar ver la animación de muerte y la desintegración
        yield return new WaitForSeconds(respawnDelay);

        ClearWave();

        Vector3 position = playerRespawnPoint != null ? playerRespawnPoint.position : respawnPosition;
        Quaternion rotation = playerRespawnPoint != null ? playerRespawnPoint.rotation : respawnRotation;
        player.Revive(position, rotation);

        WaveRestarted?.Invoke(CurrentWave);
        yield return new WaitForSeconds(restartDelay);

        waveFailed = false;
    }

    // Elimina los enemigos y las orbes de la oleada fallida
    private void ClearWave()
    {
        foreach (GameObject enemy in activeEnemies)
        {
            if (enemy != null) Destroy(enemy);
        }
        activeEnemies.Clear();
        AliveEnemies = 0;
        pendingSpawns = 0;
        NotifyEnemies();

        foreach (HealthOrb orb in FindObjectsByType<HealthOrb>(FindObjectsSortMode.None))
        {
            Destroy(orb.gameObject);
        }
    }
}