using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú: Tools → Escenas → Crear escena de Finales
// Crea Assets/Scenes/Finales.unity con las 3 imágenes y la agrega al build.
public static class CreateEndingScene
{
    private const string ScenePath = "Assets/Scenes/Finales.unity";
    private static readonly Color Pink = new Color(0.96f, 0.45f, 0.71f);
    private static readonly Color Line = new Color(0.868f, 0.299f, 0.586f);

    [MenuItem("Tools/Escenas/Crear escena de Finales")]
    private static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null &&
            !EditorUtility.DisplayDialog("Finales", "Ya existe Finales.unity. ¿Reemplazarla?", "Sí", "No"))
            return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        TMP_FontAsset font = FindFont();

        // Cámara negra
        var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camGO.tag = "MainCamera";
        Camera cam = camGO.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;

        // EventSystem con el Input System nuevo
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // Canvas
        var canvasGO = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        Transform canvas = canvasGO.transform;

        // Imagen del final (cubre la pantalla sin deformarse)
        var imageGO = new GameObject("ImagenFinal", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        imageGO.transform.SetParent(canvas, false);
        Stretch((RectTransform)imageGO.transform);
        AspectRatioFitter fitter = imageGO.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = 16f / 9f;
        RawImage raw = imageGO.GetComponent<RawImage>();
        raw.raycastTarget = false;

        // Sombra abajo para que se lean los textos y botones
        var shadeGO = new GameObject("SombraInferior", typeof(RectTransform), typeof(Image));
        shadeGO.transform.SetParent(canvas, false);
        var shade = (RectTransform)shadeGO.transform;
        shade.anchorMin = new Vector2(0f, 0f);
        shade.anchorMax = new Vector2(1f, 0.42f);
        shade.offsetMin = shade.offsetMax = Vector2.zero;
        Image shadeImg = shadeGO.GetComponent<Image>();
        shadeImg.color = new Color(0f, 0f, 0f, 0.55f);
        shadeImg.raycastTarget = false;

        // Textos
        var textsGO = new GameObject("Textos", typeof(RectTransform), typeof(CanvasGroup));
        textsGO.transform.SetParent(canvas, false);
        Stretch((RectTransform)textsGO.transform);
        TMP_Text title = NewText("Titulo", textsGO.transform, 72f, Pink, font, new Vector2(140f, -650f), new Vector2(1200f, 100f));
        title.fontStyle = FontStyles.Bold;
        TMP_Text body = NewText("Texto", textsGO.transform, 32f, Color.white, font, new Vector2(140f, -745f), new Vector2(1200f, 90f));

        // Botones con el MenuBuilder
        var buttonsGO = new GameObject("Botones", typeof(RectTransform), typeof(CanvasGroup));
        buttonsGO.transform.SetParent(canvas, false);
        Stretch((RectTransform)buttonsGO.transform);

        var controllerGO = new GameObject("EndingScreen");
        EndingScreen screen = controllerGO.AddComponent<EndingScreen>();

        MenuBuilder builder = buttonsGO.AddComponent<MenuBuilder>();
        builder.position = new Vector2(140f, -850f);
        builder.width = 460f;
        builder.height = 64f;
        builder.spacing = 12f;
        builder.textPadding = 30f;
        builder.font = font;
        builder.fontSize = 34f;
        builder.textColor = Color.white;
        builder.textHoverColor = Color.white;
        builder.bandColor = Pink;
        builder.lineColor = Line;
        builder.lineThickness = 3f;
        builder.entries = new[]
        {
            NewEntry("VOLVER AL MENÚ", screen.BackToMenu),
            NewEntry("SALIR DEL JUEGO", screen.QuitGame),
        };

        // Referencias
        var so = new SerializedObject(screen);
        so.FindProperty("image").objectReferenceValue = raw;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("textGroup").objectReferenceValue = textsGO.GetComponent<CanvasGroup>();
        so.FindProperty("buttonsGroup").objectReferenceValue = buttonsGO.GetComponent<CanvasGroup>();
        SerializedProperty images = so.FindProperty("endingImages");
        images.arraySize = 3;
        for (int i = 0; i < 3; i++)
            images.GetArrayElementAtIndex(i).objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/UI/Final {i + 1}.png");
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddToBuild(ScenePath);
        Debug.Log($"[Finales] Creada {ScenePath} y agregada al build.");
    }

    private static MenuBuilder.Entry NewEntry(string label, UnityAction action)
    {
        var entry = new MenuBuilder.Entry { label = label, onClick = new UnityEvent() };
        UnityEventTools.AddPersistentListener(entry.onClick, action);
        return entry;
    }

    private static TMP_Text NewText(string name, Transform parent, float size, Color color, TMP_FontAsset font,
                                    Vector2 position, Vector2 boxSize)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = position;
        rt.sizeDelta = boxSize;
        var tmp = go.GetComponent<TextMeshProUGUI>();
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
