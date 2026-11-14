using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Un cliente. Va en la RAÍZ del prefab del NPC, junto a NPCAppearance, CustomerPersonality y CustomerWalk.
/// - Entra caminando a su lugar en la fila y avanza cuando se libera el de adelante.
/// - Su paciencia baja con el tiempo: más lento mientras espera en la fila, normal cuando lo atienden.
/// - Su cara (NPCAppearance) cambia sola: Feliz → Serio → Enojado según la paciencia.
/// - Solo el cliente del frente usa la barra y el globo de la escena.
/// </summary>
public class Customer : MonoBehaviour
{
    [Header("Interfaz (opcional: si se deja vacía, usa la de la escena)")]
    [Tooltip("Image con Image Type = Filled. Es la barra de paciencia.")]
    public Image barraPaciencia;

    [Tooltip("Color de la barra según la paciencia (izquierda = 0, derecha = llena).")]
    public Gradient colorBarra = GradienteDefecto();

    [Tooltip("Objeto del globo de diálogo. Se oculta cuando no hay mensaje.")]
    public GameObject globo;

    [Tooltip("Texto TMP dentro del globo.")]
    public TMP_Text globoTexto;

    [Header("Ánimo (expresión de la cara)")]
    [Tooltip("Si se deja vacío, busca NPCAppearance en este mismo objeto.")]
    public NPCAppearance apariencia;

    [Tooltip("Por debajo de esta paciencia (0-1) se pone serio.")]
    [Range(0f, 1f)] public float umbralSerio = 0.6f;

    [Tooltip("Por debajo de esta paciencia (0-1) se enoja.")]
    [Range(0f, 1f)] public float umbralEnojado = 0.3f;

    [Header("Dificultad")]
    [Tooltip("Velocidad a la que baja la paciencia. 1 = normal, 1.5 = 50% más rápido.")]
    public float velocidad = 1f;

    public CustomerPersonality Personalidad { get; private set; }
    /// <summary>True cuando está quieto en su lugar (no caminando).</summary>
    public bool Llego { get; private set; }
    /// <summary>True cuando es el cliente que está atendiendo el jugador.</summary>
    public bool AlFrente { get; private set; }
    /// <summary>Qué tan rápido pierde paciencia mientras espera en la fila (lo pone CustomerManager).</summary>
    public float FactorFila { get; set; } = 1f;
    /// <summary>Cuántos productos trae (lo decide CustomerManager al crearlo).</summary>
    public int CantidadProductos { get; set; }

    /// <summary>Cómo se siente ahora: la cara del NPC, o según su paciencia si no tiene NPCAppearance.</summary>
    public Mood AnimoActual =>
        apariencia != null ? apariencia.CurrentMood
        : Paciencia01 <= umbralEnojado ? Mood.Angry
        : Paciencia01 <= umbralSerio ? Mood.Serious
        : Mood.Happy;

    public float PacienciaMax { get; private set; }
    public float Paciencia { get; private set; }
    public float Paciencia01 => PacienciaMax > 0f ? Paciencia / PacienciaMax : 0f;
    /// <summary>True mientras la paciencia está corriendo.</summary>
    public bool Esperando { get; private set; }

    public event Action OnPacienciaAgotada;

    CustomerWalk caminata;
    SortingGroup grupoOrden;
    int ordenBase;
    bool detenido;

    void Awake()
    {
        if (apariencia == null) apariencia = GetComponent<NPCAppearance>();
        Personalidad = GetComponent<CustomerPersonality>();
        caminata = GetComponent<CustomerWalk>();
        grupoOrden = GetComponent<SortingGroup>();
        if (grupoOrden != null) ordenBase = grupoOrden.sortingOrder;
    }

    void Update()
    {
        if (!Esperando) return;

        float factor = AlFrente ? 1f : FactorFila;
        Paciencia -= Time.deltaTime * velocidad * factor;
        RevisarPaciencia();
    }

    /// <summary>Usa la barra y el globo de la escena para lo que el prefab no traiga.</summary>
    public void UsarInterfaz(Image barra, GameObject globoEscena, TMP_Text textoEscena)
    {
        if (barraPaciencia == null) barraPaciencia = barra;
        if (globo == null) globo = globoEscena;
        if (globoTexto == null) globoTexto = textoEscena;
    }

