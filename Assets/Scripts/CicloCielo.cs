using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;

public class CicloCielo : MonoBehaviour
{
    [System.Serializable]
    public struct Fase
    {
        public Color skyTint, ground, luzColor, ambiente, niebla;
        public float exposure, solRotX, luzIntensidad;

        public static Fase Lerp(Fase a, Fase b, float k) => new Fase
        {
            skyTint = Color.Lerp(a.skyTint, b.skyTint, k),
            ground = Color.Lerp(a.ground, b.ground, k),
            luzColor = Color.Lerp(a.luzColor, b.luzColor, k),
            ambiente = Color.Lerp(a.ambiente, b.ambiente, k),
            niebla = Color.Lerp(a.niebla, b.niebla, k),
            exposure = Mathf.Lerp(a.exposure, b.exposure, k),
            solRotX = Mathf.Lerp(a.solRotX, b.solRotX, k),
            luzIntensidad = Mathf.Lerp(a.luzIntensidad, b.luzIntensidad, k)
        };
    }

    [Header("Cielo")]
    public Light sol;
    public Material skyMat;
    public Fase[] fases = new Fase[6];
    public float duracion = 6f;
    [Tooltip("Fuerza el ambiente en modo Color para que 'ambiente' sí se aplique")]
    public bool forzarAmbienteFlat = true;
    [Tooltip("Reparte las fases entre todas las oleadas del nivel (útil si hay 3 oleadas y 6 fases)")]
    public bool mapearAlTotal = true;

    [Header("Estrellas")]
    public ParticleSystem estrellas;
    [Tooltip("Última fase SIN estrellas (1 = oleada 2)")]
    public int faseSinEstrellas = 1;
    [Range(0f, 1f)] public float brilloMaxEstrellas = 1f;
    public Vector2 tamanoEstrellas = new Vector2(0.3f, 0.9f);

    [Header("Nubes")]
    public bool crearNubes = true;
    public Material materialNubes;
    public int cantidadNubes = 8;
    public Vector3 centroNubes = Vector3.zero;
    public Vector2 radioNubes = new Vector2(40f, 65f);
    public Vector2 alturaNubes = new Vector2(25f, 40f);
    public float velocidadNubes = 1.5f;
    [Range(0.2f, 1.5f)] public float tamanoNubes = 0.5f;
    public int bloquesPorNube = 3;

    [Header("Luciérnagas (script aparte)")]
    [Tooltip("Si lo dejas vacío lo busca en la escena")]
    public Luciernagas luciernagas;
    [Tooltip("Última fase SIN luciérnagas (1 = oleada 2)")]
    public int faseSinLuciernagas = 1;

    Coroutine rutina;
    int faseActual;
    float faseVisual;
    Fase actual;
    WaveManager waveManager;
    Camera cam;
    Transform nubesPivot;
    ParticleSystemRenderer estrellasRend;
    Material matEstrellas, matNubes;

    // ---------- Paleta (solo rellena las fases, no toca tus referencias) ----------
    [ContextMenu("Aplicar paleta rosa-violeta (solo fases)")]
    void AplicarPaleta()
    {
        string[,] d = {   // skyTint, luzColor, ambiente
            {"#FF8FB1","#FFA8C8","#7A5A9A"}, {"#E8629A","#F58CC0","#6A4A9C"},
            {"#B14FB0","#C07AD8","#4F4590"}, {"#7A4AC0","#8F85E0","#36367F"},
            {"#4A3AA8","#6F8CE8","#232C78"}, {"#2A2275","#5B7FE0","#161E55"}};
        float[] exp  = {1f, .85f, .7f, .5f, .35f, .2f};
        float[] rot  = {5, 0, -5, -10, -15, -20};
        float[] inte = {.95f, .85f, .7f, .55f, .42f, .32f};

        fases = new Fase[6];
        for (int i = 0; i < 6; i++)
        {
            Fase f = new Fase();
            ColorUtility.TryParseHtmlString(d[i, 0], out f.skyTint);
            ColorUtility.TryParseHtmlString(d[i, 1], out f.luzColor);
            ColorUtility.TryParseHtmlString(d[i, 2], out f.ambiente);
            f.niebla = Color.Lerp(f.ambiente, f.skyTint, 0.3f);
            f.ground = f.skyTint * 0.35f; f.ground.a = 1f;
            f.exposure = exp[i]; f.solRotX = rot[i]; f.luzIntensidad = inte[i];
            fases[i] = f;
        }
    }

    // ---------- Ciclo de vida ----------
    void Start()
    {
        cam = Camera.main;
        if (luciernagas == null) luciernagas = FindAnyObjectByType<Luciernagas>();
        if (forzarAmbienteFlat) RenderSettings.ambientMode = AmbientMode.Flat;
        ConfigurarEstrellas();
        if (crearNubes) CrearNubes();
        Aplicar(fases[0], 0f);
    }

    void OnEnable()
    {
        waveManager = FindAnyObjectByType<WaveManager>();
        if (waveManager != null) waveManager.WaveStarted += OnWaveStarted;
    }

    void OnDisable()
    {
        if (waveManager != null) waveManager.WaveStarted -= OnWaveStarted;
    }

