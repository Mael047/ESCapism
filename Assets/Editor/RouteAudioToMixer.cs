using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

// Conecta los AudioSource al MainMixer para que respondan a los sliders de volumen.
// - Si el objeto se llama Musica / Music → grupo Music
// - Todo lo demás (efectos, ambiente) → grupo SFX
// Solo cambia los AudioSource que no tienen Output asignado.
public static class RouteAudioToMixer
{
    [MenuItem("Tools/Audio/Conectar sonidos de la escena abierta al Mixer")]
    private static void RouteOpenScene()
    {
        if (!FindGroups(out AudioMixerGroup music, out AudioMixerGroup sfx)) return;

        Scene scene = SceneManager.GetActiveScene();
        int count = Route(scene, music, sfx);
        if (count > 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log($"[Audio] {scene.name}: {count} AudioSource conectados al mixer.");
    }

    [MenuItem("Tools/Audio/Conectar sonidos de TODAS las escenas al Mixer")]
    private static void RouteAllScenes()
    {
        if (!FindGroups(out AudioMixerGroup music, out AudioMixerGroup sfx)) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string original = SceneManager.GetActiveScene().path;
        int total = 0;

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            int count = Route(scene, music, sfx);
            if (count > 0) EditorSceneManager.SaveScene(scene);
            Debug.Log($"[Audio] {scene.name}: {count} AudioSource conectados.");
            total += count;
        }

        if (!string.IsNullOrEmpty(original)) EditorSceneManager.OpenScene(original, OpenSceneMode.Single);
        Debug.Log($"[Audio] Listo: {total} AudioSource conectados en total.");
    }

    private static int Route(Scene scene, AudioMixerGroup music, AudioMixerGroup sfx)
    {
        int count = 0;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (AudioSource source in root.GetComponentsInChildren<AudioSource>(true))
            {
                if (source.outputAudioMixerGroup != null) continue;

                string name = source.gameObject.name.ToLowerInvariant();
                bool isMusic = name.Contains("music") || name.Contains("musica") || name.Contains("música");

                Undo.RecordObject(source, "Conectar al mixer");
                source.outputAudioMixerGroup = isMusic ? music : sfx;
                EditorUtility.SetDirty(source);
                Debug.Log($"   {source.gameObject.name} → {(isMusic ? "Music" : "SFX")}", source);
                count++;
            }
        }
        return count;
    }

    private static bool FindGroups(out AudioMixerGroup music, out AudioMixerGroup sfx)
    {
        music = sfx = null;
        string guid = AssetDatabase.FindAssets("t:AudioMixer MainMixer").FirstOrDefault();
        AudioMixer mixer = guid != null ? AssetDatabase.LoadAssetAtPath<AudioMixer>(AssetDatabase.GUIDToAssetPath(guid)) : null;

        if (mixer != null)
        {
            music = mixer.FindMatchingGroups("Music").FirstOrDefault();
            sfx = mixer.FindMatchingGroups("SFX").FirstOrDefault();
        }
        if (music == null || sfx == null)
        {
            EditorUtility.DisplayDialog("Audio", "No encontré el MainMixer con los grupos Music y SFX.", "OK");
            return false;
        }
        return true;
    }
}
