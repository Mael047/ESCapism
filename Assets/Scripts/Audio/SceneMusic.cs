using UnityEngine;

// Música de una escena. Ponlo en un objeto de cada escena (Portada, Oxxo, Noche).
// En la noche, si llenas "Wave Tracks", cambia de canción según la oleada.
public class SceneMusic : MonoBehaviour
{
    [Header("Música de la escena")]
    [Tooltip("Suena al entrar a la escena. En la noche puede quedar vacío (usa la primera de Wave Tracks)")]
    [SerializeField] private AudioClip music;
    [Range(0f, 1f)][SerializeField] private float volume = 1f;
    [SerializeField] private float fadeIn = 2f;

    [Header("Noche (opcional)")]
    [Tooltip("Canciones de la noche en orden")]
    [SerializeField] private AudioClip[] waveTracks;
    [Tooltip("Cada cuántas oleadas cambia la canción")]
    [SerializeField] private int wavesPerTrack = 2;
    [SerializeField] private float trackCrossfade = 3f;
    [SerializeField] private WaveManager waveManager;

    private void Start()
    {
        if (MusicManager.Instance == null) return;

        bool usesWaves = waveTracks != null && waveTracks.Length > 0;
        if (usesWaves)
        {
            if (waveManager == null) waveManager = FindAnyObjectByType<WaveManager>();
            if (waveManager != null) waveManager.WaveStarted += OnWaveStarted;
        }

        AudioClip first = music != null ? music : (usesWaves ? waveTracks[0] : null);
        MusicManager.Instance.Play(first, volume, fadeIn);
    }

    private void OnDestroy()
    {
        if (waveManager != null) waveManager.WaveStarted -= OnWaveStarted;
    }

    // Oleadas 1-2 → canción 1, 3-4 → canción 2, 5-6 → canción 3
    private void OnWaveStarted(int wave, int total, bool isBoss)
    {
        int index = Mathf.Clamp((wave - 1) / Mathf.Max(1, wavesPerTrack), 0, waveTracks.Length - 1);
        AudioClip clip = waveTracks[index];
        if (clip == null) return;

        // Si es la misma que ya suena (ej. oleada 2, o repetir oleada al morir), no se reinicia
        MusicManager.Instance.Play(clip, volume, trackCrossfade);
    }
}
