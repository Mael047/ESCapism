using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Menú: Tools → Escenas → Acomodar créditos en columnas
// Reacomoda la escena de créditos abierta: encabezado arriba, 3 columnas y personajes más pequeños.
// No borra el texto escrito en CreditsScreen.
public static class CreditsColumnsLayout
{
    private static readonly Color DarkText = new Color(0.1f, 0.06f, 0.12f);
    private static readonly Color Pink = new Color(0.953f, 0.196f, 0.514f);

    // Medidas en píxeles de la pantalla de referencia (1920 × 1080)
    private const float Top = -285f;          // dónde empiezan las columnas
    private const float SideWidth = 440f;
    private const float SideHeight = 760f;
    private const float SideMargin = 70f;
    private const float CenterWidth = 760f;
    private const float CenterHeight = 440f;  // termina encima de los personajes
    private const float CharacterScale = 3f;
    private const float FontSize = 28f;

    [MenuItem("Tools/Escenas/Acomodar créditos en columnas")]
    private static void ArrangeOpenScene()
    {
        CreditsScreen screen = Object.FindAnyObjectByType<CreditsScreen>();
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (screen == null || canvas == null)
        {
            EditorUtility.DisplayDialog("Créditos", "Abre la escena Creditos primero.", "OK");
            return;
        }

        TMP_FontAsset font = null;
        var so = new SerializedObject(screen);
        var old = so.FindProperty("creditsText").objectReferenceValue as TMP_Text;
        if (old != null) font = old.font;

        Arrange(screen, canvas.transform, font);
        EditorSceneManager.MarkSceneDirty(screen.gameObject.scene);
        EditorSceneManager.SaveScene(screen.gameObject.scene);
        Debug.Log("[Créditos] Escena acomodada en 3 columnas y guardada.");
    }

    public static void Arrange(CreditsScreen screen, Transform canvas, TMP_FontAsset font)
    {
        // Quita la versión anterior si se vuelve a ejecutar
        foreach (string name in new[] { "Encabezado", "ColumnaIzquierda", "ColumnaCentro", "ColumnaDerecha" })
        {
            Transform previous = canvas.Find(name);
            if (previous != null) Object.DestroyImmediate(previous.gameObject);
        }

        // Oculta el texto único anterior
        Transform single = canvas.Find("TextoCreditos");
        if (single != null) single.gameObject.SetActive(false);

        // Encabezado (# MLT Games Studio)
        TMP_Text header = NewText("Encabezado", canvas, font, 52f, Pink);
        Place(header.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -185f), new Vector2(1000f, 80f));
        header.alignment = TextAlignmentOptions.Center;
        header.enableAutoSizing = true;
        header.fontSizeMin = 30f;
        header.fontSizeMax = 52f;

        // Columnas
        TMP_Text left = NewText("ColumnaIzquierda", canvas, font, FontSize, DarkText);
        Place(left.rectTransform, new Vector2(0f, 1f), new Vector2(SideMargin, Top), new Vector2(SideWidth, SideHeight));

        TMP_Text center = NewText("ColumnaCentro", canvas, font, FontSize, DarkText);
        Place(center.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, Top), new Vector2(CenterWidth, CenterHeight));

        TMP_Text right = NewText("ColumnaDerecha", canvas, font, FontSize, DarkText);
        Place(right.rectTransform, new Vector2(1f, 1f), new Vector2(-SideMargin, Top), new Vector2(SideWidth, SideHeight));

        // Personajes más pequeños para liberar los lados
        Transform characters = canvas.Find("Personajes");
        if (characters != null)
        {
            var rt = (RectTransform)characters;
            rt.sizeDelta = new Vector2(300f * CharacterScale, 200f * CharacterScale);
            rt.anchoredPosition = new Vector2(0f, -8f);
        }

        // Referencias
        var so = new SerializedObject(screen);
        so.FindProperty("headerText").objectReferenceValue = header;
        SerializedProperty cols = so.FindProperty("columns");
        cols.arraySize = 3;
        cols.GetArrayElementAtIndex(0).objectReferenceValue = left;
        cols.GetArrayElementAtIndex(1).objectReferenceValue = center;
        cols.GetArrayElementAtIndex(2).objectReferenceValue = right;
        SerializedProperty weights = so.FindProperty("columnWeights");
        weights.arraySize = 3;
        weights.GetArrayElementAtIndex(0).floatValue = 1f;
        weights.GetArrayElementAtIndex(1).floatValue = 0.6f;
        weights.GetArrayElementAtIndex(2).floatValue = 1f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Refresca el texto en el editor
        screen.SendMessage("OnValidate", SendMessageOptions.DontRequireReceiver);
    }

    private static TMP_Text NewText(string name, Transform parent, TMP_FontAsset font, float size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        // Debajo del botón Volver para que no lo tape
        Transform buttons = parent.Find("Botones");
        if (buttons != null) go.transform.SetSiblingIndex(buttons.GetSiblingIndex());

        var tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = size;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Top;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.lineSpacing = 4f;
        tmp.richText = true;
        tmp.raycastTarget = false;
        if (font != null) tmp.font = font;
        return tmp;
    }

    private static void Place(RectTransform rt, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;
    }
}
