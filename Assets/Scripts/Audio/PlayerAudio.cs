using UnityEngine;

// Sonidos del jugador. Ponlo en el Player (donde está PlayerHealth).
public class PlayerAudio : MonoBehaviour
{
    [SerializeField] private SoundData hurt;
    [SerializeField] private SoundData death;
    [Tooltip("Al recoger una orbe de vida")]
    [SerializeField] private SoundData heal;
    [SerializeField] private SoundData revive;
    [Tooltip("Se llama con el evento de animación Footstep")]
    [SerializeField] private SoundData footstep;

    private PlayerHealth health;

    private void Awake() => health = GetComponent<PlayerHealth>();

    private void OnEnable()
    {
        if (health == null) return;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
        health.Healed += OnHealed;
        health.Revived += OnRevived;
    }

    private void OnDisable()
    {
        if (health == null) return;
        health.Damaged -= OnDamaged;
        health.Died -= OnDied;
        health.Healed -= OnHealed;
        health.Revived -= OnRevived;
    }

    private void OnDamaged(int amount) => SFX.Play(hurt, transform.position);
    private void OnDied() => SFX.Play(death, transform.position);
    private void OnHealed(int amount) => SFX.Play(heal, transform.position);
    private void OnRevived() => SFX.Play(revive, transform.position);

    public void Footstep() => SFX.Play(footstep, transform.position);
}
