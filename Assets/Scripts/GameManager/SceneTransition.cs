using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum TransitionSide { Night, Day }

// Pantalla de carga que gira para mostrar el día o la noche.
// Se crea sola al iniciar el juego desde Resources/SceneTransition (prefab),
// así funciona aunque entres a Play desde cualquier escena.
//
// La imagen (carga.png) tiene la noche arriba y el día abajo, girado 180°.
// La pantalla solo muestra la mitad de arriba: con 0° se ve la noche y con 180° el día.
//
// Orden: escena → negro → pantalla de carga → giro → negro → nueva escena
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }
    public static bool IsTransitioning { get; private set; }

    [Header("Referencias")]
    [SerializeField] private CanvasGroup group;
    [Tooltip("La imagen carga.png que gira")]
    [SerializeField] private RectTransform wheel;

    [Tooltip("Orden del Canvas: debe ser mayor que el de cualquier otro Canvas del juego")]
    [SerializeField] private int sortingOrder = 1000;
    [SerializeField] private Color blackColor = Color.black;

    [Header("Tiempos (segundos reales)")]
    [Tooltip("La escena actual se funde a negro")]
    [SerializeField] private float sceneFadeOut = 0.6f;
    [Tooltip("Del negro aparece la pantalla de carga")]
    [SerializeField] private float loadingFadeIn = 0.6f;
    [SerializeField] private float holdBeforeSpin = 0.3f;
    [SerializeField] private float spinDuration = 1.1f;
    [SerializeField] private float holdAfterSpin = 0.4f;
    [Tooltip("La pantalla de carga se funde a negro")]
    [SerializeField] private float loadingFadeOut = 0.5f;
    [Tooltip("Del negro aparece la nueva escena")]
    [SerializeField] private float sceneFadeIn = 0.8f;
    [Tooltip("Tiempo mínimo de la pantalla de carga aunque la escena cargue rápido")]
    [SerializeField] private float minimumLoadingTime = 1.5f;

    [Header("Giro")]
    [SerializeField] private AnimationCurve spinCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Zoom durante el giro (1 = sin zoom). Tapa las esquinas mientras gira")]
    [SerializeField] private float spinScale = 1.15f;

    private TransitionSide currentSide = TransitionSide.Night;
    private float currentAngle;
    private Image black;
    private readonly List<GameObject> content = new List<GameObject>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
        IsTransitioning = false;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateInstance()
    {
        SceneTransition prefab = Resources.Load<SceneTransition>("SceneTransition");
        if (prefab == null)
        {
            Debug.LogWarning("[SceneTransition] No hay prefab en Resources/SceneTransition. Se cambiará de escena sin transición.");
            return;
        }
        Instantiate(prefab);
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

        // Siempre por encima de la UI de cualquier escena
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
        }

        // Todo lo que hay en el prefab (Fondo, Marco...) es la pantalla de carga
        foreach (Transform child in transform) content.Add(child.gameObject);
        CreateBlack();

        SetSide(currentSide);
        SetContentVisible(false);
        SetBlack(0f);
        group.alpha = 1f;
        group.blocksRaycasts = false;
    }

    // Capa negra encima de todo, creada por código
    private void CreateBlack()
    {
        var go = new GameObject("Negro", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(transform, false);
        go.transform.SetAsLastSibling();

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        black = go.GetComponent<Image>();
        black.raycastTarget = false;
    }

    // ---------- API ----------

    // Úsalo en lugar de SceneManager.LoadScene
    public static void Load(string sceneName, TransitionSide target)
    {
        if (Instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }
        if (IsTransitioning) return;

        Instance.StartCoroutine(Instance.LoadRoutine(sceneName, target));
    }

    // ---------- Flujo ----------

    private IEnumerator LoadRoutine(string sceneName, TransitionSide target)
    {
        IsTransitioning = true;
        group.blocksRaycasts = true;   // nadie hace clic mientras carga

        // 1. La escena actual se funde a negro (y su música se apaga)
        if (MusicManager.Instance != null) MusicManager.Instance.FadeOut(sceneFadeOut + loadingFadeIn);
        yield return FadeBlack(0f, 1f, sceneFadeOut);

        // 2. Empieza a cargar la nueva escena por detrás (sin activarla todavía)
        AsyncOperation load = SceneManager.LoadSceneAsync(sceneName);
        load.allowSceneActivation = false;
        float loadingStart = Time.unscaledTime;

        // 3. Del negro aparece la pantalla de carga (lado actual)
        SetContentVisible(true);
        yield return FadeBlack(1f, 0f, loadingFadeIn);
        yield return WaitReal(holdBeforeSpin);

        // 4. Gira al lado de destino
        if (target != currentSide) yield return Spin(target);

        // 5. Espera a que cargue y al tiempo mínimo
        while (load.progress < 0.9f || Time.unscaledTime - loadingStart < minimumLoadingTime)
            yield return null;

        yield return WaitReal(holdAfterSpin);

        // 6. La pantalla de carga se funde a negro
        yield return FadeBlack(0f, 1f, loadingFadeOut);
        SetContentVisible(false);

        // 7. Activa la escena y deja que haga su Start
        Time.timeScale = 1f;   // por si se quedó en pausa o en hit stop
        load.allowSceneActivation = true;
        while (!load.isDone) yield return null;
        yield return null;

        // 8. Del negro aparece la nueva escena
        yield return FadeBlack(1f, 0f, sceneFadeIn);

        group.blocksRaycasts = false;
        IsTransitioning = false;
    }

    private IEnumerator Spin(TransitionSide target)
    {
        float from = currentAngle;
        float to = currentAngle + 180f;   // siempre gira hacia el mismo lado

        for (float t = 0f; t < spinDuration; t += Time.unscaledDeltaTime)
        {
            float k = spinCurve.Evaluate(t / spinDuration);
            wheel.localEulerAngles = new Vector3(0f, 0f, Mathf.LerpUnclamped(from, to, k));

            // Zoom que sube a la mitad del giro y vuelve a 1
            float scale = Mathf.Lerp(1f, spinScale, Mathf.Sin(t / spinDuration * Mathf.PI));
            wheel.localScale = Vector3.one * scale;
            yield return null;
        }

        wheel.localScale = Vector3.one;
        SetSide(target);
    }

    private void SetSide(TransitionSide side)
    {
        currentSide = side;
        currentAngle = side == TransitionSide.Night ? 0f : 180f;
        wheel.localEulerAngles = new Vector3(0f, 0f, currentAngle);
    }

    private void SetContentVisible(bool visible)
    {
        foreach (GameObject go in content) go.SetActive(visible);
    }

    private void SetBlack(float alpha)
    {
        Color c = blackColor;
        c.a = alpha;
        black.color = c;
    }

    private IEnumerator FadeBlack(float from, float to, float duration)
    {
        for (float t = 0f; t < duration; t += Time.unscaledDeltaTime)
        {
            // Suavizado para que no se sienta lineal
            SetBlack(Mathf.SmoothStep(from, to, t / duration));
            yield return null;
        }
        SetBlack(to);
    }

    private static IEnumerator WaitReal(float seconds)
    {
        if (seconds > 0f) yield return new WaitForSecondsRealtime(seconds);
    }
}
