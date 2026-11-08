using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Un cliente. Va en la RAÍZ del prefab del NPC, junto a NPCAppearance y CustomerPersonality.
/// - Su paciencia baja con el tiempo; cuánta tiene depende de sus productos y de su personalidad.
/// - Su cara (NPCAppearance) cambia sola: Feliz → Serio → Enojado según la paciencia.
/// - La barra y el globo pueden venir en el prefab o ser de la escena (los pasa el CustomerManager).
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
    /// <summary>True cuando ya llegó al mostrador (terminó de entrar caminando).</summary>
    public bool Llego { get; private set; }

    CustomerWalk caminata;
    public float PacienciaMax { get; private set; }
    public float Paciencia { get; private set; }
    public float Paciencia01 => PacienciaMax > 0f ? Paciencia / PacienciaMax : 0f;
    /// <summary>True mientras la paciencia está corriendo.</summary>
    public bool Esperando { get; private set; }

    public event Action OnPacienciaAgotada;

    void Awake()
    {
        if (apariencia == null) apariencia = GetComponent<NPCAppearance>();
        Personalidad = GetComponent<CustomerPersonality>();
        caminata = GetComponent<CustomerWalk>();
    }

    void Update()
    {
        if (!Esperando) return;

        Paciencia -= Time.deltaTime * velocidad;
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
    /// El cliente llega con 'segundos' de paciencia (antes de su personalidad).
    /// Si tiene CustomerWalk, primero entra caminando y la paciencia empieza al llegar al mostrador.
    /// </summary>
    public void Llegar(float segundos)
    {
        float multiplicador = Personalidad != null ? Personalidad.MultiplicadorPaciencia : 1f;
        PacienciaMax = Mathf.Max(1f, segundos * multiplicador);
        Paciencia = PacienciaMax;
        Esperando = false;
        Llego = false;
        ActualizarAnimo();

        if (caminata != null) caminata.Entrar(AlLlegarAlMostrador);
        else AlLlegarAlMostrador();
    }

    void AlLlegarAlMostrador()
    {
        Llego = true;
        Esperando = true;

        if (barraPaciencia != null) barraPaciencia.gameObject.SetActive(true);
        ActualizarBarra();
        ActualizarAnimo();

        Decir(Personalidad != null ? Personalidad.Saludo : "");
    }

    /// <summary>Resta paciencia (por ejemplo, por un cambio mal dado).</summary>
    public void Penalizar(float segundos)
    {
        if (!Esperando) return;

        Paciencia -= segundos;
        RevisarPaciencia();
    }

    /// <summary>Deja de perder paciencia (ya terminó), pero sigue en pantalla.</summary>
    public void Detener() => Esperando = false;

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
    /// El cliente se va: oculta la interfaz, sale caminando (si tiene CustomerWalk) y se destruye.
    /// Devuelve los segundos que tarda en salir.
    /// </summary>
    public float Irse()
    {
        Esperando = false;
        if (barraPaciencia != null) barraPaciencia.gameObject.SetActive(false);
        Decir("");

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
        ActualizarBarra();
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