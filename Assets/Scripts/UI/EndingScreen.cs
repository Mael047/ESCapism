#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pantalla de finales. Muestra el final que eligió el GameManager.
// Crea la escena con: Tools → Escenas → Crear escena de Finales
public class EndingScreen : MonoBehaviour
{
    [System.Serializable]
    public class EndingLines
    {
        [TextArea(2, 4)] public string[] lines;
    }

    [Header("Referencias")]
    [SerializeField] private RawImage image;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private CanvasGroup textGroup;
    [SerializeField] private CanvasGroup buttonsGroup;

    [Header("Frases con clic (fuera de textGroup y buttonsGroup)")]
    [SerializeField] private TMP_Text phraseText;
    [SerializeField] private TMP_Text continueHint;
    [SerializeField] private string continueMessage = "- clic para continuar -";

    [Header("Finales (1, 2, 3)")]
    [SerializeField] private Texture[] endingImages = new Texture[3];
    [Tooltip("Opcional: título de cada final")]
    [SerializeField] private string[] titles = { "", "", "" };
    [Tooltip("Opcional: subtítulo que sale junto al título")]
    [TextArea(2, 4)][SerializeField] private string[] texts = { "", "", "" };
    [Tooltip("Frases que se leen una por una, avanzando con clic")]
    [SerializeField] private EndingLines[] phrases = new EndingLines[3];
    [Tooltip("Opcional: canción de cada final")]
    [SerializeField] private AudioClip[] music = new AudioClip[3];

    [Header("Tiempos")]
    [SerializeField] private float imageFadeIn = 2f;
    [SerializeField] private float textDelay = 1.5f;
    [SerializeField] private float buttonsDelay = 4f;
    [Tooltip("Letras por segundo (0 = la frase aparece completa)")]
    [SerializeField] private float typeSpeed = 40f;
    [SerializeField] private float lineFadeOut = 0.25f;
    [Tooltip("Zoom lento sobre la imagen (1 = sin zoom)")]
    [SerializeField] private float slowZoom = 1.06f;
    [SerializeField] private float zoomDuration = 20f;

    [Header("Prueba")]
    [Tooltip("Final que se muestra si abres la escena directamente (sin GameManager)")]
    [SerializeField] private EndingType previewEnding = EndingType.Good;
    [SerializeField] private string mainMenuScene = "Portada";

    private int endingIndex;
    private bool hintActive;

    private void Start()
    {
        EndingType ending = GameManager.Instance != null && GameManager.Instance.Ending != EndingType.None
            ? GameManager.Instance.Ending
            : previewEnding;
        endingIndex = Mathf.Clamp((int)ending - 1, 0, 2);
        int index = endingIndex;

        if (image != null && index < endingImages.Length) image.texture = endingImages[index];
        SetText(titleText, index < titles.Length ? titles[index] : "");
        SetText(bodyText, index < texts.Length ? texts[index] : "");

        if (phraseText != null) { phraseText.text = ""; phraseText.alpha = 0f; }
        if (continueHint != null) { continueHint.text = continueMessage; continueHint.alpha = 0f; }

        if (index < music.Length && music[index] != null && MusicManager.Instance != null)
            MusicManager.Instance.Play(music[index], 1f, 3f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        StartCoroutine(Sequence());
    }

    private void Update()
    {
        // Parpadeo suave del "clic para continuar"
        if (continueHint == null) return;
        continueHint.alpha = hintActive
            ? Mathf.Lerp(0.25f, 1f, (Mathf.Sin(Time.time * 3f) + 1f) * 0.5f)
            : 0f;
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text == null) return;
        text.text = value;
        text.gameObject.SetActive(!string.IsNullOrEmpty(value));
    }

