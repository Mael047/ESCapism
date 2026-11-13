using UnityEngine;

// Sonidos del jugador. Ponlo en el Player (donde está PlayerHealth).
// Efectos: suenan siempre. Voces: la voz de la chica, en su propio canal y sin encimarse.
public class PlayerAudio : MonoBehaviour
{
    [Header("Efectos")]
    [SerializeField] private SoundData hurt;
    [SerializeField] private SoundData death;
    [Tooltip("Al recoger una orbe de vida")]
    [SerializeField] private SoundData heal;
    [SerializeField] private SoundData revive;
    [Tooltip("Se llama con el evento de animación Footstep")]
    [SerializeField] private SoundData footstep;

    [Header("Voz")]
    [SerializeField] private SoundData attackVoice;
    [SerializeField] private SoundData hurtVoice;
    [SerializeField] private SoundData healVoice;
    [SerializeField] private SoundData dashVoice;
    [Tooltip("Opcional")]
    [SerializeField] private SoundData deathVoice;

    [Header("Frecuencia de la voz")]
    [Tooltip("Probabilidad de que diga algo en cada golpe (0-1). Si habla en todos, cansa")]
    [Range(0f, 1f)][SerializeField] private float attackVoiceChance = 0.4f;
    [Tooltip("El golpe final del combo siempre tiene voz")]
    [SerializeField] private bool finisherAlwaysVoice = true;
    [Range(0f, 1f)][SerializeField] private float dashVoiceChance = 0.5f;
    [Tooltip("Segundos mínimos entre dos voces (salvo daño y muerte)")]
    [SerializeField] private float voiceGap = 0.6f;

    private PlayerHealth health;
    private PlayerCombat combat;
    private PlayerDash dash;

    private AudioSource voiceSource;
    private int voicePriority;
    private float lastVoiceTime = -10f;

    // Prioridades: un grito de daño corta una voz de ataque, pero no al revés
    private const int AttackPriority = 0, DashPriority = 1, HealPriority = 1, HurtPriority = 2, DeathPriority = 3;

    private void Awake()
    {
        health = GetComponent<PlayerHealth>();
        combat = GetComponent<PlayerCombat>();
        dash = GetComponent<PlayerDash>();

        voiceSource = gameObject.AddComponent<AudioSource>();
        voiceSource.playOnAwake = false;
        voiceSource.spatialBlend = 0f;
    }

    private void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            health.Healed += OnHealed;
            health.Revived += OnRevived;
        }
        if (combat != null) combat.AttackStarted += OnAttackStarted;
        if (dash != null) dash.DashStarted += OnDashStarted;
    }

    private void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
            health.Healed -= OnHealed;
            health.Revived -= OnRevived;
        }
        if (combat != null) combat.AttackStarted -= OnAttackStarted;
        if (dash != null) dash.DashStarted -= OnDashStarted;
    }

    // ---------- Eventos ----------

    private void OnDamaged(int amount)
    {
        SFX.Play(hurt, transform.position);
        PlayVoice(hurtVoice, HurtPriority, 1f);
    }

    private void OnDied()
    {
        SFX.Play(death, transform.position);
        PlayVoice(deathVoice, DeathPriority, 1f);
    }

    private void OnHealed(int amount)
    {
        SFX.Play(heal, transform.position);
        PlayVoice(healVoice, HealPriority, 1f);
    }

    private void OnRevived()
    {
        SFX.Play(revive, transform.position);
        voicePriority = 0;   // se puede volver a hablar
    }

    private void OnAttackStarted(AttackData attack)
    {
        float chance = finisherAlwaysVoice && attack.isFinisher ? 1f : attackVoiceChance;
        PlayVoice(attackVoice, AttackPriority, chance);
    }

    private void OnDashStarted() => PlayVoice(dashVoice, DashPriority, dashVoiceChance);

    public void Footstep() => SFX.Play(footstep, transform.position);

    // ---------- Voz ----------

    private void PlayVoice(SoundData voice, int priority, float chance)
    {
        if (voice == null || Random.value > chance) return;

        bool speaking = voiceSource.isPlaying;
        if (speaking && priority < voicePriority) return;               // no cortar algo más importante
        if (priority < HurtPriority && Time.unscaledTime - lastVoiceTime < voiceGap) return;

        AudioClip clip = voice.PickClip();
        if (clip == null) return;

        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.volume = voice.volume * (1f - Random.Range(0f, voice.volumeVariation));
        voiceSource.pitch = Random.Range(voice.pitch.x, voice.pitch.y);
        voiceSource.outputAudioMixerGroup = voice.output != null ? voice.output
                                          : (SFX.Instance != null ? SFX.Instance.Output : null);
        voiceSource.Play();

        voicePriority = priority;
        lastVoiceTime = Time.unscaledTime;
    }
}
