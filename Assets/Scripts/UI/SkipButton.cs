using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Botón "Omitir". Sirve para el tutorial, el contexto o cualquier parte que se pueda saltar.
// También se activa con Esc o con Start del control.
// - En el tutorial: asigna el prefab en "Skip Button Prefab" del TutorialManager (él conecta la acción).
// - En otras escenas: conecta lo que debe pasar en "On Skip" o en el On Click del Button.
[RequireComponent(typeof(Button))]
public class SkipButton : MonoBehaviour
{
    [SerializeField] private bool keyboardShortcut = true;
    [Tooltip("Segundos antes de que aparezca el botón")]
    [SerializeField] private float appearDelay = 1f;
    [SerializeField] private float fadeDuration = 0.4f;

    [Tooltip("Lo que pasa al omitir (además del On Click del Button)")]
    public UnityEvent onSkip;

    private Button button;
    private CanvasGroup group;
    private bool used;

    private void Awake()
    {
        button = GetComponent<Button>();
        group = GetComponent<CanvasGroup>();
        if (group == null) group = gameObject.AddComponent<CanvasGroup>();

        button.onClick.AddListener(OnClicked);
    }

    private void OnEnable()
    {
        used = false;
        StartCoroutine(Appear());
    }

    private IEnumerator Appear()
    {
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;

        if (appearDelay > 0f) yield return new WaitForSecondsRealtime(appearDelay);

        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            group.alpha = t / fadeDuration;
            yield return null;
        }
        group.alpha = 1f;
        group.interactable = true;
        group.blocksRaycasts = true;
    }

    private void Update()
    {
        if (!keyboardShortcut || used || !group.interactable) return;

        Keyboard k = Keyboard.current;
        Gamepad g = Gamepad.current;
        bool pressed = (k != null && k.escapeKey.wasPressedThisFrame) ||
                       (g != null && g.startButton.wasPressedThisFrame);

        // Invoca el On Click para que también funcionen los listeners de otros scripts
        if (pressed) button.onClick.Invoke();
    }

    private void OnClicked()
    {
        if (used) return;
        used = true;
        group.interactable = false;
        onSkip?.Invoke();
    }
}
