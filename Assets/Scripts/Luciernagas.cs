using UnityEngine;
using UnityEngine.Rendering;

public class Luciernagas : MonoBehaviour
{
    [Header("Seguimiento")]
    [Tooltip("Si es null busca el tag Player; si no, usa la cámara")]
    public Transform seguir;
    public float alturaSuelo = 0f;

    [Header("Visuales (muchas, sin luz)")]
    public int cantidadVisual = 120;
    public float radioVisual = 25f;

    [Header("Con luz real (pocas, cerca del jugador)")]
    public int cantidadConLuz = 12;
    public float radioConLuz = 10f;
    public float rangoLuz = 8f;
    public float intensidadLuz = 2.5f;

    [Header("Aspecto")]
    public Vector2 altura = new Vector2(0.3f, 2.5f);
    public Vector2 tamano = new Vector2(0.5f, 1f);
    [Tooltip("Mayor que 1 = más brillo (más notorio con Bloom)")]
    public float brilloHDR = 1.5f;
    public Color colorA = new Color(0.85f, 1f, 0.55f);  // amarillo-verdoso
    public Color colorB = new Color(1f, 0.6f, 0.85f);   // rosado
    public Color colorC = new Color(0.7f, 0.6f, 1f);    // violeta

    const float VIDA = 8f;
    ParticleSystem visual, conLuz;
    Material mat;
    Camera cam;
    float progreso;

    void Start()
    {
        cam = Camera.main;
        if (seguir == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) seguir = p.transform;
        }
        mat = CrearMaterial();
        visual = Crear("Visuales", cantidadVisual, radioVisual, false);
        conLuz = Crear("ConLuz", cantidadConLuz, radioConLuz, true);
        SetProgreso(progreso);
    }

    void LateUpdate()
    {
        if (cam == null) cam = Camera.main;
        Transform t = seguir != null ? seguir : (cam != null ? cam.transform : null);
        if (t != null) transform.position = new Vector3(t.position.x, alturaSuelo, t.position.z);
    }

    // p = 0 (sin luciérnagas) a 1 (máximo). Lo llama CicloCielo.
    public void SetProgreso(float p)
    {
        progreso = p;
        if (visual == null) return;

        float b = Mathf.Clamp01(p * 10f) * Mathf.Lerp(0.5f, 1f, p); // mínimo 50% al aparecer
        var e1 = visual.emission; e1.rateOverTime = cantidadVisual / VIDA * b;
        var e2 = conLuz.emission; e2.rateOverTime = cantidadConLuz / VIDA * b;
        var lz = conLuz.lights; lz.intensityMultiplier = b;

        float h = brilloHDR;
        mat.SetColor("_BaseColor", new Color(h, h, h, Mathf.Lerp(0.4f, 1f, b)));
    }

    ParticleSystem Crear(string nombre, int cant, float radio, bool luz)
    {
        var go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = new ParticleSystem.MinMaxCurve(VIDA - 2f, VIDA + 2f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(tamano.x, tamano.y);
        main.maxParticles = cant + 20;

        var gc = new Gradient();
        gc.SetKeys(
            new[] { new GradientColorKey(colorA, 0f), new GradientColorKey(colorB, 0.5f), new GradientColorKey(colorC, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        var mg = new ParticleSystem.MinMaxGradient(gc) { mode = ParticleSystemGradientMode.RandomColor };
        main.startColor = mg;

        var em = ps.emission; em.rateOverTime = 0f;

        float alto = Mathf.Max(0.1f, altura.y - altura.x);
        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(radio * 2f, alto, radio * 2f);
        shape.position = new Vector3(0f, altura.x + alto * 0.5f, 0f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.8f; noise.frequency = 0.25f; noise.scrollSpeed = 0.3f;
        noise.damping = true; noise.octaveCount = 1;
        noise.quality = ParticleSystemNoiseQuality.Low;

        // Parpadeo
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gb = new Gradient();
        gb.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] {
                new GradientAlphaKey(0f, 0f),      new GradientAlphaKey(1f, 0.12f),
                new GradientAlphaKey(0.2f, 0.28f), new GradientAlphaKey(1f, 0.45f),
                new GradientAlphaKey(0.15f, 0.62f), new GradientAlphaKey(1f, 0.8f),
                new GradientAlphaKey(0f, 1f) });
        col.color = gb;

        var rend = go.GetComponent<ParticleSystemRenderer>();
        rend.renderMode = ParticleSystemRenderMode.Billboard;
        rend.shadowCastingMode = ShadowCastingMode.Off;
        rend.receiveShadows = false;
        rend.sharedMaterial = mat;

        if (luz)
        {
            var plantilla = new GameObject("PlantillaLuz");
            plantilla.transform.SetParent(go.transform, false);
            var l = plantilla.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = rangoLuz;
            l.intensity = intensidadLuz;
            l.shadows = LightShadows.None;
            plantilla.SetActive(false);

            var lz = ps.lights;
            lz.enabled = true;
            lz.light = l;
            lz.ratio = 1f;                  // todas las de este sistema iluminan
            lz.useParticleColor = true;
            lz.alphaAffectsIntensity = true;
            lz.sizeAffectsRange = false;
            lz.maxLights = cant;
        }

        ps.Play();
        return ps;
    }

    Material CrearMaterial()
    {
        Shader sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        Material m;
        if (sh != null)
        {
            m = new Material(sh);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 2f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.One);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.renderQueue = 3000;
        }
        else m = new Material(Shader.Find("Legacy Shaders/Particles/Additive"));

        var tex = CrearTexturaGlow();
        if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
        if (m.HasProperty("_MainTex")) m.SetTexture("_MainTex", tex);
        return m;
    }

    // Núcleo brillante + aureola suave
    static Texture2D CrearTexturaGlow()
    {
        const int N = 64;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        float c = (N - 1) * 0.5f;
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float d = Mathf.Clamp01(Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c);
                float a = Mathf.Clamp01(Mathf.Pow(1f - d, 4f) * 1.2f + Mathf.Pow(1f - d, 2f) * 0.25f);
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        t.Apply();
        return t;
    }
}