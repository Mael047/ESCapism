using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Pantalla de opciones: volumen, sensibilidad de cámara y pantalla completa.
// Crea el prefab con: Tools → UI → Crear prefab Menú de Opciones
// Ábrela desde un botón con OptionsMenu → Open.
public class OptionsMenu : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private AudioMixer mixer;

    [SerializeField] private Slider musicSlider;
    [SerializeField] private TMP_Text musicValue;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private TMP_Text sfxValue;
    [SerializeField] private Slider mouseSlider;
    [SerializeField] private TMP_Text mouseValue;
    [SerializeField] private Slider stickSlider;
    [SerializeField] private TMP_Text stickValue;
    [SerializeField] private Toggle fullscreenToggle;

    [SerializeField] private float fadeDuration = 0.2f;

    [Tooltip("Se llama al cerrar (ej. volver a mostrar el menú principal)")]
    public UnityEvent onClosed;

    public bool IsOpen { get; private set; }

    private Coroutine fadeRoutine;

    private void Awake()
    {
        SetVisible(false, instant: true);
    }

    private void Start()
    {
        GameSettings.RegisterMixer(mixer);

        Setup(musicSlider, 0f, 1f, v => { GameSettings.MusicVolume = v; RefreshLabels(); });
        Setup(sfxSlider, 0f, 1f, v => { GameSettings.SfxVolume = v; RefreshLabels(); });
        Setup(mouseSlider, GameSettings.MinSensitivity, GameSettings.MaxSensitivity,
              v => { GameSettings.MouseSensitivity = v; RefreshLabels(); });
        Setup(stickSlider, GameSettings.MinSensitivity, GameSettings.MaxSensitivity,
              v => { GameSettings.StickSensitivity = v; RefreshLabels(); });

        if (fullscreenToggle != null)
            fullscreenToggle.onValueChanged.AddListener(v => GameSettings.Fullscreen = v);

        Refresh();
    }

    private static void Setup(Slider slider, float min, float max, UnityAction<float> onChange)
    {
        if (slider == null) return;
        slider.minValue = min;
        slider.maxValue = max;
        slider.onValueChanged.AddListener(onChange);
    }

    // ---------- Abrir / cerrar ----------

    public void Open()
    {
        Refresh();
        IsOpen = true;
        SetVisible(true, instant: false);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        PlayerPrefs.Save();
        SetVisible(false, instant: false);
        onClosed?.Invoke();
    }

    private void Update()
    {
        if (!IsOpen) return;
        Keyboard k = Keyboard.current;
        Gamepad g = Gamepad.current;
        if ((k != null && k.escapeKey.wasPressedThisFrame) || (g != null && g.buttonEast.wasPressedThisFrame))
            Close();
    }

    // ---------- Valores ----------

    // Pone los controles con los valores guardados
    private void Refresh()
    {
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
        if (mouseSlider != null) mouseSlider.SetValueWithoutNotify(GameSettings.MouseSensitivity);
        if (stickSlider != null) stickSlider.SetValueWithoutNotify(GameSettings.StickSensitivity);
        if (fullscreenToggle != null) fullscreenToggle.SetIsOnWithoutNotify(GameSettings.Fullscreen);
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (musicValue != null) musicValue.text = $"{Mathf.RoundToInt(GameSettings.MusicVolume * 100f)}%";
        if (sfxValue != null) sfxValue.text = $"{Mathf.RoundToInt(GameSettings.SfxVolume * 100f)}%";
        if (mouseValue != null) mouseValue.text = $"{GameSettings.MouseSensitivity:0.0}x";
        if (stickValue != null) stickValue.text = $"{GameSettings.StickSensitivity:0.0}x";
    }

    // ---------- Visual ----------

    private void SetVisible(bool visible, bool instant)
    {
        if (group == null) return;
        group.interactable = visible;
        group.blocksRaycasts = visible;

        if (instant || !gameObject.activeInHierarchy)
        {
            group.alpha = visible ? 1f : 0f;
            return;
        }
        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(Fade(visible ? 1f : 0f));
    }

    private IEnumerator Fade(float target)
    {
        float start = group.alpha;
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(start, target, t / fadeDuration);
            yield return null;
        }
        group.alpha = target;
    }
}
