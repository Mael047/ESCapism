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
    public Quirk rasgo;
}

/// <summary>
/// Controla el ciclo de cada cliente:
/// crea un cliente al azar con sus productos → se escanean → paga →
/// el jugador da el cambio y embolsa → se va (se destruye) → llega otro.
/// Si se le acaba la paciencia, se va enojado.
/// </summary>
public class CustomerManager : MonoBehaviour
{
    [Header("Clientes (prefabs)")]
    [Tooltip("Prefabs de clientes. Con un solo NPC randomizado basta; si hay varios, se elige uno al azar.")]
    public Customer[] prefabsClientes;

    [Tooltip("Punto donde aparece el cliente (puede ser un objeto del Canvas, como Clients_point).")]
    public Transform puntoCliente;

    [Header("Interfaz del cliente en la escena (si el prefab no la trae)")]
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

    [Header("Clientes")]
    [Min(1)] public int productosMin = 2;
    [Min(1)] public int productosMax = 6;
    [Tooltip("Segundos entre que se va un cliente y llega el siguiente.")]
    public float esperaEntreClientes = 1.5f;
    [Tooltip("Segundos que el cliente se queda diciendo su mensaje antes de irse.")]
    public float tiempoDespedida = 1.2f;

    [Header("Paciencia (segundos)")]
    public float pacienciaBase = 20f;
    [Tooltip("Paciencia extra por cada producto que lleva.")]
    public float pacienciaPorProducto = 6f;
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
    /// <summary>True mientras llegan clientes.</summary>
    public bool EnJornada { get; private set; }

    /// <summary>Multiplica las propinas (lo controla ShiftManager según la racha).</summary>
    public float MultiplicadorPropina { get; set; } = 1f;
    /// <summary>Qué tan rápido baja la paciencia de los clientes nuevos (lo controla ShiftManager).</summary>
    public float VelocidadPaciencia { get; set; } = 1f;

    public event Action<CustomerResult> OnClienteTerminado;

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

        // Sin cliente, la barra y el globo de la escena no se ven
        if (barraPaciencia != null) barraPaciencia.gameObject.SetActive(false);
        if (globo != null) globo.SetActive(false);

        ActualizarTexto();
        if (iniciarSolo) IniciarJornada();
    }

    // ---------- Jornada ----------

    /// <summary>Reinicia los contadores y empieza a llamar clientes.</summary>
    public void IniciarJornada()
    {
        StopAllCoroutines();
        QuitarClienteActual();

        VentasJornada = 0;
        PropinasJornada = 0;
        ClientesAtendidos = 0;
        EnJornada = true;

        ActualizarTexto();
        StartCoroutine(SiguienteCliente(0.5f));
    }

    /// <summary>Corta la jornada: se va el cliente actual y se limpia todo.</summary>
    public void TerminarJornada()
    {
        EnJornada = false;
        atendiendo = false;
        StopAllCoroutines();

        QuitarClienteActual();
        LimpiarProductos();
        caja.NuevoCliente();
        bagManager.ReiniciarParaNuevoCliente();
    }

    void QuitarClienteActual()
    {
        if (cliente == null) return;
        cliente.OnPacienciaAgotada -= AlAgotarPaciencia;
        cliente.Irse();
        cliente = null;
    }

    // ---------- Ciclo del cliente ----------

    IEnumerator SiguienteCliente(float espera)
    {
        atendiendo = false;
        yield return new WaitForSeconds(espera);
        if (!EnJornada) yield break;

        LimpiarProductos();
        caja.NuevoCliente();
        bagManager.ReiniciarParaNuevoCliente();
        cambioDado = false;
        cambioEntregado = 0;

        int cantidad = Random.Range(productosMin, Mathf.Max(productosMin, productosMax) + 1);
        CrearCliente(cantidad);

        // Espera a que el cliente llegue caminando al mostrador antes de poner sus productos
        while (cliente != null && !cliente.Llego) yield return null;
        if (!EnJornada || cliente == null) yield break;

        productosCliente.AddRange(spawner.GenerarCliente(cantidad));
        atendiendo = true;
    }

    void CrearCliente(int cantidadProductos)
    {
        Customer prefab = prefabsClientes[Random.Range(0, prefabsClientes.Length)];

        if (prefab.transform is RectTransform)
        {
            // Cliente hecho con UI (Image): va dentro del punto del Canvas
            cliente = Instantiate(prefab, puntoCliente, false);
        }
        else
        {
            // Cliente hecho con SpriteRenderers (NPC): va en el mundo, en la posición del punto
            Vector3 pos = puntoCliente.position;
            cliente = Instantiate(prefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity);
        }

        // Su apariencia y personalidad ya se randomizaron en Awake (NPCAppearance / CustomerPersonality)
        cliente.UsarInterfaz(barraPaciencia, globo, globoTexto);
        cliente.velocidad *= VelocidadPaciencia;
        cliente.OnPacienciaAgotada += AlAgotarPaciencia;
        cliente.Llegar(pacienciaBase + pacienciaPorProducto * cantidadProductos);
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

    void AlAgotarPaciencia()
    {
        if (!atendiendo) return;

        cliente.Enojarse();
        cliente.Decir("¡Ya me voy!");
        Terminar(seFueEnojado: true);
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
        StartCoroutine(Despedida());
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

    IEnumerator Despedida()
    {
        yield return new WaitForSeconds(tiempoDespedida);

        if (cliente != null)
        {
            cliente.OnPacienciaAgotada -= AlAgotarPaciencia;
            cliente.Irse();
            cliente = null;
        }

        yield return SiguienteCliente(esperaEntreClientes);
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