using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// La caja registradora.
/// - Caja cerrada: muestra los productos escaneados y el total.
/// - Caja abierta: muestra total, con cuánto pagó el cliente, el cambio a dar
///   y lo que el jugador va seleccionando con los billetes y monedas.
/// </summary>
public class CashRegister : MonoBehaviour
{
    [Header("Pantalla")]
    [Tooltip("Texto (TextMeshPro) dentro de la pantalla azul.")]
    public TMP_Text pantalla;

    [Tooltip("Cuántos productos recientes se listan en la pantalla.")]
    [Min(1)] public int lineasVisibles = 4;

    [Tooltip("Color del último producto escaneado.")]
    public Color colorUltimo = new Color(0.1f, 0.25f, 0.6f);

    [Header("Colores del cambio")]
    public Color colorFalta = new Color(0.85f, 0.45f, 0f);
    public Color colorExacto = new Color(0.1f, 0.55f, 0.15f);
    public Color colorSobra = new Color(0.85f, 0.15f, 0.15f);

    [Header("Formato de dinero")]
    [Tooltip("Activo: un precio de 2550 se muestra como $25.50.\nInactivo: se muestra como $2550.")]
    public bool preciosEnCentavos = true;
    public string simbolo = "$";

    // --- Eventos ---
    public event Action<Product> OnProductoRegistrado;
    public event Action<int> OnTotalCambiado;
    public event Action<int> OnPagoRecibido;
    /// <summary>(entregado por el jugador, cambio correcto)</summary>
    public event Action<int, int> OnCambioEntregado;

    readonly List<Product> registrados = new List<Product>();
    readonly Stack<int> seleccion = new Stack<int>();

    // --- Estado ---
    public int Total { get; private set; }
    public int Cantidad => registrados.Count;
    public IReadOnlyList<Product> Registrados => registrados;

    public int Pago { get; private set; }
    public bool ClientePago => Pago > 0;
    public int CambioEsperado => Mathf.Max(0, Pago - Total);
    public int Seleccionado { get; private set; }
    public bool CambioEntregado { get; private set; }
    public bool CajaAbierta { get; private set; }

    void Start() => ActualizarPantalla();

    // ---------- Escaneo ----------

    /// <summary>Cobra un producto (si no estaba cobrado ya) y actualiza la pantalla.</summary>
    public bool Registrar(Product p)
    {
        if (p == null || p.Escaneado || ClientePago) return false;

        p.MarcarEscaneado();
        registrados.Add(p);
        Total += p.Precio;

        ActualizarPantalla();
        OnProductoRegistrado?.Invoke(p);
        OnTotalCambiado?.Invoke(Total);
        return true;
    }

    /// <summary>Deja la caja en cero para el siguiente cliente.</summary>
    public void NuevoCliente()
    {
        registrados.Clear();
        seleccion.Clear();
        Total = 0;
        Pago = 0;
        Seleccionado = 0;
        CambioEntregado = false;

        ActualizarPantalla();
        OnTotalCambiado?.Invoke(Total);
    }

    // ---------- Pago ----------

    /// <summary>El cliente entrega su dinero (lo llama CustomerManager).</summary>
    public void RecibirPago(int monto)
    {
        Pago = Mathf.Max(monto, Total);
        seleccion.Clear();
        Seleccionado = 0;
        CambioEntregado = false;

        ActualizarPantalla();
        OnPagoRecibido?.Invoke(Pago);
    }

    public void AbrirCaja() { CajaAbierta = true; ActualizarPantalla(); }
    public void CerrarCaja() { CajaAbierta = false; ActualizarPantalla(); }
    public void AlternarCaja() { if (CajaAbierta) CerrarCaja(); else AbrirCaja(); }

    /// <summary>Lo llaman los botones de billetes y monedas. Sin límite de dinero.</summary>
    public void AgregarDinero(int valor)
    {
        if (!ClientePago || CambioEntregado || valor <= 0) return;

        seleccion.Push(valor);
        Seleccionado += valor;
        ActualizarPantalla();
    }

    /// <summary>Quita el último billete o moneda agregado.</summary>
    public void Deshacer()
    {
        if (CambioEntregado || seleccion.Count == 0) return;

        Seleccionado -= seleccion.Pop();
        ActualizarPantalla();
    }

    /// <summary>Quita todo lo seleccionado.</summary>
    public void Limpiar()
    {
        if (CambioEntregado) return;

        seleccion.Clear();
        Seleccionado = 0;
        ActualizarPantalla();
    }

    /// <summary>Entrega al cliente lo seleccionado. Se evalúa aunque esté mal.</summary>
    public void EntregarCambio()
    {
        if (!ClientePago || CambioEntregado) return;

        CambioEntregado = true;
        ActualizarPantalla();
        OnCambioEntregado?.Invoke(Seleccionado, CambioEsperado);
    }

    // ---------- Pantalla ----------

    /// <summary>Convierte un monto en texto. Usa enteros para no depender del separador decimal del PC.</summary>
    public string Formatear(int monto)
    {
        string signo = monto < 0 ? "-" : "";
        monto = Mathf.Abs(monto);
        if (!preciosEnCentavos) return $"{signo}{simbolo}{monto}";
        return $"{signo}{simbolo}{monto / 100}.{monto % 100:00}";
    }

    void ActualizarPantalla()
    {
        if (pantalla == null) return;

        var sb = new StringBuilder();
        if (CajaAbierta) PantallaPago(sb);
        else PantallaEscaneo(sb);
        pantalla.text = sb.ToString();
    }

    void PantallaEscaneo(StringBuilder sb)
    {
        if (ClientePago)
        {
            sb.Append($"TOTAL ({registrados.Count})<pos=55%>{Formatear(Total)}\n");
            if (CambioEntregado)
            {
                sb.Append("Cambio entregado");
            }
            else
            {
                sb.Append($"PAGA CON<pos=55%>{Formatear(Pago)}\n");
                sb.Append("<b>Abre la caja para dar el cambio</b>");
            }
            return;
        }

        string hexUltimo = ColorUtility.ToHtmlStringRGB(colorUltimo);
        int desde = Mathf.Max(0, registrados.Count - lineasVisibles);
        for (int i = desde; i < registrados.Count; i++)
        {
            var p = registrados[i];
            bool esUltimo = i == registrados.Count - 1;

            if (esUltimo) sb.Append($"<color=#{hexUltimo}>");
            sb.Append(p.Nombre).Append("<pos=55%>").Append(Formatear(p.Precio));
            if (esUltimo) sb.Append("</color>");
            sb.Append('\n');
        }

        if (registrados.Count == 0) sb.Append("Esperando productos...\n");
        sb.Append($"<b>TOTAL ({registrados.Count})<pos=55%>{Formatear(Total)}</b>");
    }

    void PantallaPago(StringBuilder sb)
    {
        sb.Append($"TOTAL<pos=55%>{Formatear(Total)}\n");

        if (!ClientePago)
        {
            sb.Append("El cliente aun no paga");
            return;
        }

        sb.Append($"PAGA CON<pos=55%>{Formatear(Pago)}\n");
        sb.Append($"<b>CAMBIO<pos=55%>{Formatear(CambioEsperado)}</b>\n");

        Color c = Seleccionado < CambioEsperado ? colorFalta
                : Seleccionado == CambioEsperado ? colorExacto
                : colorSobra;
        string etiqueta = CambioEntregado ? "ENTREGADO" : "ENTREGAS";
        sb.Append($"<color=#{ColorUtility.ToHtmlStringRGB(c)}>{etiqueta}<pos=55%>{Formatear(Seleccionado)}</color>");
    }
}