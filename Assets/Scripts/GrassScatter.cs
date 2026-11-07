using UnityEngine;

// Esparce mechones de pasto (dos quads cruzados) sobre el suelo, dentro de un rectángulo.
// Uso: objeto vacío en el centro del jardín, ajusta Size y, en el menú ⋮ del componente,
// pulsa "Generar pasto". Los mechones se guardan en la escena (y en el prefab si lo incluyes).
public class GrassScatter : MonoBehaviour
{
    [SerializeField] private Material material;
    [SerializeField] private LayerMask groundLayer;
    [Tooltip("Ancho (X) y largo (Z) de la zona, en metros")]
    [SerializeField] private Vector2 size = new Vector2(10f, 10f);
    [SerializeField] private int count = 300;
    [Tooltip("Tamaño mínimo y máximo del mechón, en metros")]
    [SerializeField] private Vector2 tuftSize = new Vector2(0.5f, 0.8f);
    [SerializeField] private int seed = 1;

    [ContextMenu("Generar pasto")]
    private void Generate()
    {
        Clear();
        Physics.SyncTransforms();
        Mesh mesh = GetMesh();
        System.Random rng = new System.Random(seed);
        float R(float a, float b) => a + (float)rng.NextDouble() * (b - a);

        int placed = 0;
        for (int i = 0; i < count; i++)
        {
            Vector3 local = new Vector3(R(-size.x / 2f, size.x / 2f), 0f, R(-size.y / 2f, size.y / 2f));
            Vector3 origin = transform.TransformPoint(local) + Vector3.up * 3f;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 8f, groundLayer)) continue;

            GameObject go = new GameObject("Pasto");
#if UNITY_EDITOR
            UnityEditor.Undo.RegisterCreatedObjectUndo(go, "Generar pasto");
            go.isStatic = true;
#endif
            go.transform.SetParent(transform, true);
            go.transform.SetPositionAndRotation(hit.point, Quaternion.Euler(0f, R(0f, 360f), 0f));
            go.transform.localScale = Vector3.one * R(tuftSize.x, tuftSize.y);

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            placed++;
        }
        Debug.Log($"GrassScatter: {placed} mechones de {count} (los demás no encontraron suelo en la capa Ground)");
    }

    [ContextMenu("Borrar pasto")]
    private void Clear()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform c = transform.GetChild(i);
            if (c.name != "Pasto") continue;
#if UNITY_EDITOR
            UnityEditor.Undo.DestroyObjectImmediate(c.gameObject);
#else
            DestroyImmediate(c.gameObject);
#endif
        }
    }

    private Mesh GetMesh()
    {
#if UNITY_EDITOR
        // Se guarda como asset para que sobreviva en escenas y prefabs
        const string path = "Assets/VFX/TuftCross.asset";
        Mesh saved = UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (saved != null) return saved;
        Mesh m = BuildMesh();
        UnityEditor.AssetDatabase.CreateAsset(m, path);
        UnityEditor.AssetDatabase.SaveAssets();
        return m;
#else
        return BuildMesh();
#endif
    }

    // Dos quads cruzados de 1x1 m, con el pivote en la base y normales hacia arriba
    private static Mesh BuildMesh()
    {
        Mesh m = new Mesh { name = "TuftCross" };
        m.vertices = new[]
        {
            new Vector3(-0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f), new Vector3(-0.5f, 1f, 0f), new Vector3(0.5f, 1f, 0f),
            new Vector3(0f, 0f, -0.5f), new Vector3(0f, 0f, 0.5f), new Vector3(0f, 1f, -0.5f), new Vector3(0f, 1f, 0.5f)
        };
        m.uv = new[]
        {
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1),
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1)
        };
        Vector3[] n = new Vector3[8];
        for (int i = 0; i < 8; i++) n[i] = Vector3.up;
        m.normals = n;
        m.triangles = new[] { 0, 2, 1, 1, 2, 3, 4, 6, 5, 5, 6, 7 };
        m.RecalculateBounds();
        return m;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, 0.1f, size.y));
    }
}
