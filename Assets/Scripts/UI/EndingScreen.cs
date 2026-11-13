using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pantalla de finales. Muestra el final que eligió el GameManager.
// Crea la escena con: Tools → Escenas → Crear escena de Finales
public class EndingScreen : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private RawImage image;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private CanvasGroup textGroup;
    [SerializeField] private CanvasGroup buttonsGroup;

    [Header("Finales (1, 2, 3)")]
    [SerializeField] private Texture[] endingImages = new Texture[3];
    [Tooltip("Opcional: título de cada final")]
    [SerializeField] private string[] titles = { "", "", "" };
    [Tooltip("Opcional: texto corto de cada final")]
    [TextArea(2, 4)][SerializeField] private string[] texts = { "", "", "" };
    [Tooltip("Opcional: canción de cada final")]
    [SerializeField] private AudioClip[] music = new AudioClip[3];

    [Header("Tiempos")]
    [SerializeField] private float imageFadeIn = 2f;
    [SerializeField] private float textDelay = 1.5f;
    [SerializeField] private float buttonsDelay = 4f;
    [Tooltip("Zoom lento sobre la imagen (1 = sin zoom)")]
    [SerializeField] private float slowZoom = 1.06f;
    [SerializeField] private float zoomDuration = 20f;

    [Header("Prueba")]
    [Tooltip("Final que se muestra si abres la escena directamente (sin GameManager)")]
    [SerializeField] private EndingType previewEnding = EndingType.Good;
    [SerializeField] private string mainMenuScene = "Portada";

    private void Start()
    {
        EndingType ending = GameManager.Instance != null && GameManager.Instance.Ending != EndingType.None
            ? GameManager.Instance.Ending
            : previewEnding;
        int index = Mathf.Clamp((int)ending - 1, 0, 2);

        if (image != null && index < endingImages.Length) image.texture = endingImages[index];
        SetText(titleText, index < titles.Length ? titles[index] : "");
        SetText(bodyText, index < texts.Length ? texts[index] : "");

        if (index < music.Length && music[index] != null && MusicManager.Instance != null)
            MusicManager.Instance.Play(music[index], 1f, 3f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;

        StartCoroutine(Sequence());
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text == null) return;
        text.text = value;
        text.gameObject.SetActive(!string.IsNullOrEmpty(value));
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
        yield return FadeGroup(textGroup, 1f);
        yield return new WaitForSeconds(Mathf.Max(0f, buttonsDelay - textDelay));
        yield return FadeGroup(buttonsGroup, 1f);
        SetGroup(buttonsGroup, 1f, true);
    }

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