    void OnWaveStarted(int oleada, int total, bool esJefe)
    {
        int destino = oleada - 1;
        if (mapearAlTotal && total > 1)
            destino = Mathf.RoundToInt((oleada - 1f) / (total - 1f) * (fases.Length - 1));
        IrAFase(destino);
    }

    void Update()
    {
        if (nubesPivot != null) nubesPivot.Rotate(0f, velocidadNubes * Time.deltaTime, 0f);
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        if (estrellas != null && cam != null) estrellas.transform.position = cam.transform.position;
    }

    // ---------- Fases ----------
    float Prog(int faseSin) => Mathf.InverseLerp(faseSin, fases.Length - 1, faseVisual);

    public void IrAFase(int i)
    {
        faseActual = Mathf.Clamp(i, 0, fases.Length - 1);
        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Transicion(faseActual));
    }

    [ContextMenu("Siguiente fase (test)")]
    public void SiguienteTest() => IrAFase(faseActual + 1);

    IEnumerator Transicion(int destino)
    {
        Fase origen = actual;
        float visual0 = faseVisual;
        for (float t = 0; t < 1f; t += Time.deltaTime / duracion)
        {
            float k = Mathf.SmoothStep(0, 1, t);
            Aplicar(Fase.Lerp(origen, fases[destino], k), Mathf.Lerp(visual0, destino, k));
            yield return null;
        }
        Aplicar(fases[destino], destino);
    }

    void Aplicar(Fase f, float visual)
    {
        actual = f;
        faseVisual = visual;

        skyMat.SetColor("_SkyTint", f.skyTint);
        skyMat.SetColor("_GroundColor", f.ground);
        skyMat.SetFloat("_Exposure", f.exposure);
        sol.transform.rotation = Quaternion.Euler(f.solRotX, -30f, 0f);
        sol.color = f.luzColor;
        sol.intensity = f.luzIntensidad;
        RenderSettings.ambientLight = f.ambiente;
        RenderSettings.fogColor = f.niebla;
        if (RenderSettings.ambientMode == AmbientMode.Skybox) DynamicGI.UpdateEnvironment();

        ActualizarEfectos();
    }

    // Solo se llama cuando cambia la fase (no cada frame)
    void ActualizarEfectos()
    {
        if (matEstrellas != null)
        {
            float b = Prog(faseSinEstrellas) * brilloMaxEstrellas;
            estrellasRend.enabled = b > 0.01f;
            PonerColor(matEstrellas, new Color(1f, 1f, 1f, b));
        }

        if (matNubes != null)
            PonerColor(matNubes, Color.Lerp(actual.luzColor, actual.ambiente, 0.5f));

        if (luciernagas != null)
            luciernagas.SetProgreso(Prog(faseSinLuciernagas));
    }

    static void PonerColor(Material m, Color c)
    {
        if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
        if (m.HasProperty("_Color")) m.SetColor("_Color", c);
        if (m.HasProperty("_TintColor")) m.SetColor("_TintColor", c);
    }

    // ---------- Estrellas ----------
    void ConfigurarEstrellas()
    {
        if (estrellas == null) return;
        var main = estrellas.main;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.startSize = new ParticleSystem.MinMaxCurve(tamanoEstrellas.x, tamanoEstrellas.y);
        var shape = estrellas.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 150f;
        shape.radiusThickness = 0f;
        estrellasRend = estrellas.GetComponent<ParticleSystemRenderer>();
        matEstrellas = estrellasRend.material;
        estrellas.Clear();
        estrellas.Play();
    }

    // ---------- Nubes (bloques low poly) ----------
    void CrearNubes()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null) sh = Shader.Find("Unlit/Color");
        matNubes = materialNubes != null ? new Material(materialNubes) : new Material(sh);

        nubesPivot = new GameObject("Nubes").transform;
        nubesPivot.position = centroNubes;
        float sector = 360f / Mathf.Max(1, cantidadNubes);

        for (int n = 0; n < cantidadNubes; n++)
        {
            float ang = n * sector + Random.Range(-sector * 0.25f, sector * 0.25f);
            Transform nube = new GameObject("Nube").transform;
            nube.SetParent(nubesPivot, false);
            nube.localPosition = Quaternion.Euler(0, ang, 0) * Vector3.forward * Random.Range(radioNubes.x, radioNubes.y)
                               + Vector3.up * Random.Range(alturaNubes.x, alturaNubes.y);
            nube.localRotation = Quaternion.Euler(0, Random.Range(0f, 360f), 0);

            int bloques = Mathf.Max(1, bloquesPorNube + Random.Range(0, 2));
            for (int b = 0; b < bloques; b++)
            {
                GameObject cubo = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Destroy(cubo.GetComponent<Collider>());
                Transform ct = cubo.transform;
                ct.SetParent(nube, false);
                ct.localPosition = new Vector3(Random.Range(-5f, 5f), Random.Range(-0.5f, 1f), Random.Range(-2.5f, 2.5f)) * tamanoNubes;
                ct.localScale = new Vector3(Random.Range(6f, 12f), Random.Range(2f, 3.5f), Random.Range(4f, 7f)) * tamanoNubes;

                var r = cubo.GetComponent<Renderer>();
                r.sharedMaterial = matNubes;
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.receiveShadows = false;
            }
        }
    }
}