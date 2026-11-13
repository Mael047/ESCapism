using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

// Reproduce efectos de sonido con un grupo de AudioSources reutilizables.
// Uso desde cualquier script:  SFX.Play(sonido, posicion)  o  SFX.Play(sonido)  (2D)
// Se crea solo. Si existe un prefab en Resources/SFX se usa ese (para asignar el Audio Mixer).
public class SFX : MonoBehaviour
{
    public static SFX Instance { get; private set; }

    [Tooltip("Grupo SFX del Audio Mixer")]
    [SerializeField] private AudioMixerGroup output;
    [SerializeField] private int poolSize = 32;

    private readonly List<AudioSource> pool = new List<AudioSource>();
    private readonly Dictionary<AudioSource, SoundData> owners = new Dictionary<AudioSource, SoundData>();
    private readonly Dictionary<SoundData, float> lastPlayed = new Dictionary<SoundData, float>();
    private int nextSteal;

    public AudioMixerGroup Output => output;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Instance = null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        SFX prefab = Resources.Load<SFX>("SFX");
        if (prefab != null) Instantiate(prefab);
        else new GameObject("SFX").AddComponent<SFX>();
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

        for (int i = 0; i < poolSize; i++)
        {
            var go = new GameObject("Source " + i);
            go.transform.SetParent(transform, false);
            AudioSource source = go.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.ignoreListenerPause = true;   // los sonidos de UI funcionan en pausa
            pool.Add(source);
        }
    }

    // ---------- API ----------

    // Sonido en un punto del mundo (3D según su Spatial Blend)
    public static AudioSource Play(SoundData sound, Vector3 position)
    {
        return Instance != null ? Instance.PlayInternal(sound, position, true) : null;
    }

    // Sonido 2D (UI, anuncios)
    public static AudioSource Play(SoundData sound)
    {
        return Instance != null ? Instance.PlayInternal(sound, Vector3.zero, false) : null;
    }

    // ---------- Interno ----------

    private AudioSource PlayInternal(SoundData sound, Vector3 position, bool positional)
    {
        if (sound == null) return null;

        // Cooldown
        float now = Time.unscaledTime;
        if (lastPlayed.TryGetValue(sound, out float last) && now - last < sound.cooldown) return null;

        // Máximo de copias simultáneas
        int playing = 0;
        foreach (AudioSource s in pool)
        {
            if (s.isPlaying && owners.TryGetValue(s, out SoundData o) && o == sound) playing++;
        }
        if (playing >= sound.maxInstances) return null;

        AudioClip clip = sound.PickClip();
        if (clip == null) return null;

        AudioSource source = GetFreeSource();
        lastPlayed[sound] = now;
        owners[source] = sound;

        source.transform.position = position;
        source.clip = clip;
        source.spatialBlend = positional ? sound.spatialBlend : 0f;
        source.minDistance = sound.minDistance;
        source.maxDistance = sound.maxDistance;
        source.volume = sound.volume * (1f - Random.Range(0f, sound.volumeVariation));
        source.pitch = Random.Range(sound.pitch.x, sound.pitch.y);
        source.outputAudioMixerGroup = sound.output != null ? sound.output : output;
        source.Play();
        return source;
    }

    private AudioSource GetFreeSource()
    {
        foreach (AudioSource s in pool)
        {
            if (!s.isPlaying) return s;
        }
        // Todas ocupadas: reutiliza la más vieja en turno
        AudioSource stolen = pool[nextSteal];
        nextSteal = (nextSteal + 1) % pool.Count;
        stolen.Stop();
        return stolen;
    }
}
