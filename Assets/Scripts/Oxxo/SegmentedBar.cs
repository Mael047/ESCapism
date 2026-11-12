using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra por segmentos (estilo pixel art): N cuadritos que se llenan según un valor de 0 a 1.
/// Va en la raíz de la barra. Los segmentos se crean solos dentro de 'contenedor'
/// a partir de 'prefabSegmento', o se usan las Images que ya tenga el contenedor.
/// Sirve para estrés, depresión o cualquier otra barra.
/// </summary>
public class SegmentedBar : MonoBehaviour
{
    [Header("Segmentos")]
    [Tooltip("Objeto con Horizontal Layout Group donde van los cuadritos.")]
    public RectTransform contenedor;

    [Tooltip("Puede ser:\n" +
             "• Un prefab de FILA con los cuadritos adentro: se crea una vez y se pintan sus cuadritos.\n" +
             "• Un prefab de UN cuadrito: se crean 'Cantidad' copias.\n" +
             "• Vacío: usa los cuadritos que ya estén dentro del contenedor.")]
    public Image prefabSegmento;

    [Tooltip("Cuántos cuadritos crear (solo con prefab de UN cuadrito).")]
    [Min(1)] public int cantidad = 8;

    [Header("Aspecto")]
    [Tooltip("Sprite del cuadrito lleno (opcional; si no, solo cambia el color).")]
    public Sprite spriteLleno;
    [Tooltip("Sprite del cuadrito vacío (opcional).")]
    public Sprite spriteVacio;
    public Color colorLleno = new Color(1f, 0.68f, 0.38f);
    public Color colorVacio = new Color(0.14f, 0.14f, 0.24f);

    [Tooltip("Si está activo, el color de los llenos cambia según qué tan llena está la barra.")]
    public bool colorSegunNivel = false;
    [Tooltip("Color según el nivel (izquierda = vacía, derecha = llena).")]
    public Gradient colorPorNivel = GradienteDefecto();

    [Header("Animación")]
    [Tooltip("Hace un pequeño 'pop' en los cuadritos que cambian.")]
    public bool animar = true;
    public float escalaPulso = 1.35f;
    public float duracionPulso = 0.15f;

    /// <summary>Cuántos cuadritos están llenos ahora.</summary>
    public int Llenos { get; private set; } = -1;

    readonly List<Image> segmentos = new List<Image>();
    bool listo;

    void Awake() => Preparar();

    void Preparar()
    {
        if (listo) return;
        listo = true;

        if (contenedor == null) contenedor = transform as RectTransform;

        // Si ya hay cuadritos puestos a mano dentro del contenedor, se usan esos y se ignora el prefab
        bool hayCuadritos = false;
        for (int i = 0; i < contenedor.childCount; i++)
            if (contenedor.GetChild(i).GetComponent<Image>() != null) { hayCuadritos = true; break; }

        // Prefab de FILA COMPLETA (trae los cuadritos adentro): se crea una sola vez y se usan sus hijos
        if (prefabSegmento != null && !hayCuadritos && TieneCuadritosAdentro(prefabSegmento.transform))
        {
            Image fila = Instantiate(prefabSegmento, contenedor, false);
            fila.name = prefabSegmento.name;
            for (int i = 0; i < fila.transform.childCount; i++)
            {
                Image s = fila.transform.GetChild(i).GetComponent<Image>();
                if (s != null) segmentos.Add(s);
            }
        }
        // Prefab de UN cuadrito: se crean 'cantidad' copias
        else if (prefabSegmento != null && !hayCuadritos)
        {
            // Borra los que hubiera y crea 'cantidad' cuadritos nuevos
            for (int i = contenedor.childCount - 1; i >= 0; i--)
                Destroy(contenedor.GetChild(i).gameObject);

            for (int i = 0; i < cantidad; i++)
            {
                Image s = Instantiate(prefabSegmento, contenedor, false);
                s.name = $"Segmento {i + 1}";
                segmentos.Add(s);
            }
        }
        else
        {
            // Usa las Images que ya estén dentro del contenedor, en orden
            for (int i = 0; i < contenedor.childCount; i++)
            {
                Image s = contenedor.GetChild(i).GetComponent<Image>();
                if (s != null) segmentos.Add(s);
            }
        }
    }

    static bool TieneCuadritosAdentro(Transform t)
    {
        for (int i = 0; i < t.childCount; i++)
            if (t.GetChild(i).GetComponent<Image>() != null) return true;
        return false;
    }

    /// <summary>Llena la barra según un valor de 0 (vacía) a 1 (llena).</summary>
    public void SetValor(float valor01)
    {
        Preparar();
        if (segmentos.Count == 0) return;

        valor01 = Mathf.Clamp01(valor01);
        // Cualquier valor mayor que 0 enciende al menos un cuadrito
        int llenos = valor01 <= 0f ? 0 : Mathf.CeilToInt(valor01 * segmentos.Count);
        Color lleno = colorSegunNivel ? colorPorNivel.Evaluate(valor01) : colorLleno;
        bool primeraVez = Llenos < 0;

        for (int i = 0; i < segmentos.Count; i++)
        {
            bool estaLleno = i < llenos;
            bool cambio = !primeraVez && estaLleno != (i < Llenos);

            Image s = segmentos[i];
            s.color = estaLleno ? lleno : colorVacio;
            if (estaLleno && spriteLleno != null) s.sprite = spriteLleno;
            if (!estaLleno && spriteVacio != null) s.sprite = spriteVacio;

            if (cambio && animar && isActiveAndEnabled) StartCoroutine(Pulso(s.transform));
        }

        Llenos = llenos;
    }

    IEnumerator Pulso(Transform t)
    {
        float mitad = duracionPulso * 0.5f;
        for (float x = 0f; x < duracionPulso; x += Time.unscaledDeltaTime)
        {
            float k = x < mitad ? x / mitad : 1f - (x - mitad) / mitad;
            t.localScale = Vector3.one * Mathf.Lerp(1f, escalaPulso, k);
            yield return null;
        }
        t.localScale = Vector3.one;
    }

    static Gradient GradienteDefecto()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(1f, 0.85f, 0.45f), 0f),
                new GradientColorKey(new Color(1f, 0.6f, 0.3f), 0.5f),
                new GradientColorKey(new Color(0.9f, 0.25f, 0.25f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }
}