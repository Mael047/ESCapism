using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// Menú: Tools → UI → Crear prefab Botón Omitir
// Crea Assets/Prefabs/UI/BotonOmitir.prefab con el estilo del juego.
public static class CreateSkipButtonPrefab
{
    private const string Folder = "Assets/Prefabs/UI";
    private const string Path = Folder + "/BotonOmitir.prefab";

    [MenuItem("Tools/UI/Crear prefab Botón Omitir")]
    private static void Create()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(Path) != null &&
            !EditorUtility.DisplayDialog("Botón Omitir", "Ya existe BotonOmitir.prefab. ¿Reemplazarlo?", "Sí", "No"))
            return;

        Color pink = new Color(0.953f, 0.196f, 0.514f);        // #F33283
        Color pinkLight = new Color(1f, 0.678f, 0.843f);        // #FFADD7

        // Raíz: arriba a la derecha
        var root = new GameObject("BotonOmitir", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
        var rect = (RectTransform)root.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-40f, -40f);
        rect.sizeDelta = new Vector2(250f, 64f);

        Image bg = root.GetComponent<Image>();
        bg.color = new Color(0.07f, 0.04f, 0.09f, 0.75f);

        var outline = root.AddComponent<Outline>();
        outline.effectColor = pink;
        outline.effectDistance = new Vector2(3f, -3f);

        Button button = root.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.75f, 0.88f);
        colors.pressedColor = new Color(0.85f, 0.55f, 0.7f);
        colors.selectedColor = colors.highlightedColor;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        // Texto
        var textGO = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(root.transform, false);
        var textRect = (RectTransform)textGO.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 6f);
        textRect.offsetMax = new Vector2(-16f, -6f);

        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.text = "Omitir  <color=#F33283>>></color>";
        tmp.fontSize = 32f;
        tmp.color = pinkLight;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        TMP_FontAsset font = FindPixelifyFont();
        if (font != null) tmp.font = font;

        root.AddComponent<SkipButton>();

        if (!AssetDatabase.IsValidFolder(Folder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs")) AssetDatabase.CreateFolder("Assets", "Prefabs");
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, Path);
        Object.DestroyImmediate(root);

        Selection.activeObject = prefab;
        EditorGUIUtility.PingObject(prefab);
        Debug.Log($"[UI] Creado {Path}" + (font == null ? " (no encontré la fuente Pixelify Sans: asígnala en el texto)" : ""));
    }

    private static TMP_FontAsset FindPixelifyFont()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:TMP_FontAsset Pixelify"))
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(guid));
            if (font != null && !font.name.Contains("Bold")) return font;
        }
        return null;
    }
}