    private static bool Pressed()
{
#if ENABLE_INPUT_SYSTEM
    return (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        || (Keyboard.current != null &&
            (Keyboard.current.spaceKey.wasPressedThisFrame ||
             Keyboard.current.enterKey.wasPressedThisFrame));
#else
    return Input.GetMouseButtonDown(0)
        || Input.GetKeyDown(KeyCode.Space)
        || Input.GetKeyDown(KeyCode.Return);
#endif
}

    private IEnumerator Sequence()
    {
        SetGroup(textGroup, 0f, false);
        SetGroup(buttonsGroup, 0f, false);
        if (image != null) image.color = new Color(1f, 1f, 1f, 0f);

        StartCoroutine(Zoom());

        // Espera a que termine la pantalla de carga
        while (SceneTransition.IsTransitioning) yield return null;

        yield return FadeImage(imageFadeIn);
        yield return new WaitForSeconds(textDelay);

        yield return ShowPhrases();

        yield return FadeGroup(textGroup, 1f);
        yield return new WaitForSeconds(Mathf.Max(0f, buttonsDelay - textDelay));
        yield return FadeGroup(buttonsGroup, 1f);
        SetGroup(buttonsGroup, 1f, true);
    }

    // ---------- Frases con clic ----------

    private IEnumerator ShowPhrases()
    {
        if (phraseText == null || endingIndex >= phrases.Length) yield break;
        EndingLines data = phrases[endingIndex];
        if (data == null || data.lines == null) yield break;

        foreach (string line in data.lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            hintActive = false;
            phraseText.text = line;
            phraseText.alpha = 1f;
            phraseText.ForceMeshUpdate();
            int total = phraseText.textInfo.characterCount;

            // Máquina de escribir (un clic la completa)
            if (typeSpeed > 0f)
            {
                float shown = 0f;
                phraseText.maxVisibleCharacters = 0;
                while (shown < total)
                {
                    if (Pressed()) break;
                    shown += typeSpeed * Time.deltaTime;
                    phraseText.maxVisibleCharacters = Mathf.FloorToInt(shown);
                    yield return null;
                }
                yield return null; // evita contar el mismo clic dos veces
            }
            phraseText.maxVisibleCharacters = total;

            // Espera el clic para seguir
            hintActive = true;
            while (!Pressed()) yield return null;
            hintActive = false;

            // Desvanece la frase antes de la siguiente
            for (float t = 0f; t < lineFadeOut; t += Time.deltaTime)
            {
                phraseText.alpha = 1f - t / lineFadeOut;
                yield return null;
            }
            phraseText.alpha = 0f;
        }

        phraseText.text = "";
    }

    // ---------- Imagen y fades ----------

    private IEnumerator FadeImage(float duration)
    {
        if (image == null) yield break;
        for (float t = 0f; t < duration; t += Time.deltaTime)
        {
            image.color = new Color(1f, 1f, 1f, t / duration);
            yield return null;
        }
        image.color = Color.white;
    }

    private IEnumerator Zoom()
    {
        if (image == null || slowZoom == 1f) yield break;
        for (float t = 0f; t < zoomDuration; t += Time.deltaTime)
        {
            float s = Mathf.Lerp(1f, slowZoom, Mathf.SmoothStep(0f, 1f, t / zoomDuration));
            image.rectTransform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    private static IEnumerator FadeGroup(CanvasGroup group, float target)
    {
        if (group == null) yield break;
        float start = group.alpha;
        for (float t = 0f; t < 0.8f; t += Time.deltaTime)
        {
            group.alpha = Mathf.Lerp(start, target, t / 0.8f);
            yield return null;
        }
        group.alpha = target;
    }

    private static void SetGroup(CanvasGroup group, float alpha, bool interactable)
    {
        if (group == null) return;
        group.alpha = alpha;
        group.interactable = interactable;
        group.blocksRaycasts = interactable;
    }

    // ---------- Botones ----------

    // Vuelve a la portada y deja todo listo para una partida nueva
    public void BackToMenu()
    {
        if (GameManager.Instance != null) GameManager.Instance.ResetRun();
        SceneTransition.Load(mainMenuScene, TransitionSide.Night);
    }

    public void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}