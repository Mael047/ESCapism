using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Arrastrar y soltar productos hacia la bolsa activa.
/// Clic izquierdo: tomar y soltar. Clic derecho o R (mientras arrastras): rotar.
/// Funciona con el Input System nuevo o con el antiguo.
/// Pon uno solo en la escena.
/// </summary>
public class DragController : MonoBehaviour
{
    [Header("Referencias")]
    public Camera camara;
    public BagManager bagManager;

    [Header("Ajustes")]
    [Tooltip("Capas en las que están los productos.")]
    public LayerMask capaProductos = ~0;

    [Tooltip("Distancia alrededor de la bolsa desde la que el producto se imanta a la grilla.")]
    public float margenIman = 0.5f;

    [Tooltip("Si está activo, la bolsa solo acepta productos que ya pasaron por el lector.")]
    public bool soloEscaneadosEnBolsa = true;

    public Color colorValido = new Color(0.55f, 1f, 0.55f);
    public Color colorInvalido = new Color(1f, 0.5f, 0.5f);

    /// <summary>Se lanza cuando un producto queda colocado en una bolsa.</summary>
    public event Action<Product> OnProductoEnBolsa;
    /// <summary>Se lanza al empezar a arrastrar un producto.</summary>
    public event Action<Product> OnProductoTomado;
    /// <summary>Se lanza cuando un producto se suelta en un lugar inválido y vuelve a donde estaba.</summary>
    public event Action<Product> OnProductoDevuelto;

    public Product Arrastrado { get; private set; }

    Vector3 offset;
    Vector2Int origenPreview;
    bool previewValido;

    void Awake()
    {
        if (camara == null) camara = Camera.main;
    }

    void Update()
    {
        Vector3 puntero = PunteroEnMundo();

        if (Arrastrado == null)
        {
            if (PresionoClic() && !PunteroSobreUI()) IntentarTomar(puntero);
            return;
        }

        if (PresionoRotar()) Arrastrado.Rotar();
        Mover(puntero);
        if (SoltoClic()) Soltar();
    }

    void IntentarTomar(Vector3 puntero)
    {
        // Si hay varios productos encimados, toma el que se dibuja más arriba
        Product elegido = null;
        int mejorOrden = int.MinValue;
        foreach (var col in Physics2D.OverlapPointAll(puntero, capaProductos))
        {
            var p = col.GetComponentInParent<Product>();
            if (p == null || !p.arrastrable) continue;

            int orden = p.SpriteRenderer != null ? p.SpriteRenderer.sortingOrder : 0;
            if (orden > mejorOrden) { mejorOrden = orden; elegido = p; }
        }
        if (elegido == null) return;

        Arrastrado = elegido;
        elegido.GuardarEstado();
        if (elegido.BolsaActual != null) elegido.BolsaActual.Quitar(elegido);
        elegido.transform.SetParent(null, true);
        elegido.Elevar(true);

        offset = elegido.transform.position - puntero;
        offset.z = 0f;

        OnProductoTomado?.Invoke(elegido);
    }

    void Mover(Vector3 puntero)
    {
        Vector3 destino = puntero + offset;
        destino.z = Arrastrado.transform.position.z;

        previewValido = false;
        BagGrid bolsa = bagManager != null ? bagManager.BolsaActual : null;

        if (bolsa != null && bolsa.EstaCerca(destino, margenIman))
        {
            origenPreview = bolsa.OrigenDesdePosicion(Arrastrado, destino);
            bool escaneadoOk = !soloEscaneadosEnBolsa || Arrastrado.Escaneado;
            previewValido = escaneadoOk && bolsa.PuedeColocar(Arrastrado, origenPreview);
            Arrastrado.Tintar(previewValido ? colorValido : colorInvalido);

            // Si cabe, se imanta a la celda para que el jugador vea dónde quedará
            if (previewValido)
            {
                Vector3 encajada = bolsa.PosicionProducto(Arrastrado, origenPreview);
                destino = new Vector3(encajada.x, encajada.y, destino.z);
            }
        }
        else
        {
            Arrastrado.QuitarTinte();
        }

        Arrastrado.transform.position = destino;
    }

    void Soltar()
    {
        Product p = Arrastrado;
        Arrastrado = null;
        p.QuitarTinte();
        p.Elevar(false);

        BagGrid bolsa = bagManager != null ? bagManager.BolsaActual : null;
        if (previewValido && bolsa != null && bolsa.Colocar(p, origenPreview))
        {
            OnProductoEnBolsa?.Invoke(p);
            return;
        }

        // Lugar inválido: vuelve a donde estaba (cinta o su lugar anterior en la bolsa)
        p.RestaurarEstado();
        OnProductoDevuelto?.Invoke(p);
    }

    // --- Entrada (Input System nuevo o antiguo) ---

    Vector2 PosicionPantalla()
    {
#if ENABLE_INPUT_SYSTEM
        return Pointer.current != null ? Pointer.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    Vector3 PunteroEnMundo()
    {
        Vector2 pantalla = PosicionPantalla();
        float distancia = -camara.transform.position.z; // distancia al plano z = 0
        Vector3 mundo = camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, distancia));
        mundo.z = 0f;
        return mundo;
    }

    bool PresionoClic()
    {
#if ENABLE_INPUT_SYSTEM
        return Pointer.current != null && Pointer.current.press.wasPressedThisFrame;
#else
        return Input.GetMouseButtonDown(0);
#endif
    }

    bool SoltoClic()
    {
#if ENABLE_INPUT_SYSTEM
        return Pointer.current != null && Pointer.current.press.wasReleasedThisFrame;
#else
        return Input.GetMouseButtonUp(0);
#endif
    }

    bool PresionoRotar()
    {
#if ENABLE_INPUT_SYSTEM
        return (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            || (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame);
#else
        return Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.R);
#endif
    }

    // Solo bloquea el arrastre si el puntero está sobre un BOTÓN (u otro control interactivo),
    // no sobre imágenes de fondo del Canvas.
    readonly List<RaycastResult> resultadosUI = new List<RaycastResult>();

    bool PunteroSobreUI()
    {
        if (EventSystem.current == null) return false;

        var datos = new PointerEventData(EventSystem.current) { position = PosicionPantalla() };
        resultadosUI.Clear();
        EventSystem.current.RaycastAll(datos, resultadosUI);

        foreach (var r in resultadosUI)
            if (r.gameObject.GetComponentInParent<Selectable>() != null) return true;
        return false;
    }
}