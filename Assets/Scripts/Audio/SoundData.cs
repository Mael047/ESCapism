using UnityEngine;
using UnityEngine.Audio;

// Un sonido del juego. Crea uno con clic derecho → Create → Audio → Sound.
// Si le pones varios clips, elige uno al azar (sin repetir el anterior).
[CreateAssetMenu(fileName = "SFX_", menuName = "Audio/Sound")]
public class SoundData : ScriptableObject
{
    public AudioClip[] clips;

    [Header("Volumen y tono")]
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Variación aleatoria de volumen en cada reproducción")]
    [Range(0f, 0.5f)] public float volumeVariation = 0.1f;
    [Tooltip("Tono mínimo y máximo (1 = normal). Variar un poco evita que suene repetitivo")]
    public Vector2 pitch = new Vector2(0.95f, 1.05f);

    [Header("Espacio")]
    [Tooltip("0 = 2D (suena igual en todas partes), 1 = 3D (se oye según la distancia a la cámara)")]
    [Range(0f, 1f)] public float spatialBlend = 0.7f;
    public float minDistance = 3f;
    public float maxDistance = 35f;

    [Header("Límites")]
    [Tooltip("Cuántas copias de este sonido pueden sonar al mismo tiempo")]
    [Min(1)] public int maxInstances = 4;
    [Tooltip("Tiempo mínimo entre dos reproducciones (evita que 5 golpes a la vez saturen)")]
    public float cooldown = 0.03f;

    [Tooltip("Opcional: grupo del mixer solo para este sonido. Vacío = grupo SFX")]
    public AudioMixerGroup output;

    [System.NonSerialized] private int lastIndex = -1;

    public AudioClip PickClip()
    {
        if (clips == null || clips.Length == 0) return null;
        if (clips.Length == 1) return clips[0];

        int index = Random.Range(0, clips.Length - 1);
        if (index >= lastIndex) index++;   // nunca el mismo dos veces seguidas
        lastIndex = index;
        return clips[index];
    }
}
