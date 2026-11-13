using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.UI;

// Menú: Tools → UI → Crear prefab Menú de Opciones
// Crea Assets/Prefabs/UI/MenuOpciones.prefab con el estilo de la portada.
public static class CreateOptionsMenuPrefab
{
    private const string Folder = "Assets/Prefabs/UI";
    private const string Path = Folder + "/MenuOpciones.prefab";

    private static readonly Color Pink = new Color(0.96f, 0.45f, 0.71f);
    private static readonly Color Line = new Color(0.868f, 0.299f, 0.586f);
    private static readonly Color PinkLight = new Color(1f, 0.678f, 0.843f);

    private const float RowStartY = -320f;
    private const float RowStep = 95f;

    [MenuItem("Tools/UI/Crear prefab Menú de Opciones")]
    private static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Path) != null &&
            !EditorUtility.DisplayDialog("Menú de opciones", "Ya existe MenuOpciones.prefab. ¿Reemplazarlo?", "Sí", "No"))
            return;

        TMP_FontAsset font = FindFont();

        var root = new GameObject("MenuOpciones", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                                  typeof(GraphicRaycaster), typeof(CanvasGroup));
        Canvas canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        CanvasScaler scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        OptionsMenu options = root.AddComponent<OptionsMenu>();

        Image veil = NewImage("Velo", root.transform, new Color(0.04f, 0.02f, 0.06f, 0.92f));
        Stretch(veil.rectTransform);

        TMP_Text title = NewText("Titulo", root.transform, "OPCIONES", 100f, Pink, font);
        TopLeft(title.rectTransform, new Vector2(140f, -150f), new Vector2(900f, 140f));
        title.fontStyle = FontStyles.Bold;

        TMP_Text musicValue, sfxValue, mouseValue, stickValue;
        Slider music = NewSliderRow(root.transform, "Musica", "Música", 0, font, out musicValue);
        Slider sfx = NewSliderRow(root.transform, "Efectos", "Efectos", 1, font, out sfxValue);
        Slider mouse = NewSliderRow(root.transform, "Mouse", "Sensibilidad mouse", 2, font, out mouseValue);
        Slider stick = NewSliderRow(root.transform, "Control", "Sensibilidad control", 3, font, out stickValue);
        Toggle fullscreen = NewToggleRow(root.transform, "PantallaCompleta", "Pantalla completa", 4, font);

        // Botón Volver con el MenuBuilder
        var buttonsGO = new GameObject("Botones", typeof(RectTransform));
        buttonsGO.transform.SetParent(root.transform, false);
        Stretch((RectTransform)buttonsGO.transform);
        MenuBuilder builder = buttonsGO.AddComponent<MenuBuilder>();
        builder.position = new Vector2(140f, RowStartY - RowStep * 5 - 30f);
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
        var back = new MenuBuilder.Entry { label = "VOLVER", onClick = new UnityEvent() };
        UnityEventTools.AddPersistentListener(back.onClick, options.Close);
        builder.entries = new[] { back };

        var so = new SerializedObject(options);
        so.FindProperty("group").objectReferenceValue = root.GetComponent<CanvasGroup>();
        so.FindProperty("mixer").objectReferenceValue = FindMixer();
        so.FindProperty("musicSlider").objectReferenceValue = music;
        so.FindProperty("musicValue").objectReferenceValue = musicValue;
        so.FindProperty("sfxSlider").objectReferenceValue = sfx;
        so.FindProperty("sfxValue").objectReferenceValue = sfxValue;
        so.FindProperty("mouseSlider").objectReferenceValue = mouse;
        so.FindProperty("mouseValue").objectReferenceValue = mouseValue;
        so.FindProperty("stickSlider").objectReferenceValue = stick;
        so.FindProperty("stickValue").objectReferenceValue = stickValue;
        so.FindProperty("fullscreenToggle").objectReferenceValue = fullscreen;
        so.ApplyModifiedPropertiesWithoutUndo();

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Path);
        Object.DestroyImmediate(root);
        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[UI] Creado {Path}");
    }

    // ---------- Filas ----------

    private static RectTransform NewRow(Transform parent, string name, string label, int index, TMP_FontAsset font)
    {
        var row = new GameObject("Fila" + name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var rt = (RectTransform)row.transform;
        TopLeft(rt, new Vector2(140f, RowStartY - RowStep * index), new Vector2(900f, 70f));

        TMP_Text text = NewText("Etiqueta", row.transform, label, 32f, Color.white, font);
        var tr = text.rectTransform;
        tr.anchorMin = new Vector2(0f, 0f);
        tr.anchorMax = new Vector2(0f, 1f);
        tr.pivot = new Vector2(0f, 0.5f);
        tr.sizeDelta = new Vector2(360f, 0f);
        tr.anchoredPosition = Vector2.zero;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return rt;
    }

    private static Slider NewSliderRow(Transform parent, string name, string label, int index,
                                       TMP_FontAsset font, out TMP_Text valueText)
    {
        RectTransform row = NewRow(parent, name, label, index, font);

        var sliderGO = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
        sliderGO.transform.SetParent(row, false);
        var sr = (RectTransform)sliderGO.transform;
        sr.anchorMin = sr.anchorMax = new Vector2(0f, 0.5f);
        sr.pivot = new Vector2(0f, 0.5f);
        sr.anchoredPosition = new Vector2(380f, 0f);
        sr.sizeDelta = new Vector2(380f, 28f);

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
        ColorBlock colors = slider.colors;
        colors.highlightedColor = Color.white;
        colors.pressedColor = Pink;
        slider.colors = colors;

        valueText = NewText("Valor", row, "100%", 30f, PinkLight, font);
        var vr = valueText.rectTransform;
        vr.anchorMin = vr.anchorMax = new Vector2(0f, 0.5f);
        vr.pivot = new Vector2(0f, 0.5f);
        vr.anchoredPosition = new Vector2(790f, 0f);
        vr.sizeDelta = new Vector2(110f, 60f);
        valueText.alignment = TextAlignmentOptions.MidlineLeft;

        return slider;
    }

    private static Toggle NewToggleRow(Transform parent, string name, string label, int index, TMP_FontAsset font)
    {
        RectTransform row = NewRow(parent, name, label, index, font);

        var toggleGO = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
        toggleGO.transform.SetParent(row, false);
        var tr = (RectTransform)toggleGO.transform;
        tr.anchorMin = tr.anchorMax = new Vector2(0f, 0.5f);
        tr.pivot = new Vector2(0f, 0.5f);
        tr.anchoredPosition = new Vector2(380f, 0f);
        tr.sizeDelta = new Vector2(44f, 44f);

        Image box = NewImage("Caja", toggleGO.transform, new Color(1f, 1f, 1f, 0.15f));
        Stretch(box.rectTransform);
        var outline = box.gameObject.AddComponent<Outline>();
        outline.effectColor = Line;
        outline.effectDistance = new Vector2(3f, -3f);

        Image check = NewImage("Marca", box.transform, Pink);
        Stretch(check.rectTransform);
        check.rectTransform.offsetMin = new Vector2(8f, 8f);
        check.rectTransform.offsetMax = new Vector2(-8f, -8f);

        Toggle toggle = toggleGO.GetComponent<Toggle>();
        toggle.targetGraphic = box;
        toggle.graphic = check;
        toggle.isOn = true;
        return toggle;
    }

    // ---------- Utilidades ----------

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
