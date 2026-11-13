using UnityEngine;

// Sonidos de un enemigo. Ponlo en la raíz del murciélago (donde está EnemyHealth).
// En las variantes (fuerte, jefe) puedes poner otros sonidos o los mismos con tono más grave.
public class EnemyAudio : MonoBehaviour
{
    [Header("Aleteo continuo")]
    [Tooltip("Sonido en loop mientras vive (usa el primer clip)")]
    [SerializeField] private SoundData wingLoop;

    [Header("Ataque")]
    [SerializeField] private SoundData charge;
    [SerializeField] private SoundData lunge;

    [Header("Daño")]
    [SerializeField] private SoundData hurt;
    [SerializeField] private SoundData death;

    private EnemyHealth health;
    private BatAI bat;
    private AudioSource loopSource;

    private void Awake()
    {
        // Busca en este objeto, en los padres y en los hijos (EnemyAudio puede ir en un hijo)
        health = GetComponentInParent<EnemyHealth>();
        if (health == null) health = GetComponentInChildren<EnemyHealth>();
        bat = GetComponentInParent<BatAI>();
        if (bat == null) bat = GetComponentInChildren<BatAI>();

        if (health == null) Debug.LogWarning($"[EnemyAudio] {name} no encontró EnemyHealth", this);
        CreateLoop();
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
        if (bat != null)
        {
            bat.WindupStarted += OnWindup;
            bat.LungeStarted += OnLunge;
        }
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
        if (bat != null)
        {
            bat.WindupStarted -= OnWindup;
            bat.LungeStarted -= OnLunge;
        }
    }

    private void CreateLoop()
    {
        if (wingLoop == null || wingLoop.clips == null || wingLoop.clips.Length == 0) return;

        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.clip = wingLoop.clips[0];
        loopSource.loop = true;
        loopSource.volume = wingLoop.volume;
        loopSource.pitch = Random.Range(wingLoop.pitch.x, wingLoop.pitch.y);
        loopSource.spatialBlend = wingLoop.spatialBlend;
        loopSource.minDistance = wingLoop.minDistance;
        loopSource.maxDistance = wingLoop.maxDistance;
        loopSource.rolloffMode = AudioRolloffMode.Linear;
        if (SFX.Instance != null)
            loopSource.outputAudioMixerGroup = wingLoop.output != null ? wingLoop.output : SFX.Instance.Output;

        // Empieza en un punto al azar para que varios murciélagos no suenen sincronizados
        loopSource.time = Random.Range(0f, loopSource.clip.length);
        loopSource.Play();
    }

    private void OnWindup() => SFX.Play(charge, transform.position);
    private void OnLunge() => SFX.Play(lunge, transform.position);
    private void OnDamaged(int amount) => SFX.Play(hurt, transform.position);

    private void OnDied()
    {
        SFX.Play(death, transform.position);
        if (loopSource != null) loopSource.Stop();
    }
}
