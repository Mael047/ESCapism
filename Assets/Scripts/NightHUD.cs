using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NightHUD : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private PlayerHealth playerHealth;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private WaveManager waveManager;

    [Header("Vida")]
    [SerializeField] private Image healthFill;
    [SerializeField] private Image healthTrail;
    [SerializeField] private TMP_Text healthText;
    [SerializeField]
    private Gradient healthColor = new Gradient
    {
        colorKeys = new[]
        {
            new GradientColorKey(new Color(0.9f, 0.25f, 0.3f), 0f),    // poca vida
            new GradientColorKey(new Color(1f, 0.75f, 0.25f), 0.5f),
            new GradientColorKey(new Color(0.4f, 0.85f, 0.5f), 1f)     // vida llena
        }
    };
    [Tooltip("Segundos antes de que la barra de rastro empiece a bajar")]
    [SerializeField] private float trailDelay = 0.4f;
    [SerializeField] private float trailSpeed = 1.5f;

    [Header("Oleada")]
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private TMP_Text enemiesText;

    [Header("Stats")]
    [SerializeField] private TMP_Text statsText;

    [Header("Curación")]
    [SerializeField] private RectTransform healthBarRoot;
    [SerializeField] private TMP_Text healPopupText;
    [SerializeField] private Color healColor = new Color(0.55f, 1f, 0.6f);
    [SerializeField] private float healFeedbackDuration = 0.9f;
    [SerializeField] private float punchScale = 1.12f;

    [Header("Anuncios")]
    [SerializeField] private TMP_Text bannerText;
    [SerializeField] private float bannerDuration = 2f;
    [SerializeField] private float bannerFade = 0.3f;

    private Coroutine bannerRoutine;
    private Coroutine healRoutine;
    private int accumulatedHeal;
    private Vector2 popupStartPosition;

    private float trailTimer;

    private void Awake()
    {
        // El HUD vive en el prefab UIRoot, así que busca las referencias de la escena
        if (playerHealth == null) playerHealth = FindAnyObjectByType<PlayerHealth>();
        if (playerStats == null) playerStats = FindAnyObjectByType<PlayerStats>();
        if (waveManager == null) waveManager = FindAnyObjectByType<WaveManager>();
        if (healPopupText != null)
        {
            popupStartPosition = healPopupText.rectTransform.anchoredPosition;
            healPopupText.gameObject.SetActive(false);
        }
        if (bannerText != null) bannerText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.Healed += OnHealed;
            playerHealth.HealthChanged += OnHealthChanged;
        }
        if (playerStats != null) playerStats.StatsChanged += UpdateStats;
        if (waveManager != null)
        {
            waveManager.WaveStarted += UpdateWave;
            waveManager.EnemiesRemainingChanged += UpdateEnemies;
            waveManager.WaveCleared += OnWaveCleared;
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.Healed -= OnHealed;
            playerHealth.HealthChanged -= OnHealthChanged;
        }
        if (playerStats != null) playerStats.StatsChanged -= UpdateStats;
        if (waveManager != null)
        {
            waveManager.WaveStarted -= UpdateWave;
            waveManager.EnemiesRemainingChanged -= UpdateEnemies;
            waveManager.WaveCleared -= OnWaveCleared;
        }
    }

    private void Start()
    {
        UpdateHealth(instant: true);
        UpdateStats();

        if (waveText != null) waveText.text = "Preparándose...";
        if (enemiesText != null) enemiesText.text = "";
    }

    private void Update()
    {
        if (healthTrail == null) return;

        // La barra de rastro espera un momento y luego alcanza a la barra de vida
        if (trailTimer > 0f)
        {
            trailTimer -= Time.deltaTime;
            return;
        }

        healthTrail.fillAmount = Mathf.MoveTowards(
            healthTrail.fillAmount, healthFill.fillAmount, trailSpeed * Time.deltaTime);
    }

    // ---------- Vida ----------

    private void OnHealthChanged() => UpdateHealth(instant: false);

    private void UpdateHealth(bool instant)
    {
        if (playerHealth == null) return;

        int current = playerHealth.CurrentHealth;
        int max = playerHealth.MaxHealth;
        float ratio = max > 0 ? (float)current / max : 0f;

        healthFill.fillAmount = ratio;
        healthFill.color = healthColor.Evaluate(ratio);
        if (healthText != null) healthText.text = $"{current} / {max}";

        if (healthTrail == null) return;

        if (instant || ratio >= healthTrail.fillAmount)
        {
            // Al empezar o al curarse, el rastro se pone al día de inmediato
            healthTrail.fillAmount = ratio;
        }
        else
        {
            // Al recibir daño, el rastro se queda atrás un momento
            trailTimer = trailDelay;
        }
    }

    // ---------- Oleada ----------

    private void UpdateWave(int wave, int total, bool isBoss)
    {
        if (waveText == null) return;
        waveText.text = isBoss ? $"Oleada {wave}/{total} — ¡JEFE!" : $"Oleada {wave}/{total}";
        ShowBanner(isBoss ? "¡Llega el jefe!" : $"Oleada {wave}");
    }

    private void UpdateEnemies(int remaining)
    {
        if (enemiesText != null) enemiesText.text = $"Enemigos: {remaining}";
    }

    // ---------- Stats ----------

    private void UpdateStats()
    {
        if (statsText == null || playerStats == null) return;

        statsText.text =
    $"Fuerza  {playerStats.DamageBonus * 100f:+0;-0;0}%\n" +
    $"Remate  {playerStats.FinisherBonus * 100f:+0;-0;0}%\n" +
    $"Agilidad  {(playerStats.SpeedMultiplier - 1f) * 100f:+0;-0;0}%\n" +
    $"Resistencia  {(playerStats.HealthMultiplier - 1f) * 100f:+0;-0;0}%";
        statsText.text +=
            $"\n\n<size=80%>Estrés {PermanentUpgrades.Data.stress}  ·  Depresión {PermanentUpgrades.Data.depression}</size>";
    }

    private void OnHealed(int amount)
    {
        // Si llegan varias orbes seguidas, el número se acumula: "+18", luego "+40"
        accumulatedHeal = healRoutine != null ? accumulatedHeal + amount : amount;

        if (healRoutine != null) StopCoroutine(healRoutine);
        healRoutine = StartCoroutine(HealFeedback());
    }

    private System.Collections.IEnumerator HealFeedback()
    {
        if (healPopupText != null)
        {
            healPopupText.gameObject.SetActive(true);
            healPopupText.text = $"+{accumulatedHeal}";
        }

        float timer = 0f;
        while (timer < healFeedbackDuration)
        {
            timer += Time.deltaTime;
            float t = timer / healFeedbackDuration;

            // La barra rebota al inicio
            if (healthBarRoot != null)
            {
                float punch = Mathf.Lerp(punchScale, 1f, Mathf.Clamp01(t * 4f));
                healthBarRoot.localScale = Vector3.one * punch;
            }

            // El relleno se ilumina con el color de curación y vuelve a su color
            float ratio = healthFill.fillAmount;
            float glow = 1f - Mathf.Clamp01(t * 3f);
            healthFill.color = Color.Lerp(healthColor.Evaluate(ratio), healColor, glow);

            // El "+N" sube y se desvanece
            if (healPopupText != null)
            {
                healPopupText.rectTransform.anchoredPosition = popupStartPosition + Vector2.up * (40f * t);
                Color c = healColor;
                c.a = 1f - t * t;
                healPopupText.color = c;
            }

            yield return null;
        }

        if (healthBarRoot != null) healthBarRoot.localScale = Vector3.one;
        healthFill.color = healthColor.Evaluate(healthFill.fillAmount);
        if (healPopupText != null) healPopupText.gameObject.SetActive(false);

        healRoutine = null;
    }
    private void OnWaveCleared(int wave)
    {
        ShowBanner("¡Oleada completada!");
    }

    private void ShowBanner(string message)
    {
        if (bannerText == null) return;
        if (bannerRoutine != null) StopCoroutine(bannerRoutine);
        bannerRoutine = StartCoroutine(BannerRoutine(message));
    }

    private System.Collections.IEnumerator BannerRoutine(string message)
    {
        bannerText.text = message;
        bannerText.gameObject.SetActive(true);

        float timer = 0f;
        while (timer < bannerDuration)
        {
            timer += Time.deltaTime;

            // Aparece y desaparece con un fundido
            float fadeIn = Mathf.Clamp01(timer / bannerFade);
            float fadeOut = Mathf.Clamp01((bannerDuration - timer) / bannerFade);
            bannerText.alpha = Mathf.Min(fadeIn, fadeOut);

            // Pequeño "pop" al aparecer
            float scale = Mathf.Lerp(1.2f, 1f, fadeIn);
            bannerText.rectTransform.localScale = Vector3.one * scale;

            yield return null;
        }

        bannerText.gameObject.SetActive(false);
        bannerRoutine = null;
    }
}