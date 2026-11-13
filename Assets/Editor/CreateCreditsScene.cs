using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú: Tools → Escenas → Crear escena de Créditos
// Crea Assets/Scenes/Creditos.unity con Fondo.jpeg, los personajes de CREDIT.png y el texto editable.
public static class CreateCreditsScene
{
    private const string ScenePath = "Assets/Scenes/Creditos.unity";
    private const string BackgroundPath = "Assets/UI/Fondo.jpeg";
    private const string CharactersPath = "Assets/UI/CREDIT.png";

    private static readonly Color Pink = new Color(0.96f, 0.45f, 0.71f);
    private static readonly Color Line = new Color(0.868f, 0.299f, 0.586f);
    private static readonly Color DarkText = new Color(0.1f, 0.06f, 0.12f);

    [MenuItem("Tools/Escenas/Crear escena de Créditos")]
    private static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Créditos", "Ya existe Creditos.unity. ¿Reemplazarla? (se pierde el texto escrito)", "Sí", "No"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        SetupPixelArt(CharactersPath);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TMP_FontAsset font = FindFont();

        var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGO.tag = "MainCamera";
        Camera cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.white;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform canvas = canvasGO.transform;

        // Fondo (cubre la pantalla sin deformarse)
        var bgGO = new GameObject("Fondo", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        bgGO.transform.SetParent(canvas, false);
        Stretch((RectTransform)bgGO.transform);
        RawImage bg = bgGO.GetComponent<RawImage>();
        bg.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BackgroundPath);
        bg.raycastTarget = false;
        AspectRatioFitter fitter = bgGO.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;

        // Título
        TMP_Text title = NewText("Titulo", canvas, "CRÉDITOS", 96f, Pink, font);
        var tr = title.rectTransform;
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0f, 1f);
        tr.anchoredPosition = new Vector2(120f, -60f);
        tr.sizeDelta = new Vector2(900f, 130f);
        title.fontStyle = FontStyles.Bold;

        // Texto de créditos (centrado, encima de los personajes)
        TMP_Text text = NewText("TextoCreditos", canvas, "", 34f, DarkText, font);
        var xr = text.rectTransform;
        xr.anchorMin = new Vector2(0.5f, 1f);
        xr.anchorMax = new Vector2(0.5f, 1f);
        xr.pivot = new Vector2(0.5f, 1f);
        xr.anchoredPosition = new Vector2(0f, -190f);
        xr.sizeDelta = new Vector2(1300f, 440f);
        text.alignment = TextAlignmentOptions.Top;
        text.enableAutoSizing = true;   // si escriben mucho, la letra se achica sola
        text.fontSizeMin = 18f;
        text.fontSizeMax = 34f;
        text.lineSpacing = 4f;

        // Personajes (pixel art ×4) abajo, sobre el piso
        var charGO = new GameObject("Personajes", typeof(RectTransform), typeof(Image));
        charGO.transform.SetParent(canvas, false);
        var cr = (RectTransform)charGO.transform;
        cr.anchorMin = cr.anchorMax = new Vector2(0.5f, 0f);
        cr.pivot = new Vector2(0.5f, 0f);
        cr.anchoredPosition = new Vector2(0f, -10f);
        cr.sizeDelta = new Vector2(300f * 4f, 200f * 4f);
        Image chars = charGO.GetComponent<Image>();
        chars.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(CharactersPath);
        chars.preserveAspect = true;
        chars.raycastTarget = false;

        // Controlador
        var controllerGO = new GameObject("CreditsScreen");
        CreditsScreen screen = controllerGO.AddComponent<CreditsScreen>();

        // Botón Volver (MenuBuilder) arriba a la derecha
        var buttonsGO = new GameObject("Botones", typeof(RectTransform));
        buttonsGO.transform.SetParent(canvas, false);
        var br = (RectTransform)buttonsGO.transform;
        br.anchorMin = br.anchorMax = br.pivot = new Vector2(1f, 1f);
        br.anchoredPosition = new Vector2(-60f, -70f);
        br.sizeDelta = new Vector2(340f, 80f);
        MenuBuilder builder = buttonsGO.AddComponent<MenuBuilder>();
        builder.position = Vector2.zero;
        builder.width = 340f;
        builder.height = 70f;
        builder.spacing = 12f;
        builder.textPadding = 30f;
        builder.font = font;
        builder.fontSize = 36f;
        builder.textColor = DarkText;
        builder.textHoverColor = Color.white;
        builder.bandColor = Pink;
        builder.lineColor = Line;
        builder.lineThickness = 3f;
        var back = new MenuBuilder.Entry { label = "VOLVER", onClick = new UnityEvent() };
        UnityEventTools.AddPersistentListener(back.onClick, screen.BackToMenu);
        builder.entries = new[] { back };

        var so = new SerializedObject(screen);
        so.FindProperty("creditsText").objectReferenceValue = text;
        so.FindProperty("characters").objectReferenceValue = cr;
        so.ApplyModifiedPropertiesWithoutUndo();
        text.text = "(el texto se escribe en CreditsScreen)";
        CreditsColumnsLayout.Arrange(screen, canvas, font);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuild(ScenePath);
        Selection.activeGameObject = controllerGO;
        Debug.Log($"[Créditos] Creada {ScenePath}. Escribe los créditos en el objeto CreditsScreen.");
    }

    // Pixel art nítido: sin filtro ni compresión
    private static void SetupPixelArt(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;   // la imagen completa, sin recortes
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
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
        tmp.richText = true;
        if (font != null) tmp.font = font;
        return tmp;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static void AddToBuild(string path)
    {
        var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (scenes.Exists(s => s.path == path)) return;
        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
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
}
