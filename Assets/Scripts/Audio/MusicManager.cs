using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Reproduce la música del juego y hace crossfade entre canciones.
// Se crea solo al iniciar el juego y no se destruye al cambiar de escena.
// Si existe un prefab en Resources/MusicManager se usa ese (para asignar el Audio Mixer).
public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Tooltip("Opcional: grupo Music del Audio Mixer")]
    [SerializeField] private AudioMixerGroup output;
    [Range(0f, 1f)][SerializeField] private float masterVolume = 0.7f;
    [SerializeField] private float defaultFade = 1.5f;

    private const string VolumeKey = "MusicVolume";

    private AudioSource[] sources;
    private int activeIndex;
    private float activeClipVolume = 1f;
    private Coroutine fadeRoutine;

    public AudioClip CurrentClip => sources[activeIndex].isPlaying ? sources[activeIndex].clip : null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        MusicManager prefab = Resources.Load<MusicManager>("MusicManager");
        if (prefab != null) Instantiate(prefab);
        else new GameObject("MusicManager").AddComponent<MusicManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        masterVolume = PlayerPrefs.GetFloat(VolumeKey, masterVolume);

        sources = new AudioSource[2];
        for (int i = 0; i < 2; i++)
        {
            AudioSource s = gameObject.AddComponent<AudioSource>();
            s.loop = true;
            s.playOnAwake = false;
            s.spatialBlend = 0f;   // 2D
            s.volume = 0f;
            s.outputAudioMixerGroup = output;
            s.ignoreListenerPause = true;   // la música sigue en el menú de pausa
            sources[i] = s;
        }
    }

    // ---------- API ----------

    // Cambia a otra canción con crossfade. Si ya está sonando, no la reinicia.
    public void Play(AudioClip clip, float volume = 1f, float fade = -1f)
    {
        if (clip == null) return;
        if (fade < 0f) fade = defaultFade;

        AudioSource current = sources[activeIndex];
        if (current.clip == clip && current.isPlaying)
        {
            activeClipVolume = volume;
            StartFade(fade);
            return;
        }

        activeIndex = 1 - activeIndex;
        AudioSource next = sources[activeIndex];
        next.clip = clip;
        next.volume = 0f;
        next.Play();
        activeClipVolume = volume;

        StartFade(fade);
    }

    // Apaga la música poco a poco (la usa la pantalla de carga)
    public void FadeOut(float fade = -1f)
    {
        if (fade < 0f) fade = defaultFade;
        activeClipVolume = 0f;
        StartFade(fade);
    }

    // Para un menú de opciones: 0 a 1, se guarda entre partidas
    public void SetVolume(float value)
    {
        masterVolume = Mathf.Clamp01(value);
        PlayerPrefs.SetFloat(VolumeKey, masterVolume);
        if (fadeRoutine == null) sources[activeIndex].volume = masterVolume * activeClipVolume;
    }

    public float Volume => masterVolume;

    // ---------- Crossfade ----------

    private void StartFade(float duration)
    {
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Crossfade(duration));
    }

    private IEnumerator Crossfade(float duration)
    {
        AudioSource incoming = sources[activeIndex];
        AudioSource outgoing = sources[1 - activeIndex];
        float inStart = incoming.volume;
        float outStart = outgoing.volume;

        // Tiempo real: no le afectan la pausa ni el hit stop
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            float k = t / duration;
            incoming.volume = Mathf.Lerp(inStart, masterVolume * activeClipVolume, k);
            outgoing.volume = Mathf.Lerp(outStart, 0f, k);
            yield return null;
        }

        incoming.volume = masterVolume * activeClipVolume;
        outgoing.volume = 0f;
        outgoing.Stop();
        if (activeClipVolume <= 0f) incoming.Stop();

        fadeRoutine = null;
    }
}
