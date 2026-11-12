using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Herramienta de editor: cambia la fuente de todos los textos TextMeshPro
// de los prefabs del proyecto y de la escena abierta.
// Menú: Tools → Fuentes → Reemplazar fuente TMP
public class ReplaceTMPFont : EditorWindow
{
    private TMP_FontAsset newFont;
    private bool onlyDefaultFont = true;

    [MenuItem("Tools/Fuentes/Reemplazar fuente TMP")]
    private static void Open() => GetWindow<ReplaceTMPFont>("Reemplazar fuente");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            "1) Haz commit antes de usarla.\n" +
            "2) Primero 'Prefabs', luego 'Escena abierta' en cada escena.", MessageType.Info);

        newFont = (TMP_FontAsset)EditorGUILayout.ObjectField("Nueva fuente", newFont, typeof(TMP_FontAsset), false);
        onlyDefaultFont = EditorGUILayout.Toggle(
            new GUIContent("Solo la fuente por defecto",
                "Cambia solo los textos que usan la fuente por defecto de TMP (LiberationSans). " +
                "Así no se pisan los textos a los que ya les pusieron otra fuente a propósito."),
            onlyDefaultFont);

        using (new EditorGUI.DisabledScope(newFont == null))
        {
            if (GUILayout.Button("Prefabs del proyecto")) ReplaceInPrefabs();
            if (GUILayout.Button("Escena abierta")) ReplaceInOpenScene();
        }
    }

    private bool ShouldReplace(TMP_Text text)
    {
        if (text.font == newFont) return false;
        if (!onlyDefaultFont) return true;

        TMP_FontAsset defaultFont = TMP_Settings.defaultFontAsset;
        return text.font == null || text.font == defaultFont || text.font.name.StartsWith("LiberationSans");
    }

    private void Apply(TMP_Text text)
    {
        text.font = newFont;
        text.fontSharedMaterial = newFont.material;
        EditorUtility.SetDirty(text);
    }

    private void ReplaceInPrefabs()
    {
        int changedTexts = 0, changedPrefabs = 0;

        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("TextMesh Pro")) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int count = 0;

            foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!ShouldReplace(text)) continue;
                Apply(text);
                count++;
            }

            if (count > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, path);
                changedTexts += count;
                changedPrefabs++;
                Debug.Log($"[Fuente] {path}: {count} textos");
            }
            PrefabUtility.UnloadPrefabContents(root);
        }

        Debug.Log($"[Fuente] Prefabs: {changedTexts} textos en {changedPrefabs} prefabs.");
    }

    private void ReplaceInOpenScene()
    {
        var texts = new List<TMP_Text>(
            Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        int count = 0;

        foreach (TMP_Text text in texts)
        {
            if (!ShouldReplace(text)) continue;
            Undo.RecordObject(text, "Reemplazar fuente TMP");
            Apply(text);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"[Fuente] Escena '{EditorSceneManager.GetActiveScene().name}': {count} textos. Guarda la escena.");
    }
}
