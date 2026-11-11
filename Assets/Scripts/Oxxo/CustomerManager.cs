using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

/// <summary>Resumen de cómo le fue al jugador con un cliente.</summary>
public struct CustomerResult
{
    public int total;
    public int pago;
    public int cambioEsperado;
    public int cambioEntregado;
    public int propina;
    public float paciencia01;
    public float ocupacionBolsas;
    public int bolsasUsadas;
    public bool seFueEnojado;
    /// <summary>True si se cansó de esperar en la fila y se fue sin ser atendido.</summary>
    public bool abandonoLaFila;
    public Quirk rasgo;
}

/// <summary>
/// Controla la fila de clientes:
/// - Los clientes llegan cada cierto tiempo y hacen fila (máximo 'maxFila').
/// - El del frente es el que atiendes: sus productos salen a la cinta, paga, das el cambio y embolsas.
/// - Al irse, los demás avanzan un lugar.
/// - Los de la fila también pierden paciencia (más lento); si se les acaba, se van.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    [Header("Clientes (prefabs)")]
    [Tooltip("Prefab del NPC (con Customer). Si hay varios, se elige uno al azar.")]
    public Customer[] prefabsClientes;

    [Tooltip("Lugar del cliente que atiendes (frente de la fila), como Clients_point.")]
    public Transform puntoCliente;

    [Header("Interfaz del cliente en la escena")]
    [Tooltip("Image del Canvas con Image Type = Filled.")]
    public Image barraPaciencia;
    [Tooltip("Globo de diálogo del Canvas.")]
    public GameObject globo;
    [Tooltip("Texto TMP dentro del globo.")]
    public TMP_Text globoTexto;

    [Header("Referencias")]
    public ProductSpawner spawner;
    public CashRegister caja;
    public BagManager bagManager;
    public DragController dragController;

    [Header("Jornada")]
    [Tooltip("Si está activo, los clientes empiezan a llegar solos al iniciar.\nSi usas ShiftManager, él lo desactiva y controla el inicio y el final.")]
    public bool iniciarSolo = true;

    [Header("Fila")]
    [Tooltip("Máximo de clientes en la fila, contando al que atiendes.")]
    [Min(1)] public int maxFila = 5;
    [Tooltip("Clientes que ya vienen llegando al empezar la jornada.")]
    [Min(1)] public int clientesAlIniciar = 2;
    [Tooltip("Segundos entre la llegada de un cliente y el siguiente (sin bono).")]
    [Min(0.5f)] public float intervaloLlegada = 10f;
    [Tooltip("Variación al azar del intervalo (0.3 = ±30%).")]
    [Range(0f, 0.9f)] public float variacionLlegada = 0.3f;
    [Tooltip("Separación entre un lugar de la fila y el siguiente (hacia atrás).\n" +
             "Y negativa = los de atrás quedan más abajo, así el mostrador les tapa donde termina el dibujo.")]
    public Vector3 separacionFila = new Vector3(1.6f, -0.15f, 0f);
    [Tooltip("Tamaño de cada lugar respecto al anterior, para dar profundidad. 1 = todos iguales.")]
    [Range(0.7f, 1f)] public float escalaPorPuesto = 0.93f;
    [Tooltip("Qué tan rápido pierden paciencia los que esperan en la fila (0.4 = 40% de lo normal).")]
    [Range(0f, 1f)] public float pacienciaEnFila = 0.4f;

    [Header("Clientes")]
    [Min(1)] public int productosMin = 2;
    [Min(1)] public int productosMax = 6;
    [Tooltip("Segundos que el cliente se queda diciendo su mensaje antes de irse.")]
    public float tiempoDespedida = 1.2f;

    [Header("Paciencia (segundos)")]
    [Tooltip("Paciencia fija de todo cliente: el tiempo para cobrarle y dar el cambio.")]
    public float pacienciaBase = 10f;
    [Tooltip("Paciencia extra por cada producto (lo que tardas en escanearlo y embolsarlo).")]
    public float pacienciaPorProducto = 3.5f;
    [Tooltip("Tope de paciencia, ya contando su personalidad. Así nadie tiene paciencia de sobra en una jornada de 3 minutos.")]
    public float pacienciaMaxima = 60f;
    public float penalizacionCambioDeMas = 4f;
    public float penalizacionCambioDeMenos = 8f;

    [Header("Pago del cliente")]
    [Tooltip("Billetes con los que paga el cliente (misma unidad que los precios). $1 = 100, $50 = 5000.")]
    public int[] billetesCliente = { 100, 500, 1000, 2000, 5000 };

    [Tooltip("Monedas que el cliente puede agregar a su pago. Pon las MISMAS monedas que tiene tu caja.\n" +
             "En centavos: 5¢ = 5, 10¢ = 10, 25¢ = 25, 50¢ = 50.")]
    public int[] monedasCliente = { 5, 10, 25, 50 };

    [Tooltip("Probabilidad de que el cliente agregue monedas aunque el cambio ya lleve centavos.")]
    [Range(0f, 1f)] public float probAgregarMonedas = 0.4f;

    [Tooltip("Máximo de monedas que agrega de una vez.")]
    [Min(1)] public int maxMonedas = 3;

    [Header("Propina")]
    [Tooltip("Propina máxima como porcentaje del total (0.15 = 15%).")]
    [Range(0f, 1f)] public float porcentajePropina = 0.15f;
    [Tooltip("Multiplicador de propina si dio cambio de más.")]
    [Range(0f, 1f)] public float factorCambioDeMas = 0.5f;
    [Tooltip("Multiplicador de propina si dio cambio de menos.")]
    [Range(0f, 1f)] public float factorCambioDeMenos = 0f;

    [Header("Bono por acomodar la bolsa")]
    [Range(0f, 1f)] public float ocupacionBonoCompleto = 0.85f;
    [Range(0f, 1f)] public float ocupacionBonoParcial = 0.6f;
    [Tooltip("Extra de propina con bolsas bien llenas (0.5 = +50%).")]
    [Range(0f, 2f)] public float bonoBolsaCompleto = 0.5f;
    [Range(0f, 2f)] public float bonoBolsaParcial = 0.2f;

    [Header("Interfaz (opcional)")]
    [Tooltip("Texto para ver ventas y propinas de la jornada.")]
    public TMP_Text textoGanancias;

    public int VentasJornada { get; private set; }
    public int PropinasJornada { get; private set; }
    public int ClientesAtendidos { get; private set; }
    /// <summary>El cliente que se está atendiendo ahora (null entre clientes).</summary>
    public Customer ClienteActual => cliente;
    /// <summary>Clientes en la fila (0 = el del frente).</summary>
    public IReadOnlyList<Customer> Fila => fila;
    /// <summary>True mientras llegan clientes.</summary>
    public bool EnJornada { get; private set; }

    /// <summary>Multiplica las propinas (lo controla ShiftManager según la racha).</summary>
    public float MultiplicadorPropina { get; set; } = 1f;
    /// <summary>Qué tan rápido baja la paciencia de los clientes nuevos (lo controla ShiftManager).</summary>
    public float VelocidadPaciencia { get; set; } = 1f;
    /// <summary>Qué tan seguido llegan clientes: 1.5 = 50% más seguido (lo controla ShiftManager).</summary>
    public float MultiplicadorLlegada { get; set; } = 1f;

    public event Action<CustomerResult> OnClienteTerminado;

    readonly List<Customer> fila = new List<Customer>();
    readonly List<Product> productosCliente = new List<Product>();
    Customer cliente;
    bool atendiendo;
    bool cambioDado;
    int cambioEntregado;

    void Awake()
    {
        // Ahora los productos los pide este manager; el spawner no genera nada por su cuenta
        if (spawner != null) spawner.productosAlIniciar = 0;
    }

    void OnEnable()
    {
        caja.OnProductoRegistrado += AlRegistrarProducto;
        caja.OnCambioEntregado += AlEntregarCambio;
        dragController.OnProductoEnBolsa += AlEmbolsar;
    }

    void OnDisable()
    {
        caja.OnProductoRegistrado -= AlRegistrarProducto;
        caja.OnCambioEntregado -= AlEntregarCambio;
        dragController.OnProductoEnBolsa -= AlEmbolsar;
    }

    void Start()
    {
        if (prefabsClientes == null || prefabsClientes.Length == 0 || puntoCliente == null)
        {
            Debug.LogError("CustomerManager: asigna al menos un prefab de cliente y el Punto Cliente.", this);
            enabled = false;
            return;
        }

        // Sin cliente al frente, la barra y el globo de la escena no se ven
        if (barraPaciencia != null) barraPaciencia.gameObject.SetActive(false);
        if (globo != null) globo.SetActive(false);

        ActualizarTexto();
        if (iniciarSolo) IniciarJornada();
    }

    // ---------- Jornada ----------

    /// <summary>Reinicia los contadores y empieza a llegar gente.</summary>
    public void IniciarJornada()
    {
        StopAllCoroutines();
        VaciarFila();
        LimpiarCaja();

        VentasJornada = 0;
        PropinasJornada = 0;
        ClientesAtendidos = 0;
        EnJornada = true;

        ActualizarTexto();
        StartCoroutine(Llegadas());
        StartCoroutine(Atencion());
    }

    /// <summary>Corta la jornada: se va toda la fila y se limpia todo.</summary>
    public void TerminarJornada()
    {
        EnJornada = false;
        atendiendo = false;
        StopAllCoroutines();

        VaciarFila();
        LimpiarCaja();
    }

    void VaciarFila()
    {
        foreach (var c in fila)
            if (c != null) c.Irse();
        fila.Clear();
        cliente = null;
    }

    void LimpiarCaja()
    {
        LimpiarProductos();
        caja.NuevoCliente();
        bagManager.ReiniciarParaNuevoCliente();
        cambioDado = false;
        cambioEntregado = 0;
    }

    // ---------- Fila ----------

    /// <summary>Va sumando clientes a la fila cada cierto tiempo (más seguido con el bono).</summary>
    IEnumerator Llegadas()
    {
        for (int i = 0; i < clientesAlIniciar && fila.Count < maxFila; i++)
        {
            AgregarALaFila();
            yield return new WaitForSeconds(0.7f);
        }

        while (EnJornada)
        {
            float intervalo = intervaloLlegada / Mathf.Max(0.1f, MultiplicadorLlegada);
            intervalo *= Random.Range(1f - variacionLlegada, 1f + variacionLlegada);
            yield return new WaitForSeconds(intervalo);

            // Si la fila está llena, ese cliente ni entra
            if (EnJornada && fila.Count < maxFila) AgregarALaFila();
        }
    }

    void AgregarALaFila()
    {
        int puesto = fila.Count;
        Customer prefab = prefabsClientes[Random.Range(0, prefabsClientes.Length)];
        Customer c = Instantiate(prefab, PosicionPuesto(puesto), Quaternion.identity);

        // Su apariencia y personalidad ya se randomizaron en Awake (NPCAppearance / CustomerPersonality)
        c.CantidadProductos = Random.Range(productosMin, Mathf.Max(productosMin, productosMax) + 1);
        c.FactorFila = pacienciaEnFila;
        c.velocidad *= VelocidadPaciencia;
        c.SetPuesto(puesto);
        c.OnPacienciaAgotada += () => AlAgotarPaciencia(c);

        fila.Add(c);
        c.Llegar(pacienciaBase + pacienciaPorProducto * c.CantidadProductos, EscalaPuesto(puesto), pacienciaMaxima);
    }

    /// <summary>Saca a un cliente de la fila y los de atrás avanzan un lugar.</summary>
    void SacarDeLaFila(Customer c)
    {
        int indice = fila.IndexOf(c);
        if (indice < 0) return;

        fila.RemoveAt(indice);
        c.Irse();

        for (int i = indice; i < fila.Count; i++)
        {
            fila[i].SetPuesto(i);
            fila[i].CaminarA(PosicionPuesto(i), EscalaPuesto(i));
        }
    }

    Vector3 PosicionPuesto(int puesto)
    {
        Vector3 p = puntoCliente.position + separacionFila * puesto;
        p.z = 0f;
        return p;
    }

    float EscalaPuesto(int puesto) => Mathf.Pow(escalaPorPuesto, puesto);

    // ---------- Atención del cliente del frente ----------

    IEnumerator Atencion()
    {
        while (EnJornada)
        {
            // Espera a que haya alguien al frente y que ya esté parado en el mostrador
            while (EnJornada && (fila.Count == 0 || !fila[0].Llego)) yield return null;
            if (!EnJornada) yield break;

            cliente = fila[0];
            cliente.PasarAlFrente(barraPaciencia, globo, globoTexto);
            productosCliente.AddRange(spawner.GenerarCliente(cliente.CantidadProductos));
            atendiendo = true;

            // Espera a que termine (Terminar pone atendiendo = false)
            while (atendiendo) yield return null;

            // Se despide, se va y la fila avanza
            yield return new WaitForSeconds(tiempoDespedida);
            if (cliente != null) SacarDeLaFila(cliente);
            cliente = null;
            LimpiarCaja();
        }
    }

    void AlRegistrarProducto(Product p)
    {
        if (!atendiendo || caja.ClientePago) return;

        // Cuando se escanea el último producto, el cliente paga
        if (caja.Cantidad >= productosCliente.Count)
        {
            int pago = CalcularPago(caja.Total);
            caja.RecibirPago(pago);
            cliente.Decir($"Pago con {caja.Formatear(pago)}");
        }
    }

    /// <summary>
    /// Genera con cuánto paga el cliente:
    /// - Nunca paga exacto: siempre hay cambio.
    /// - El cambio siempre necesita monedas (no solo billetes).
    /// - El cambio siempre se puede dar con la moneda más chica.
    /// </summary>
    int CalcularPago(int total)
    {
        bool hayBilletes = billetesCliente != null && billetesCliente.Length > 0;
        bool hayMonedas = monedasCliente != null && monedasCliente.Length > 0;

        // 1) Billete: el siguiente múltiplo del billete elegido, siempre MAYOR que el total.
        //    Billete chico = cambio chico; billete grande = cambio grande.
        var personalidad = cliente != null ? cliente.Personalidad : null;

        int billete = hayBilletes ? Mathf.Max(1, billetesCliente[Random.Range(0, billetesCliente.Length)]) : 100;
        if (hayBilletes && personalidad != null && personalidad.PagaConBilleteGrande)
            billete = Max(billetesCliente);
        int pago = (total / billete + 1) * billete;

        // 2) Monedas: si el cambio saldría "redondo" (solo billetes), el cliente agrega monedas.
        //    A veces también las agrega aunque el cambio ya lleve centavos, para variar.
        int billeteMinimo = hayBilletes ? Mathf.Max(1, Min(billetesCliente)) : 100;
        if (hayMonedas)
        {
            bool cambioRedondo = (pago - total) % billeteMinimo == 0;
            bool monedero = personalidad != null && personalidad.PagaConMonedas;
            if (cambioRedondo || monedero || Random.value < probAgregarMonedas)
            {
                // El que paga con monedas agrega más de lo normal
                int cantidad = monedero ? Random.Range(maxMonedas, maxMonedas + 3) : Random.Range(1, maxMonedas + 1);
                for (int i = 0; i < cantidad; i++) pago += MonedaAlAzar();
            }

            // Si con las monedas el cambio volvió a quedar redondo, agrega otra
            for (int intentos = 0; (pago - total) % billeteMinimo == 0 && intentos < 10; intentos++)
                pago += MonedaAlAzar();
        }

        // 3) Seguridad: el cambio debe poder armarse con la moneda más chica
        int minima = hayMonedas ? Mathf.Max(1, Min(monedasCliente)) : billeteMinimo;
        int resto = (pago - total) % minima;
        if (resto != 0) pago += minima - resto;

        return pago;
    }

    int MonedaAlAzar() => Mathf.Max(1, monedasCliente[Random.Range(0, monedasCliente.Length)]);

    static int Max(int[] valores)
    {
        int m = 1;
        foreach (int v in valores) if (v > m) m = v;
        return m;
    }

    static int Min(int[] valores)
    {
        int m = int.MaxValue;
        foreach (int v in valores) if (v > 0 && v < m) m = v;
        return m == int.MaxValue ? 1 : m;
    }

    void AlEntregarCambio(int entregado, int esperado)
    {
        if (!atendiendo) return;

        cambioDado = true;
        cambioEntregado = entregado;
        int diferencia = entregado - esperado;

        if (diferencia == 0)
        {
            cliente.Decir("¡Gracias!");
        }
        else if (diferencia > 0)
        {
            cliente.Decir("Mmm... me diste de más");
            cliente.Penalizar(penalizacionCambioDeMas);
        }
        else
        {
            cliente.Decir("¡Me falta cambio!");
            cliente.Penalizar(penalizacionCambioDeMenos);
        }

        RevisarFin();
    }

    void AlEmbolsar(Product p) => RevisarFin();

    void AlAgotarPaciencia(Customer c)
    {
        if (c == null) return;

        // El que estás atendiendo se va enojado
        if (c == cliente && atendiendo)
        {
            c.Enojarse();
            c.Decir("¡Ya me voy!");
            Terminar(seFueEnojado: true);
            return;
        }

        // Uno que esperaba en la fila se cansó y se va sin comprar
        if (!fila.Contains(c)) return;

        c.Enojarse();
        SacarDeLaFila(c);
        OnClienteTerminado?.Invoke(new CustomerResult
        {
            seFueEnojado = true,
            abandonoLaFila = true,
            rasgo = c.Personalidad != null ? c.Personalidad.Rasgo : Quirk.Ninguno
        });
    }

    /// <summary>El cliente se va contento cuando recibió su cambio y todo está en bolsas.</summary>
    void RevisarFin()
    {
        if (!atendiendo || !cambioDado) return;

        foreach (var p in productosCliente)
            if (p != null && p.BolsaActual == null) return;

        Terminar(seFueEnojado: false);
    }

    void Terminar(bool seFueEnojado)
    {
        atendiendo = false;
        cliente.Detener();

        var r = new CustomerResult
        {
            total = caja.Total,
            pago = caja.Pago,
            cambioEsperado = caja.CambioEsperado,
            cambioEntregado = cambioEntregado,
            paciencia01 = cliente.Paciencia01,
            ocupacionBolsas = bagManager.OcupacionPromedio,
            bolsasUsadas = bagManager.BolsasUsadas,
            seFueEnojado = seFueEnojado,
            rasgo = cliente.Personalidad != null ? cliente.Personalidad.Rasgo : Quirk.Ninguno
        };

        // Venta: solo si se completó el pago. Si diste de más, la tienda pierde ese exceso.
        if (cambioDado)
        {
            int exceso = Mathf.Max(0, cambioEntregado - r.cambioEsperado);
            VentasJornada += r.total - exceso;
            ClientesAtendidos++;
        }

        if (!seFueEnojado)
        {
            r.propina = CalcularPropina(r);
            PropinasJornada += r.propina;
            if (r.cambioEntregado == r.cambioEsperado) cliente.Alegrarse();
            if (r.propina > 0) cliente.Decir($"¡Gracias! Propina {caja.Formatear(r.propina)}");
        }

        ActualizarTexto();
        OnClienteTerminado?.Invoke(r);
        // La corrutina Atencion se encarga de la despedida y de avanzar la fila
    }

    int CalcularPropina(CustomerResult r)
    {
        int diferencia = r.cambioEntregado - r.cambioEsperado;
        float factorCambio = diferencia == 0 ? 1f : diferencia > 0 ? factorCambioDeMas : factorCambioDeMenos;

        float bonoBolsa = r.ocupacionBolsas >= ocupacionBonoCompleto ? bonoBolsaCompleto
                        : r.ocupacionBolsas >= ocupacionBonoParcial ? bonoBolsaParcial
                        : 0f;

        // Personalidad: generoso/tacaño cambian la propina; el ordenado valora el doble la bolsa
        var personalidad = cliente != null ? cliente.Personalidad : null;
        float factorPersonalidad = personalidad != null ? personalidad.MultiplicadorPropina : 1f;
        if (personalidad != null) bonoBolsa *= personalidad.MultiplicadorBonoBolsa;

        float propina = r.total * porcentajePropina * r.paciencia01 * factorCambio * (1f + bonoBolsa) * factorPersonalidad * MultiplicadorPropina;
        return Mathf.RoundToInt(propina);
    }

    void LimpiarProductos()
    {
        foreach (var p in productosCliente)
            if (p != null) Destroy(p.gameObject);
        productosCliente.Clear();
    }

    void ActualizarTexto()
    {
        if (textoGanancias == null) return;
        textoGanancias.text =
            $"Ventas {caja.Formatear(VentasJornada)}\n" +
            $"Propinas {caja.Formatear(PropinasJornada)}\n" +
            $"Clientes {ClientesAtendidos}";
    }
}