using UnityEngine;

// Sonidos de anuncio de la noche (2D). Ponlo en un objeto de la escena de noche.
public class NightAudio : MonoBehaviour
{
    [SerializeField] private WaveManager waveManager;

    [SerializeField] private SoundData waveStart;
    [Tooltip("Si está vacío, usa Wave Start")]
    [SerializeField] private SoundData bossWaveStart;
    [SerializeField] private SoundData waveCleared;
    [SerializeField] private SoundData lifeLost;
    [Tooltip("Al reaparecer y repetir la oleada")]
    [SerializeField] private SoundData waveRestarted;
    [SerializeField] private SoundData nightCompleted;

    private int lastLives = -1;

    private void Awake()
    {
        if (waveManager == null) waveManager = FindAnyObjectByType<WaveManager>();
    }

    private void OnEnable()
    {
        if (waveManager == null) return;
        waveManager.WaveStarted += OnWaveStarted;
        waveManager.WaveCleared += OnWaveCleared;
        waveManager.LivesChanged += OnLivesChanged;
        waveManager.WaveRestarted += OnWaveRestarted;
        waveManager.NightCompleted += OnNightCompleted;
    }

    private void OnDisable()
    {
        if (waveManager == null) return;
        waveManager.WaveStarted -= OnWaveStarted;
        waveManager.WaveCleared -= OnWaveCleared;
        waveManager.LivesChanged -= OnLivesChanged;
        waveManager.WaveRestarted -= OnWaveRestarted;
        waveManager.NightCompleted -= OnNightCompleted;
    }

    private void OnWaveStarted(int wave, int total, bool isBoss)
    {
        SFX.Play(isBoss && bossWaveStart != null ? bossWaveStart : waveStart);
    }

    private void OnWaveCleared(int wave) => SFX.Play(waveCleared);
    private void OnWaveRestarted(int wave) => SFX.Play(waveRestarted);
    private void OnNightCompleted() => SFX.Play(nightCompleted);

    private void OnLivesChanged(int remaining, int total)
    {
        // El primer aviso es el valor inicial, no una vida perdida
        if (lastLives >= 0 && remaining < lastLives) SFX.Play(lifeLost);
        lastLives = remaining;
    }
}
