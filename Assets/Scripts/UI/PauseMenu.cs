using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú de pausa del día y de la noche. Se abre con Esc o Start del control.
// Se crea solo desde Resources/PauseMenu y no se destruye al cambiar de escena.
// Crea el prefab con: Tools → UI → Crear prefab Menú de Pausa
public class PauseMenu : MonoBehaviour
{
    public static PauseMenu Instance { get; private set; }
    public static bool IsOpen { get; private set; }

    [Header("Dónde se puede pausar")]
    [SerializeField] private string[] scenesWithPause = { "Oxxo", "NivelHackAndSlash" };

    [Header("Referencias")]
    [SerializeField] private CanvasGroup group;
    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    [Header("Volumen (Audio Mixer con MusicVolume y SFXVolume expuestos)")]
    [SerializeField] private AudioMixer mixer;

    [Header("Comportamiento")]
    [Tooltip("Pausa los sonidos del juego (aleteos, voces). La música y la UI siguen sonando")]
    [SerializeField] private bool pauseGameAudio = true;
    [Tooltip("Escena a la que va 'Menú principal'")]
    [SerializeField] private string mainMenuScene = "Portada";
    [SerializeField] private float fadeDuration = 0.2f;

    private CursorLockMode savedLock;
    private bool savedVisible;
    private Coroutine fadeRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        IsOpen = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        PauseMenu prefab = Resources.Load<PauseMenu>("PauseMenu");
        if (prefab != null) Instantiate(prefab);
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
        SetVisible(false, instant: true);
    }

    private void Start()
    {
        // El mixer no acepta cambios en Awake, por eso se aplica aquí.
        // Como este menú existe en todas las escenas, también aplica el volumen guardado al iniciar.
        GameSettings.RegisterMixer(mixer);

        if (musicSlider != null) musicSlider.onValueChanged.AddListener(v => GameSettings.MusicVolume = v);
        if (sfxSlider != null) sfxSlider.onValueChanged.AddListener(v => GameSettings.SfxVolume = v);
        RefreshSliders();
    }

    // ---------- Abrir / cerrar ----------

    private void Update()
    {
        if (TogglePressed())
        {
            if (IsOpen) Resume();
            else if (CanPause()) Open();
        }

        // Por si algo (ej. el hit stop) cambió el tiempo mientras está en pausa
        if (IsOpen && Time.timeScale != 0f) Time.timeScale = 0f;
    }

    private static bool TogglePressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad g = Gamepad.current;
        return (k != null && k.escapeKey.wasPressedThisFrame) ||
               (g != null && g.startButton.wasPressedThisFrame);
    }

    private bool CanPause()
    {
        if (SceneTransition.IsTransitioning) return false;
        if (TutorialManager.Active != null) return false;   // Esc salta el tutorial
        if (MenuPause.IsPaused) return false;               // hay una tienda abierta

        string scene = SceneManager.GetActiveScene().name;
        return System.Array.IndexOf(scenesWithPause, scene) >= 0;
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;

        savedLock = Cursor.lockState;
        savedVisible = Cursor.visible;

        Time.timeScale = 0f;
        SetPlayerInput(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (pauseGameAudio) AudioListener.pause = true;

        RefreshSliders();   // por si se cambió el volumen en Opciones
        SetVisible(true, instant: false);
    }

    // Botón Continuar
    public void Resume()
    {
        if (!IsOpen) return;
        IsOpen = false;

        Time.timeScale = 1f;
        SetPlayerInput(true);
        Cursor.lockState = savedLock;
        Cursor.visible = savedVisible;
        AudioListener.pause = false;

        SetVisible(false, instant: false);
    }

    // Botón Salir del juego
    public void QuitGame()
    {
        PlayerPrefs.Save();
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // Botón opcional: volver a la portada (empieza una partida nueva)
    public void GoToMainMenu()
    {
        Resume();
        if (GameManager.Instance != null) Destroy(GameManager.Instance.gameObject);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        SceneTransition.Load(mainMenuScene, TransitionSide.Night);
    }

    private static void SetPlayerInput(bool enabled)
    {
        var map = InputSystem.actions != null ? InputSystem.actions.FindActionMap("Player") : null;
        if (map == null) return;
        if (enabled) map.Enable(); else map.Disable();
    }

    // ---------- Volumen ----------

    private void RefreshSliders()
    {
        if (musicSlider != null) musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
        if (sfxSlider != null) sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
    }

    // ---------- Visual ----------

    private void SetVisible(bool visible, bool instant)
    {
        if (group == null) return;
        group.interactable = visible;
        group.blocksRaycasts = visible;

        if (instant)
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
