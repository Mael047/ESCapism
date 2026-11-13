using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Tutorial por pasos: oscurece la pantalla, resalta un elemento y muestra una tarjeta.
// Los pasos se arman en la lista "Steps" del Inspector. Toda la UI la crea este script.
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Active { get; private set; }

    [Header("Pasos")]
    [SerializeField] private List<TutorialStep> steps = new List<TutorialStep>();

    [Header("Inicio")]
    [Tooltip("Nombre único para recordar si ya se vio (ej. Dia, Noche)")]
    [SerializeField] private string tutorialId = "Dia";
    [SerializeField] private bool playOnStart = true;
    [Tooltip("Segundos después de que termina la pantalla de carga")]
    [SerializeField] private float startDelay = 0.5f;
    [Tooltip("Solo se muestra la primera vez que se juega")]
    [SerializeField] private bool onlyFirstTime = true;
    [Tooltip("En el editor siempre se muestra, aunque ya se haya visto (para probar)")]
    [SerializeField] private bool alwaysShowInEditor = true;
    [Tooltip("Solo sale en el día 1 (ciclo 1) y en su noche, no en el día 2")]
    [SerializeField] private bool onlyFirstDay = true;
    [Tooltip("Al saltar este tutorial también se saltan los demás de la partida (ej. el de la noche)")]
    [SerializeField] private bool skipAllTutorials = true;
    [Tooltip("Congela el juego mientras se lee")]
    [SerializeField] private bool pauseGame = true;

    [Header("Tarjeta")]
    [Tooltip("Opcional: tu propia tarjeta (prefab con TutorialCard). Vacío = tarjeta por defecto")]
    [SerializeField] private TutorialCard cardPrefab;
    [Tooltip("Opcional: tu propio botón de Saltar (prefab con Button). Vacío = botón por defecto arriba a la derecha")]
    [SerializeField] private Button skipButtonPrefab;

    [Header("Estilo (tarjeta por defecto, velo, borde y botón Saltar)")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private Color veilColor = new Color(0f, 0f, 0f, 0.6f);
    [SerializeField] private Color pink = new Color(0.953f, 0.196f, 0.514f);        // #F33283
    [SerializeField] private Color keyColor = new Color(1f, 0.502f, 0.706f);        // #FF80B4
    [SerializeField] private Color cardColor = new Color(0.07f, 0.04f, 0.09f, 0.95f);
    [SerializeField] private Color textColor = Color.white;
    [Tooltip("Opcional: fondo de la tarjeta (sprite con bordes en modo Sliced)")]
    [SerializeField] private Sprite cardSprite;
    [Tooltip("Opcional: flecha apuntando HACIA ARRIBA. Sin sprite no hay flecha")]
    [SerializeField] private Sprite arrowSprite;
    [SerializeField] private float cardWidth = 560f;
    [SerializeField] private float titleSize = 40f;
    [SerializeField] private float textSize = 28f;
    [SerializeField] private float borderThickness = 4f;
    [SerializeField] private int sortingOrder = 500;

    [Header("Animación")]
    [SerializeField] private float cardGap = 50f;
    [SerializeField] private float holeSpeed = 12f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float pulseAmount = 0.03f;
    [SerializeField] private float fadeDuration = 0.3f;

    [Header("Eventos")]
    public UnityEvent onFinished;

    // true cuando terminó, se saltó o no tenía que mostrarse
    public bool Finished { get; private set; }

    private int index = -1;
    private bool finishing;
    private TutorialStep Current => index >= 0 && index < steps.Count ? steps[index] : null;

    // UI
    private RectTransform root;
    private CanvasGroup group;
    private RectTransform veilTop, veilBottom, veilLeft, veilRight, holeBlocker;
    private RectTransform border, arrow, card;
    private TutorialCard cardUI;

    private Rect hole;
    private bool holeInitialized;

    // Pausa
    private bool paused;
    private float savedTimeScale = 1f;
    private CursorLockMode savedLock;
    private bool savedVisible;

    private string SaveKey => "Tutorial_" + tutorialId;

    // ---------- Inicio ----------

    private void Awake()
    {
        bool alreadySeen = onlyFirstTime && PlayerPrefs.GetInt(SaveKey, 0) == 1;
#if UNITY_EDITOR
        if (alwaysShowInEditor) alreadySeen = false;
#endif
        GameManager gm = GameManager.Instance;
        bool notFirstDay = onlyFirstDay && gm != null && gm.CurrentCycle > 1;
        bool skippedBefore = gm != null && gm.Data.tutorialSkipped;

        // Si no se va a mostrar solo, cuenta como terminado (para que la noche no se quede esperando)
        if (steps.Count == 0 || alreadySeen || notFirstDay || skippedBefore || !playOnStart) Finished = true;
    }

    private IEnumerator Start()
    {
        if (!playOnStart || Finished) yield break;

        while (SceneTransition.IsTransitioning) yield return null;
        yield return new WaitForSecondsRealtime(startDelay);
        Play();
    }

    public void Play()
    {
        if (Active != null || steps.Count == 0) return;

        Active = this;
        Finished = false;
        finishing = false;
        if (root == null) BuildUI();
        root.gameObject.SetActive(true);

        if (pauseGame) Pause(true);
        StartCoroutine(FadeGroup(0f, 1f));
        GoTo(0);
    }

    // ---------- Pasos ----------

    public void Next()
    {
        if (Active != this || finishing) return;
        if (index + 1 >= steps.Count) Finish();
        else GoTo(index + 1);
    }

    // Salta el tutorial entero (botón "Saltar tutorial" o Esc)
    public void Skip()
    {
        if (Active != this || finishing) return;

        if (skipAllTutorials && GameManager.Instance != null)
            GameManager.Instance.Data.tutorialSkipped = true;
        Finish();
    }

    // Llamar desde el código del juego cuando el jugador hace la acción del paso
    public static void Notify(string actionId)
    {
        TutorialStep step = Active != null ? Active.Current : null;
        if (step != null && step.advance == TutorialAdvance.Action && step.actionId == actionId)
            Active.Next();
    }

    private void GoTo(int newIndex)
    {
        index = newIndex;
        TutorialStep step = Current;

        step.onStepStart?.Invoke();

        if (cardUI.counter != null) cardUI.counter.text = $"Paso {index + 1} de {steps.Count}";
        if (cardUI.title != null) cardUI.title.text = cardUI.uppercaseTitle ? step.title.ToUpper() : step.title;
        if (cardUI.body != null) cardUI.body.text = FormatKeys(step.text);
        cardUI.SetProgress((index + 1f) / steps.Count);

        bool waitsForAction = step.advance == TutorialAdvance.Action;
        if (cardUI.nextButton != null) cardUI.nextButton.gameObject.SetActive(!waitsForAction);

        // En pasos de acción se puede tocar el elemento resaltado y el juego corre
        holeBlocker.GetComponent<Image>().raycastTarget = !waitsForAction;
        if (pauseGame) Pause(!waitsForAction);

        LayoutRebuilder.ForceRebuildLayoutImmediate(card);
    }

    private void Finish()
    {
        finishing = true;
        if (onlyFirstTime) PlayerPrefs.SetInt(SaveKey, 1);
        index = -1;
        StartCoroutine(FinishRoutine());
    }

    private IEnumerator FinishRoutine()
    {
        yield return FadeGroup(1f, 0f);
        root.gameObject.SetActive(false);
        if (paused) Pause(false);

        Active = null;
        Finished = true;
        onFinished?.Invoke();
    }

    // {E} → E en rosa
    private string FormatKeys(string text)
    {
        string hex = ColorUtility.ToHtmlStringRGB(keyColor);
        return Regex.Replace(text, @"\{([^}]+)\}", $"<color=#{hex}>$1</color>");
    }

    // ---------- Pausa ----------

    private void Pause(bool pause)
    {
        if (pause == paused) return;
        paused = pause;
        var playerMap = InputSystem.actions != null ? InputSystem.actions.FindActionMap("Player") : null;

        if (pause)
        {
            savedTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            savedLock = Cursor.lockState;
            savedVisible = Cursor.visible;

            Time.timeScale = 0f;
            playerMap?.Disable();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Time.timeScale = savedTimeScale;
            playerMap?.Enable();
            Cursor.lockState = savedLock;
            Cursor.visible = savedVisible;
        }
    }

    // ---------- Cada frame ----------

    private void Update()
    {
        if (Active != this || Current == null) return;

        if (SkipPressed()) { Skip(); return; }
        if (Current.advance == TutorialAdvance.NextButton && NextPressed()) Next();
        if (Current == null) return;

        UpdateLayout(Current);
    }

    private static bool NextPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad g = Gamepad.current;
        return (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame)) ||
               (g != null && g.buttonSouth.wasPressedThisFrame);
    }

    private static bool SkipPressed()
    {
        Keyboard k = Keyboard.current;
        Gamepad g = Gamepad.current;
        return (k != null && k.escapeKey.wasPressedThisFrame) ||
               (g != null && g.startButton.wasPressedThisFrame);
    }

    private void UpdateLayout(TutorialStep step)
    {
        Rect area = root.rect;
        bool hasTarget = TryGetTargetRect(step, out Rect targetRect);

        if (!hasTarget) targetRect = new Rect(0f, 0f, 0f, 0f);   // hueco vacío al centro

        // El hueco se desliza suavemente de un elemento al siguiente
        if (!holeInitialized) { hole = targetRect; holeInitialized = true; }
        float k = 1f - Mathf.Exp(-holeSpeed * Time.unscaledDeltaTime);
        hole = new Rect(Vector2.Lerp(hole.position, targetRect.position, k),
                        Vector2.Lerp(hole.size, targetRect.size, k));

        // Velo alrededor del hueco
        SetRect(veilTop, area.xMin, hole.yMax, area.xMax, area.yMax);
        SetRect(veilBottom, area.xMin, area.yMin, area.xMax, hole.yMin);
        SetRect(veilLeft, area.xMin, hole.yMin, hole.xMin, hole.yMax);
        SetRect(veilRight, hole.xMax, hole.yMin, area.xMax, hole.yMax);
        SetRect(holeBlocker, hole.xMin, hole.yMin, hole.xMax, hole.yMax);

        // Borde que late
        border.gameObject.SetActive(hasTarget);
        border.anchoredPosition = hole.center;
        border.sizeDelta = hole.size;
        float pulse = 1f + pulseAmount * 0.5f * (1f + Mathf.Sin(Time.unscaledTime * pulseSpeed));
        border.localScale = new Vector3(pulse, pulse, 1f);

        // Tarjeta
        TutorialCardPosition side = hasTarget ? step.cardPosition : TutorialCardPosition.Center;
        if (side == TutorialCardPosition.Auto) side = PickSide(targetRect, area, card.rect.size);

        Vector2 size = card.rect.size;
        Vector2 pos = side switch
        {
            TutorialCardPosition.Below => new Vector2(hole.center.x, hole.yMin - cardGap - size.y / 2f),
            TutorialCardPosition.Above => new Vector2(hole.center.x, hole.yMax + cardGap + size.y / 2f),
            TutorialCardPosition.Left  => new Vector2(hole.xMin - cardGap - size.x / 2f, hole.center.y),
            TutorialCardPosition.Right => new Vector2(hole.xMax + cardGap + size.x / 2f, hole.center.y),
            _ => Vector2.zero
        };
        pos += step.cardOffset;

        const float margin = 20f;
        pos.x = Mathf.Clamp(pos.x, area.xMin + size.x / 2f + margin, area.xMax - size.x / 2f - margin);
        pos.y = Mathf.Clamp(pos.y, area.yMin + size.y / 2f + margin, area.yMax - size.y / 2f - margin);
        card.anchoredPosition = pos;

        // Flecha entre la tarjeta y el elemento
        bool showArrow = arrow != null && step.showArrow && hasTarget && side != TutorialCardPosition.Center;
        if (arrow != null) arrow.gameObject.SetActive(showArrow);
        if (showArrow)
        {
            float bob = Mathf.Sin(Time.unscaledTime * pulseSpeed * 1.5f) * 6f;
            float half = cardGap / 2f;
            switch (side)
            {
                case TutorialCardPosition.Below:   // tarjeta abajo, flecha apunta arriba
                    arrow.anchoredPosition = new Vector2(hole.center.x, hole.yMin - half - bob);
                    arrow.localEulerAngles = new Vector3(0f, 0f, 0f); break;
                case TutorialCardPosition.Above:
                    arrow.anchoredPosition = new Vector2(hole.center.x, hole.yMax + half + bob);
                    arrow.localEulerAngles = new Vector3(0f, 0f, 180f); break;
                case TutorialCardPosition.Left:
                    arrow.anchoredPosition = new Vector2(hole.xMin - half - bob, hole.center.y);
                    arrow.localEulerAngles = new Vector3(0f, 0f, -90f); break;
                case TutorialCardPosition.Right:
                    arrow.anchoredPosition = new Vector2(hole.xMax + half + bob, hole.center.y);
                    arrow.localEulerAngles = new Vector3(0f, 0f, 90f); break;
            }
        }
    }

    private TutorialCardPosition PickSide(Rect target, Rect area, Vector2 cardSize)
    {
        float below = target.yMin - area.yMin;
        float above = area.yMax - target.yMax;
        float left = target.xMin - area.xMin;
        float right = area.xMax - target.xMax;
        float needH = cardSize.y + cardGap + 20f;
        float needW = cardSize.x + cardGap + 20f;

        if (below >= needH || above >= needH) return below >= above ? TutorialCardPosition.Below : TutorialCardPosition.Above;
        if (left >= needW || right >= needW) return right >= left ? TutorialCardPosition.Right : TutorialCardPosition.Left;
        return TutorialCardPosition.Center;
    }

    // ---------- Posición del elemento en pantalla ----------

    private bool TryGetTargetRect(TutorialStep step, out Rect rect)
    {
        rect = default;
        bool any = false;

        // Target + extras: el hueco es el rectángulo que envuelve a todos
        if (TryGetRectOf(step.target, out Rect r)) { rect = r; any = true; }
        if (step.extraTargets != null)
        {
            foreach (Transform extra in step.extraTargets)
            {
                if (!TryGetRectOf(extra, out r)) continue;
                rect = any ? Rect.MinMaxRect(Mathf.Min(rect.xMin, r.xMin), Mathf.Min(rect.yMin, r.yMin),
                                             Mathf.Max(rect.xMax, r.xMax), Mathf.Max(rect.yMax, r.yMax))
                           : r;
                any = true;
            }
        }
        if (!any) return false;

        rect = Rect.MinMaxRect(rect.xMin - step.padding.x, rect.yMin - step.padding.y,
                               rect.xMax + step.padding.x, rect.yMax + step.padding.y);
        return true;
    }

    // Rectángulo de un objeto en coordenadas de este Canvas
    private bool TryGetRectOf(Transform target, out Rect rect)
    {
        rect = default;
        if (target == null || !target.gameObject.activeInHierarchy) return false;

        var points = new List<Vector2>(8);

        if (target is RectTransform rt)
        {
            // UI: funciona con Canvas Overlay y Canvas en modo cámara
            Canvas canvas = rt.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.rootCanvas.worldCamera;

            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            foreach (Vector3 c in corners) points.Add(RectTransformUtility.WorldToScreenPoint(cam, c));
        }
        else
        {
            // Objeto del mundo: usa el tamaño de su Renderer o Collider
            if (!TryGetBounds(target, out Bounds b) || Camera.main == null) return false;
            Vector3 min = b.min, max = b.max;
            for (int i = 0; i < 8; i++)
            {
                Vector3 corner = new Vector3((i & 1) == 0 ? min.x : max.x,
                                             (i & 2) == 0 ? min.y : max.y,
                                             (i & 4) == 0 ? min.z : max.z);
                Vector3 screen = Camera.main.WorldToScreenPoint(corner);
                if (screen.z < 0f) return false;   // detrás de la cámara
                points.Add(screen);
            }
        }

        // Pantalla → coordenadas de este Canvas
        Vector2 lo = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 hi = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector2 p in points)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(root, p, null, out Vector2 local);
            lo = Vector2.Min(lo, local);
            hi = Vector2.Max(hi, local);
        }

        rect = Rect.MinMaxRect(lo.x, lo.y, hi.x, hi.y);
        return true;
    }

    private static bool TryGetBounds(Transform t, out Bounds bounds)
    {
        bounds = default;
        Renderer r = t.GetComponentInChildren<Renderer>();
        if (r != null) { bounds = r.bounds; return true; }
        Collider c = t.GetComponentInChildren<Collider>();
        if (c != null) { bounds = c.bounds; return true; }
        Collider2D c2 = t.GetComponentInChildren<Collider2D>();
        if (c2 != null) { bounds = c2.bounds; return true; }
        return false;
    }

    // ---------- Construcción de la UI ----------

    private void BuildUI()
    {
        var canvasGO = new GameObject("TutorialCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        group = canvasGO.GetComponent<CanvasGroup>();
        root = (RectTransform)canvasGO.transform;

        // Velo
        veilTop = NewImage("VeloArriba", root, veilColor).rectTransform;
        veilBottom = NewImage("VeloAbajo", root, veilColor).rectTransform;
        veilLeft = NewImage("VeloIzq", root, veilColor).rectTransform;
        veilRight = NewImage("VeloDer", root, veilColor).rectTransform;
        holeBlocker = NewImage("BloqueoHueco", root, Color.clear).rectTransform;
        foreach (RectTransform v in new[] { veilTop, veilBottom, veilLeft, veilRight, holeBlocker })
        {
            v.anchorMin = v.anchorMax = new Vector2(0.5f, 0.5f);
            v.pivot = Vector2.zero;
        }

        // Borde rosa (4 líneas)
        border = NewRect("Borde", root);
        border.anchorMin = border.anchorMax = new Vector2(0.5f, 0.5f);
        BorderLine(border, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, borderThickness));
        BorderLine(border, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, borderThickness));
        BorderLine(border, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(borderThickness, 0f));
        BorderLine(border, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(borderThickness, 0f));

        // Flecha
        if (arrowSprite != null)
        {
            Image a = NewImage("Flecha", root, pink);
            a.sprite = arrowSprite;
            a.preserveAspect = true;
            arrow = a.rectTransform;
            arrow.anchorMin = arrow.anchorMax = new Vector2(0.5f, 0.5f);
            arrow.sizeDelta = new Vector2(40f, 40f);
        }

        if (cardPrefab != null)
        {
            cardUI = Instantiate(cardPrefab, root);
            if (cardUI.nextButton != null) cardUI.nextButton.onClick.AddListener(Next);
        }
        else
        {
            cardUI = BuildCard();
        }

        // La tarjeta se posiciona desde su centro
        card = cardUI.Rect;
        card.anchorMin = card.anchorMax = card.pivot = new Vector2(0.5f, 0.5f);

        BuildSkipButton();

        root.gameObject.SetActive(false);
    }

    private TutorialCard BuildCard()
    {
        Image bg = NewImage("Tarjeta", root, cardColor);
        bg.raycastTarget = true;
        if (cardSprite != null) { bg.sprite = cardSprite; bg.type = Image.Type.Sliced; }
        RectTransform card = bg.rectTransform;
        TutorialCard ui = card.gameObject.AddComponent<TutorialCard>();
        card.anchorMin = card.anchorMax = new Vector2(0.5f, 0.5f);
        card.pivot = new Vector2(0.5f, 0.5f);

        var layout = card.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 22, 22);
        layout.spacing = 10f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = card.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        card.sizeDelta = new Vector2(cardWidth, 0f);

        // Borde rosa de la tarjeta
        var outline = card.gameObject.AddComponent<Outline>();
        outline.effectColor = pink;
        outline.effectDistance = new Vector2(3f, -3f);

        ui.counter = NewText("Contador", card, textSize * 0.65f, keyColor);

        // Barra de progreso
        Image barBg = NewImage("Progreso", card, new Color(1f, 1f, 1f, 0.15f));
        barBg.gameObject.AddComponent<LayoutElement>().preferredHeight = 6f;
        Image fill = NewImage("Relleno", barBg.rectTransform, pink);
        ui.progressFill = fill.rectTransform;
        ui.progressFill.anchorMin = Vector2.zero;
        ui.progressFill.anchorMax = new Vector2(0.3f, 1f);
        ui.progressFill.offsetMin = ui.progressFill.offsetMax = Vector2.zero;

        ui.title = NewText("Titulo", card, titleSize, pink);
        ui.title.fontStyle = FontStyles.Bold;
        ui.title.text = "TÍTULO DEL PASO";
        ui.body = NewText("Texto", card, textSize, textColor);
        ui.body.text = FormatKeys("Texto de ejemplo con una tecla {E} en rosa.");
        ui.counter.text = "Paso 1 de 3";

        // Fila del botón Siguiente (a la derecha)
        RectTransform row = NewRect("FilaBoton", card);
        var rowLayout = row.gameObject.AddComponent<HorizontalLayoutGroup>();
        rowLayout.childAlignment = TextAnchor.MiddleRight;
        rowLayout.childControlWidth = rowLayout.childControlHeight = true;
        rowLayout.childForceExpandWidth = false;
        ui.nextButton = NewButton("Siguiente", row, "Siguiente  >", pink, Color.white, Next).GetComponent<Button>();
        return ui;
    }

    private void BuildSkipButton()
    {
        if (skipButtonPrefab != null)
        {
            Button custom = Instantiate(skipButtonPrefab, root);
            custom.onClick.AddListener(Skip);
            return;
        }

        RectTransform skip = NewButton("Saltar", root, "Saltar tutorial", new Color(0f, 0f, 0f, 0.5f), textColor, Skip);
        skip.anchorMin = skip.anchorMax = skip.pivot = new Vector2(1f, 1f);
        skip.anchoredPosition = new Vector2(-30f, -30f);

        var fitter = skip.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }

    private RectTransform NewButton(string name, Transform parent, string label, Color bgColor, Color labelColor, UnityAction onClick)
    {
        Image bg = NewImage(name, parent, bgColor);
        bg.raycastTarget = true;
        Button button = bg.gameObject.AddComponent<Button>();
        button.onClick.AddListener(onClick);

        ColorBlock colors = button.colors;
        colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f);
        button.colors = colors;

        var layout = bg.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 10, 10);
        layout.childControlWidth = layout.childControlHeight = true;

        NewText("Texto", bg.rectTransform, textSize * 0.8f, labelColor);
        bg.GetComponentInChildren<TMP_Text>().text = label;
        return bg.rectTransform;
    }

    private void BorderLine(RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 size)
    {
        Image img = NewImage("Linea", parent, pink);
        img.raycastTarget = false;
        RectTransform line = img.rectTransform;
        line.anchorMin = anchorMin;
        line.anchorMax = anchorMax;
        line.pivot = new Vector2(0.5f, 0.5f);
        line.sizeDelta = size;
        line.anchoredPosition = Vector2.zero;
    }

    private static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        Image img = NewRect(name, parent).gameObject.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = color.a > 0f || name == "BloqueoHueco";
        return img;
    }

    private TMP_Text NewText(string name, Transform parent, float size, Color color)
    {
        TextMeshProUGUI t = NewRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.fontSize = size;
        t.color = color;
        t.richText = true;
        t.raycastTarget = false;
        return t;
    }

    private static void SetRect(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
    {
        rt.anchoredPosition = new Vector2(xMin, yMin);
        rt.sizeDelta = new Vector2(Mathf.Max(0f, xMax - xMin), Mathf.Max(0f, yMax - yMin));
    }

    private IEnumerator FadeGroup(float from, float to)
    {
        for (float t = 0f; t < fadeDuration; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }
        group.alpha = to;
    }

    // ---------- Utilidades del editor ----------

#if UNITY_EDITOR
    // Crea la tarjeta por defecto en la escena para que la edites y la guardes como prefab
    [ContextMenu("Crear tarjeta editable en la escena")]
    private void CreateEditableCard()
    {
        var canvasGO = new GameObject("Canvas_EditarTarjeta (borrar después)", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform previousRoot = root;
        root = (RectTransform)canvasGO.transform;
        TutorialCard ui = BuildCard();
        root = previousRoot;

        ui.name = "TarjetaTutorial";
        ui.nextButton.onClick.RemoveAllListeners();
        ui.Rect.anchoredPosition = Vector2.zero;

        UnityEditor.Undo.RegisterCreatedObjectUndo(canvasGO, "Crear tarjeta del tutorial");
        UnityEditor.Selection.activeGameObject = ui.gameObject;
        Debug.Log("[Tutorial] Edita 'TarjetaTutorial', arrástrala a una carpeta del Project para hacerla prefab, " +
                  "asígnala en 'Card Prefab' y borra el Canvas_EditarTarjeta.");
    }
#endif

    [ContextMenu("Olvidar que ya se vio (volver a mostrarlo)")]
    private void ResetSeen()
    {
        PlayerPrefs.DeleteKey(SaveKey);
        Debug.Log($"[Tutorial] '{tutorialId}' se mostrará de nuevo.");
    }

    private void OnDestroy()
    {
        if (Active == this)
        {
            if (paused) Pause(false);
            Active = null;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active = null;
}
