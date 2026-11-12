using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Barra de vida del jefe. Se muestra cuando aparece un enemigo con BossMarker.
public class BossHealthBar : MonoBehaviour
{
    [SerializeField] private WaveManager waveManager;

    [Header("UI")]
    [Tooltip("Objeto con CanvasGroup que contiene toda la barra")]
    [SerializeField] private CanvasGroup root;
    [SerializeField] private Image fill;
    [SerializeField] private Image trail;
    [SerializeField] private TMP_Text nameText;

    [Header("Animación")]
    [SerializeField] private float fadeDuration = 0.4f;
    [SerializeField] private float trailDelay = 0.4f;
    [SerializeField] private float trailSpeed = 1f;
    [SerializeField] private float hideDelay = 1.5f;

    private EnemyHealth boss;
    private bool shown;
    private float trailTimer;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (waveManager == null) waveManager = FindAnyObjectByType<WaveManager>();
        if (root != null) root.alpha = 0f;
    }

    private void OnEnable()
    {
        if (waveManager != null) waveManager.BossSpawned += Show;
    }

    private void OnDisable()
    {
        if (waveManager != null) waveManager.BossSpawned -= Show;
        Unsubscribe();
    }

    private void Show(EnemyHealth health, string displayName)
    {
        Unsubscribe();
        boss = health;
        boss.HealthChanged += OnHealthChanged;
        boss.Died += OnBossDied;

        if (nameText != null) nameText.text = displayName;
        fill.fillAmount = 1f;
        if (trail != null) trail.fillAmount = 1f;

        shown = true;
        FadeTo(1f, 0f);
    }

    private void Unsubscribe()
    {
        if (boss == null) return;
        boss.HealthChanged -= OnHealthChanged;
        boss.Died -= OnBossDied;
        boss = null;
    }

    private void OnHealthChanged(int current, int max)
    {
        fill.fillAmount = Mathf.Clamp01((float)current / max);
        trailTimer = trailDelay;
    }

    private void OnBossDied()
    {
        Unsubscribe();
        Hide(hideDelay);
    }

    private void Update()
    {
        // Si se reinicia la oleada (el jugador murió), el jefe se destruye sin morir
        if (shown && boss == null) Hide(0f);

        if (trail == null) return;
        if (trailTimer > 0f) { trailTimer -= Time.deltaTime; return; }
        trail.fillAmount = Mathf.MoveTowards(trail.fillAmount, fill.fillAmount, trailSpeed * Time.deltaTime);
    }

    private void Hide(float delay)
    {
        if (!shown) return;
        shown = false;
        FadeTo(0f, delay);
    }

    private void FadeTo(float target, float delay)
    {
        if (root == null) return;
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(target, delay));
    }

    private IEnumerator Fade(float target, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        float start = root.alpha;
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            root.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        root.alpha = target;
    }
}
