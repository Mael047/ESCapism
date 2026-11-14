using System;
using TMPro;
using UnityEngine;

/// <summary>Resumen de la jornada para pagarle al jugador.</summary>
public struct ShiftResult
{
    public int ventas;
    public int propinas;
    public int clientes;
    public int rachaMaxima;
    public int sueldoBase;
    public int parteDePropinas;
    public int pagoTotal;
}

/// <summary>
/// La jornada del cajero:
/// - Dura 3 minutos (configurable). Al terminar se corta todo y se muestra el pago.
/// - Racha: clientes seguidos sin errores (cambio exacto y sin que se vaya enojado).
///   Cada cierto número de clientes perfectos sube un nivel de bono:
///   propinas más grandes, pero paciencia que baja más rápido y cinta más veloz.
///   Un error reinicia la racha.
/// - Pago: un sueldo base miserable + una parte de las propinas.
/// </summary>
public class ShiftManager : MonoBehaviour
{
    [Header("Referencias")]
    public CustomerManager customerManager;
    [Tooltip("Para dar formato al dinero ($0.00).")]
    public CashRegister caja;
    public DragController dragController;
    [Tooltip("Opcional: la cinta acelera con la racha.")]
    public ConveyorBelt cinta;

    [Header("Tiempo")]
    [Tooltip("Duración de la jornada en segundos (180 = 3 minutos).")]
    [Min(10f)] public float duracion = 180f;
    public bool iniciarAlArrancar = true;
    [Tooltip("En los últimos segundos el timer se pone del color de aviso.")]
    public float segundosAviso = 15f;

    [Header("Racha (ventas seguidas sin errores)")]
    [Tooltip("Clientes perfectos seguidos para subir un nivel de bono.")]
    [Min(1)] public int clientesPorNivel = 2;
    [Min(0)] public int nivelMaximo = 5;
    [Tooltip("Propina extra por nivel (0.25 = +25% por nivel).")]
    public float propinaPorNivel = 0.25f;
    [Tooltip("Qué tan rápido baja la paciencia por nivel (0.12 = +12% por nivel).")]
    public float pacienciaPorNivel = 0.12f;
    [Tooltip("Velocidad extra de la cinta por nivel (0.1 = +10% por nivel).")]
    public float cintaPorNivel = 0.1f;
    [Tooltip("Clientes llegan más seguido por nivel (0.2 = 20% más seguido por nivel).")]
    public float llegadaPorNivel = 0.2f;
    [Tooltip("Si un cliente se cansa de esperar en la fila y se va, ¿cuenta como error y reinicia la racha?")]
    public bool abandonoRompeRacha = true;

    [Header("Pago al terminar")]
    [Tooltip("Sueldo fijo (miserable), en la misma unidad que los precios. 3000 = $30.00")]
    public int sueldoBase = 3000;
    [Tooltip("Parte de las propinas que se queda el jugador (0.7 = 70%).")]
    [Range(0f, 1f)] public float parteDePropinas = 0.7f;
    [Tooltip("Suma el pago al dinero del GameManager al terminar la jornada.")]
    public bool pagarEnGameManager = true;

    [Header("Interfaz")]
    [Tooltip("Texto del reloj (por ejemplo dentro de tu objeto Timer).")]
    public TMP_Text textoTimer;
    [Tooltip("Texto que muestra la racha y el bono actual.")]
    public TMP_Text textoRacha;
    public Color colorTimer = Color.white;
    public Color colorAviso = new Color(1f, 0.3f, 0.3f);
    [Tooltip("Panel que aparece al terminar (empieza oculto).")]
    public GameObject panelResultados;
    public TMP_Text textoResultados;

    [Header("Sonido de tiempo por terminar")]
    [Tooltip("Suena una vez cuando el timer se pone rojo (en los últimos 'Segundos Aviso').")]
    public AudioClip sonidoAviso;
    [Tooltip("Si se deja vacío, usa el AudioSource de este objeto o crea uno (su Volume controla el volumen).")]
    public AudioSource audioAviso;

    /// <summary>Se lanza al terminar la jornada, con el pago. Úsalo desde tu GameManager.</summary>
    public event Action<ShiftResult> OnJornadaTerminada;

    public float TiempoRestante { get; private set; }
    public bool EnCurso { get; private set; }
    public int Racha { get; private set; }
    public int RachaMaxima { get; private set; }
    public int Nivel => Mathf.Min(Racha / clientesPorNivel, nivelMaximo);
    public float MultiplicadorPropina => 1f + Nivel * propinaPorNivel;
    public float VelocidadPaciencia => 1f + Nivel * pacienciaPorNivel;

    float velocidadCintaBase = 3f;

    void Awake()
    {
        // La jornada la controla este script
        if (customerManager != null) customerManager.iniciarSolo = false;
        if (cinta != null) velocidadCintaBase = cinta.velocidad;
        if (panelResultados != null) panelResultados.SetActive(false);

        // Si hay sonido pero no AudioSource, usa el de este objeto o crea uno
        if (audioAviso == null) audioAviso = GetComponent<AudioSource>();
        if (audioAviso == null && sonidoAviso != null)
        {
            audioAviso = gameObject.AddComponent<AudioSource>();
            audioAviso.playOnAwake = false;
        }
    }

    void OnEnable()
    {
        if (customerManager != null) customerManager.OnClienteTerminado += AlTerminarCliente;
    }

    void OnDisable()
    {
        if (customerManager != null) customerManager.OnClienteTerminado -= AlTerminarCliente;
    }

