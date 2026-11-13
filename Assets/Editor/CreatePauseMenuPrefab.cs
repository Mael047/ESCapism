using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;

// Menú: Tools → UI → Crear prefab Menú de Pausa
// Crea Assets/Resources/PauseMenu.prefab usando el MenuBuilder de la portada.
public static class CreatePauseMenuPrefab
{
    private const string Folder = "Assets/Resources";
    private const string Path = Folder + "/PauseMenu.prefab";

    // Mismos colores que la portada
    private static readonly Color Pink = new Color(0.96f, 0.45f, 0.71f);
    private static readonly Color Line = new Color(0.868f, 0.299f, 0.586f);
    private static readonly Color PinkLight = new Color(1f, 0.678f, 0.843f);

    [MenuItem("Tools/UI/Crear prefab Menú de Pausa")]
    private static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Path) != null &&
            !EditorUtility.DisplayDialog("Menú de pausa", "Ya existe PauseMenu.prefab. ¿Reemplazarlo?", "Sí", "No"))
            return;

        TMP_FontAsset font = FindFont();

        // Raíz con Canvas propio, encima del HUD
        var root = new GameObject("PauseMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                                  typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 800;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        PauseMenu pause = root.AddComponent<PauseMenu>();

        // Velo oscuro
        Image veil = NewImage("Velo", root.transform, new Color(0.04f, 0.02f, 0.06f, 0.8f));
        Stretch(veil.rectTransform);

        // Título
        TMP_Text title = NewText("Titulo", root.transform, "PAUSA", 110f, Pink, font);
        TopLeft(title.rectTransform, new Vector2(140f, -170f), new Vector2(700f, 150f));
        title.fontStyle = FontStyles.Bold;

        // Botones con el MenuBuilder de la portada
        var buttonsGO = new GameObject("Botones", typeof(RectTransform));
        buttonsGO.transform.SetParent(root.transform, false);
        Stretch((RectTransform)buttonsGO.transform);
        MenuBuilder builder = buttonsGO.AddComponent<MenuBuilder>();
        builder.position = new Vector2(140f, -360f);
        builder.width = 420f;
        builder.height = 70f;
        builder.spacing = 15f;
        builder.textPadding = 30f;
        builder.font = font;
        builder.fontSize = 36f;
        builder.textColor = Color.white;
        builder.textHoverColor = Color.white;
        builder.bandColor = Pink;
        builder.lineColor = Line;
        builder.lineThickness = 3f;
        builder.entries = new[]
        {
            NewEntry("CONTINUAR", pause.Resume),
            NewEntry("SALIR DEL JUEGO", pause.QuitGame),
        };

        // Volumen
        TMP_Text volTitle = NewText("TituloVolumen", root.transform, "VOLUMEN", 40f, Pink, font);
        TopLeft(volTitle.rectTransform, new Vector2(140f, -560f), new Vector2(420f, 60f));

        Slider music = NewSliderRow(root.transform, "Musica", "Música", new Vector2(140f, -630f), font);
        Slider sfx = NewSliderRow(root.transform, "Efectos", "Efectos", new Vector2(140f, -720f), font);

        // Referencias del PauseMenu
        var so = new SerializedObject(pause);
        so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
        so.FindProperty("musicSlider").objectReferenceValue = music;
        so.FindProperty("sfxSlider").objectReferenceValue = sfx;
        so.FindProperty("mixer").objectReferenceValue = FindMixer();
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets", "Resources");
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Path);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[UI] Creado {Path}");
    }

    // ---------- Piezas ----------

    private static MenuBuilder.Entry NewEntry(string label, UnityAction action)
    {
        var entry = new MenuBuilder.Entry { label = label, onClick = new UnityEvent() };
        UnityEventTools.AddPersistentListener(entry.onClick, action);
        return entry;
    }

    private static Slider NewSliderRow(Transform parent, string name, string label, Vector2 position, TMP_FontAsset font)
    {
        var row = new GameObject("Fila" + name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        TopLeft((RectTransform)row.transform, position, new Vector2(620f, 70f));

        TMP_Text text = NewText("Etiqueta", row.transform, label, 32f, Color.white, font);
        var tr = text.rectTransform;
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(0f, 1f);
        tr.pivot = new Vector2(0f, 0.5f);
        tr.sizeDelta = new Vector2(170f, 0f);
        tr.anchoredPosition = Vector2.zero;
        text.alignment = TextAlignmentOptions.MidlineLeft;

        // Slider
        var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(row.transform, false);
        var sr = (RectTransform)sliderGO.transform;
        sr.anchorMin = new Vector2(0f, 0.5f);
        sr.anchorMax = new Vector2(1f, 0.5f);
        sr.pivot = new Vector2(0f, 0.5f);
        sr.offsetMin = new Vector2(190f, -14f);
        sr.offsetMax = new Vector2(0f, 14f);

        Image background = NewImage("Fondo", sliderGO.transform, new Color(1f, 1f, 1f, 0.15f));
        var br = background.rectTransform;
        br.anchorMin = new Vector2(0f, 0.3f);
        br.anchorMax = new Vector2(1f, 0.7f);
        br.offsetMin = br.offsetMax = Vector2.zero;
        background.gameObject.AddComponent<Outline>().effectColor = Line;

        var fillArea = new GameObject("Area Relleno", typeof(RectTransform));
        fillArea.transform.SetParent(sliderGO.transform, false);
        var far = (RectTransform)fillArea.transform;
        far.anchorMin = new Vector2(0f, 0.3f);
        far.anchorMax = new Vector2(1f, 0.7f);
        far.offsetMin = far.offsetMax = Vector2.zero;
        Image fill = NewImage("Relleno", fillArea.transform, Pink);
        fill.rectTransform.sizeDelta = Vector2.zero;

        var handleArea = new GameObject("Area Manija", typeof(RectTransform));
        handleArea.transform.SetParent(sliderGO.transform, false);
        var har = (RectTransform)handleArea.transform;
        har.anchorMin = Vector2.zero;
        har.anchorMax = Vector2.one;
        har.offsetMin = new Vector2(12f, 0f);
        har.offsetMax = new Vector2(-12f, 0f);
        Image handle = NewImage("Manija", handleArea.transform, PinkLight);
        handle.rectTransform.sizeDelta = new Vector2(24f, 0f);
        handle.gameObject.AddComponent<Outline>().effectColor = Line;

        Slider slider = sliderGO.GetComponent<Slider>();
        slider.fillRect = fill.rectTransform;
        slider.handleRect = handle.rectTransform;
        slider.targetGraphic = handle;
        slider.direction = Slider.Direction.LeftToRight;
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 0.8f;

        ColorBlock colors = slider.colors;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Pink;
        slider.colors = colors;

        return slider;
    }

    private static Image NewImage(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    private static TMP_Text NewText(string name, Transform parent, string text, float size, Color color, TMP_FontAsset font)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void TopLeft(RectTransform rt, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }

    private static TMP_FontAsset FindFont()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset Pixelify"))
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (f != null && !f.name.Contains("Bold")) return f;
        }
        return null;
    }

    private static AudioMixer FindMixer()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:AudioMixer MainMixer"))
            return AssetDatabase.LoadAssetAtPath<AudioMixer>(AssetDatabase.GUIDToAssetPath(guid));
        return null;
    }
}
