using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Un cliente. Va en la RAÍZ del prefab de cada cliente (una Image del Canvas con su dibujo).
/// El CustomerManager crea uno al azar, le da paciencia y lo destruye cuando se va.
/// Como la barra es hija del prefab, cada cliente nuevo trae su barra llena.
/// </summary>
public class Customer : MonoBehaviour
{
    [Header("Partes del prefab")]
    [Tooltip("Image hija con Image Type = Filled. Es la barra de paciencia.")]
    public Image barraPaciencia;

    [Tooltip("Color de la barra según la paciencia (izquierda = 0, derecha = llena).")]
    public Gradient colorBarra = GradienteDefecto();

    [Tooltip("Objeto del globo de diálogo (opcional). Se oculta cuando no hay mensaje.")]
    public GameObject globo;

    [Tooltip("Texto TMP dentro del globo (opcional).")]
    public TMP_Text globoTexto;

    [Header("Personalidad")]
    [Tooltip("1 = normal, 1.5 = muy paciente, 0.7 = apurado. Así cada prefab se siente distinto.")]
    [Min(0.1f)] public float multiplicadorPaciencia = 1f;

    [Header("Dificultad")]
    [Tooltip("Velocidad a la que baja la paciencia. 1 = normal, 1.5 = 50% más rápido.")]
    public float velocidad = 1f;

    public float PacienciaMax { get; private set; }
    public float Paciencia { get; private set; }
    public float Paciencia01 => PacienciaMax > 0f ? Paciencia / PacienciaMax : 0f;
    /// <summary>True mientras la paciencia está corriendo.</summary>
    public bool Esperando { get; private set; }

    public event Action OnPacienciaAgotada;

    void Awake() => Decir("");

    void Update()
    {
        if (!Esperando) return;

        Paciencia -= Time.deltaTime * velocidad;
        RevisarPaciencia();
    }

    /// <summary>El cliente llega con 'segundos' de paciencia (antes de su multiplicador).</summary>
    public void Llegar(float segundos)
    {
        PacienciaMax = Mathf.Max(1f, segundos * multiplicadorPaciencia);
        Paciencia = PacienciaMax;
        Esperando = true;
        ActualizarBarra();
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

    /// <summary>El cliente se va: se destruye.</summary>
    public void Irse()
    {
        Esperando = false;
        Destroy(gameObject);
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