    void Start()
    {
        TiempoRestante = duracion;
        ActualizarTimer();
        ActualizarRacha();
        if (iniciarAlArrancar) IniciarJornada();
    }

    /// <summary>Empieza (o reinicia) la jornada. Puedes llamarlo desde un botón "Jugar otra vez".</summary>
    public void IniciarJornada()
    {
        TiempoRestante = duracion;
        Racha = 0;
        RachaMaxima = 0;
        EnCurso = true;
        avisoSonado = false;

        if (panelResultados != null) panelResultados.SetActive(false);
        if (dragController != null) dragController.enabled = true;

        AplicarBonos();
        customerManager.IniciarJornada();

        ActualizarTimer();
        ActualizarRacha();
    }

    void Update()
    {
        if (!EnCurso) return;

        TiempoRestante -= Time.deltaTime;
        if (TiempoRestante <= 0f)
        {
            TiempoRestante = 0f;
            TerminarJornada();
        }
        ActualizarTimer();

        // Suena una sola vez en el instante en que el timer se pone rojo
        if (EnCurso && !avisoSonado && TiempoRestante <= segundosAviso)
        {
            avisoSonado = true;
            if (audioAviso != null && sonidoAviso != null)
            {
                audioAviso.clip = sonidoAviso;
                audioAviso.Play();
            }
        }
    }

    bool avisoSonado;

    void AlTerminarCliente(CustomerResult r)
    {
        if (!EnCurso) return;
        if (r.abandonoLaFila && !abandonoRompeRacha) return;

        bool perfecto = !r.seFueEnojado && r.cambioEntregado == r.cambioEsperado;
        if (perfecto)
        {
            Racha++;
            RachaMaxima = Mathf.Max(RachaMaxima, Racha);
        }
        else
        {
            Racha = 0;
        }

        // El bono nuevo se aplica desde el siguiente cliente
        AplicarBonos();
        ActualizarRacha();
    }

    void AplicarBonos()
    {
        customerManager.MultiplicadorPropina = MultiplicadorPropina;
        customerManager.VelocidadPaciencia = VelocidadPaciencia;
        customerManager.MultiplicadorLlegada = 1f + Nivel * llegadaPorNivel;
        if (cinta != null) cinta.velocidad = velocidadCintaBase * (1f + Nivel * cintaPorNivel);
    }

    void TerminarJornada()
    {
        EnCurso = false;
        if (audioAviso != null && audioAviso.clip == sonidoAviso) audioAviso.Stop();
        if (dragController != null) dragController.enabled = false;
        customerManager.TerminarJornada();

        int propinas = customerManager.PropinasJornada;
        int parte = Mathf.RoundToInt(propinas * parteDePropinas);

        var r = new ShiftResult
        {
            ventas = customerManager.VentasJornada,
            propinas = propinas,
            clientes = customerManager.ClientesAtendidos,
            rachaMaxima = RachaMaxima,
            sueldoBase = sueldoBase,
            parteDePropinas = parte,
            pagoTotal = sueldoBase + parte
        };

        // Le paga al jugador en el GameManager (persiste entre escenas)
        if (pagarEnGameManager && GameManager.Instance != null)
            GameManager.Instance.AddMoney(r.pagoTotal);

        MostrarResultados(r);
        OnJornadaTerminada?.Invoke(r);
    }

    /// <summary>Para el botón "Continuar" del panel de resultados: pasa a la noche.</summary>
    public void Continuar()
    {
        if (GameManager.Instance != null) GameManager.Instance.StartNight();
    }

    // ---------- Interfaz ----------

    void ActualizarTimer()
    {
        if (textoTimer == null) return;

        int segundos = Mathf.CeilToInt(TiempoRestante);
        textoTimer.text = $"{segundos / 60}:{segundos % 60:00}";
        textoTimer.color = EnCurso && TiempoRestante <= segundosAviso ? colorAviso : colorTimer;
    }

    void ActualizarRacha()
    {
        if (textoRacha == null) return;

        if (Nivel == 0)
        {
            int faltan = clientesPorNivel - Racha;
            textoRacha.text = $"Racha {Racha}\n<size=70%>{faltan} más para bono</size>";
        }
        else
        {
            int extra = Mathf.RoundToInt((MultiplicadorPropina - 1f) * 100f);
            textoRacha.text = $"Racha {Racha}\n<size=70%>Nivel {Nivel}: +{extra}% propinas</size>";
        }
    }

    void MostrarResultados(ShiftResult r)
    {
        if (panelResultados != null) panelResultados.SetActive(true);
        if (textoResultados == null) return;

        int porcentaje = Mathf.RoundToInt(parteDePropinas * 100f);
        textoResultados.text =
            "<b>JORNADA TERMINADA</b>\n\n" +
            $"Clientes atendidos<pos=65%>{r.clientes}\n" +
            $"Ventas<pos=65%>{F(r.ventas)}\n" +
            $"Racha máxima<pos=65%>{r.rachaMaxima}\n\n" +
            $"Propinas<pos=65%>{F(r.propinas)}\n" +
            $"Tu parte ({porcentaje}%)<pos=65%>{F(r.parteDePropinas)}\n" +
            $"Sueldo base<pos=65%>{F(r.sueldoBase)}\n\n" +
            $"<b>TU PAGO<pos=65%>{F(r.pagoTotal)}</b>";
    }

    string F(int monto) => caja != null ? caja.Formatear(monto) : monto.ToString();
}