    /// <summary>
    /// Llega a la fila con 'segundos' de paciencia (antes de su personalidad).
    /// Entra caminando hasta su posición actual; la paciencia empieza al llegar.
    /// </summary>
    public void Llegar(float segundos, float escala = 1f, float maximo = float.MaxValue)
    {
        float multiplicador = Personalidad != null ? Personalidad.MultiplicadorPaciencia : 1f;
        PacienciaMax = Mathf.Clamp(segundos * multiplicador, 1f, Mathf.Max(1f, maximo));
        Paciencia = PacienciaMax;
        Esperando = false;
        Llego = false;
        detenido = false;
        ActualizarAnimo();

        if (caminata != null) caminata.Entrar(escala, AlLlegarASuLugar);
        else AlLlegarASuLugar();
    }

    /// <summary>Camina a otro lugar (avanzar en la fila). La paciencia sigue corriendo.</summary>
    public void CaminarA(Vector3 destino, float escala)
    {
        Llego = false;
        if (caminata != null)
        {
            caminata.MoverA(destino, escala, AlLlegarASuLugar);
        }
        else
        {
            transform.position = destino;
            AlLlegarASuLugar();
        }
    }

    void AlLlegarASuLugar()
    {
        Llego = true;
        if (!detenido) Esperando = true;
        ActualizarAnimo();
        if (AlFrente) ActualizarBarra();
    }

    /// <summary>Le toca: usa la barra y el globo de la escena y saluda.</summary>
    public void PasarAlFrente(Image barra, GameObject globoEscena, TMP_Text textoEscena)
    {
        AlFrente = true;
        UsarInterfaz(barra, globoEscena, textoEscena);

        if (barraPaciencia != null) barraPaciencia.gameObject.SetActive(true);
        ActualizarBarra();
        Decir(Personalidad != null ? Personalidad.Saludo : "");
    }

    /// <summary>Lo dibuja más atrás según su lugar en la fila (0 = frente).</summary>
    public void SetPuesto(int puesto)
    {
        if (grupoOrden != null) grupoOrden.sortingOrder = ordenBase - puesto;
    }

    /// <summary>Resta paciencia (por ejemplo, por un cambio mal dado).</summary>
    public void Penalizar(float segundos)
    {
        if (!Esperando) return;

        Paciencia -= segundos;
        RevisarPaciencia();
    }

    /// <summary>Deja de perder paciencia (ya terminó), pero sigue en pantalla.</summary>
    public void Detener()
    {
        detenido = true;
        Esperando = false;
    }

    /// <summary>Se va contento: la cara mejora un nivel.</summary>
    public void Alegrarse()
    {
        if (apariencia != null) apariencia.ImproveMood();
    }

    /// <summary>Se va molesto: cara enojada.</summary>
    public void Enojarse()
    {
        if (apariencia != null) apariencia.SetMood(Mood.Angry);
    }

    /// <summary>
    /// El cliente se va: sale caminando (si tiene CustomerWalk) y se destruye.
    /// Solo oculta la barra y el globo si era el del frente (los demás no los usan).
    /// Devuelve los segundos que tarda en salir.
    /// </summary>
    public float Irse()
    {
        Detener();
        if (AlFrente)
        {
            if (barraPaciencia != null) barraPaciencia.gameObject.SetActive(false);
            Decir("");
        }

        if (caminata != null)
            return caminata.Salir(() => Destroy(gameObject));

        Destroy(gameObject);
        return 0f;
    }

    public void Decir(string mensaje)
    {
        bool hayMensaje = !string.IsNullOrEmpty(mensaje);
        if (globoTexto != null) globoTexto.text = mensaje;
        if (globo != null) globo.SetActive(hayMensaje);
    }

    void RevisarPaciencia()
    {
        Paciencia = Mathf.Max(0f, Paciencia);
        if (AlFrente) ActualizarBarra();
        ActualizarAnimo();

        if (Paciencia <= 0f && Esperando)
        {
            Esperando = false;
            OnPacienciaAgotada?.Invoke();
        }
    }

    void ActualizarBarra()
    {
        if (barraPaciencia == null) return;

        barraPaciencia.fillAmount = Paciencia01;
        barraPaciencia.color = colorBarra.Evaluate(Paciencia01);
    }

    void ActualizarAnimo()
    {
        if (apariencia == null) return;

        Mood animo = Paciencia01 <= umbralEnojado ? Mood.Angry
                   : Paciencia01 <= umbralSerio ? Mood.Serious
                   : Mood.Happy;

        if (animo != apariencia.CurrentMood) apariencia.SetMood(animo);
    }

    static Gradient GradienteDefecto()
    {
        var g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.9f, 0.25f, 0.25f), 0f),
                new GradientColorKey(new Color(1f, 0.8f, 0.2f), 0.5f),
                new GradientColorKey(new Color(0.3f, 0.85f, 0.35f), 1f)
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }
}