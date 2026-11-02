using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cinta transportadora. Los productos forman una fila desde el escáner hacia atrás.
/// - Cuando el jugador toma uno, los de atrás avanzan para ocupar el espacio libre.
/// - Los que no caben esperan ocultos al final de la cinta y van entrando cuando hay lugar.
/// - Si el jugador suelta un producto en un lugar inválido, vuelve al frente de la cinta.
/// </summary>
public class ConveyorBelt : MonoBehaviour
{
    [Header("Puntos")]
    [Tooltip("Frente de la cinta, junto al escáner. Aquí queda el primer producto.")]
    public Transform puntoInicio;

    [Tooltip("Final visible de la cinta. Lo que no cabe espera oculto detrás de este punto.")]
    public Transform puntoFin;

    [Header("Movimiento")]
    [Tooltip("Velocidad de la cinta en unidades por segundo.")]
    public float velocidad = 3f;

    [Tooltip("Separación entre productos.")]
    public float espacioEntreProductos = 0.15f;

    [Header("Referencias")]
    public DragController dragController;

    readonly List<Product> cola = new List<Product>();

    /// <summary>Productos en la cinta, del frente hacia atrás.</summary>
    public IReadOnlyList<Product> Productos => cola;

    float Largo => Mathf.Abs(puntoFin.position.x - puntoInicio.position.x);
    float Direccion => puntoFin.position.x >= puntoInicio.position.x ? 1f : -1f;

    void OnEnable()
    {
        if (dragController == null) return;
        dragController.OnProductoTomado += AlTomar;
        dragController.OnProductoDevuelto += AlDevolver;
    }

    void OnDisable()
    {
        if (dragController == null) return;
        dragController.OnProductoTomado -= AlTomar;
        dragController.OnProductoDevuelto -= AlDevolver;
    }

    /// <summary>Pone un producto al final de la fila. Entra desde atrás de la cinta.</summary>
    public void Agregar(Product p)
    {
        if (p == null || cola.Contains(p)) return;

        // Distancia a la que quedará su centro
        float acumulado = 0f;
        foreach (var otro in cola)
            if (otro != null) acumulado += Ancho(otro) + espacioEntreProductos;
        float centro = acumulado + Ancho(p) * 0.5f;

        cola.Add(p);

        // Empieza un largo de cinta más atrás y avanza hasta su lugar
        p.transform.position = Posicion(centro + Largo, p);
    }

    void Update()
    {
        if (puntoInicio == null || puntoFin == null) return;

        cola.RemoveAll(p => p == null);

        float acumulado = 0f;
        foreach (var p in cola)
        {
            float ancho = Ancho(p);
            bool cabe = acumulado + ancho <= Largo + 0.001f;
            float centro = acumulado + ancho * 0.5f;
            acumulado += ancho + espacioEntreProductos;

            if (cabe)
            {
                Vector3 objetivo = Posicion(centro, p);
                p.transform.position = Vector3.MoveTowards(p.transform.position, objetivo, velocidad * Time.deltaTime);
            }
            else
            {
                // Espera justo detrás del final, oculto, listo para entrar
                p.transform.position = Posicion(Largo + ancho * 0.5f, p);
            }

            Mostrar(p, cabe);
        }
    }

    void AlTomar(Product p)
    {
        // Sale de la cinta: los demás avanzan solos en el siguiente Update
        cola.Remove(p);
    }

    void AlDevolver(Product p)
    {
        // Si no quedó en una bolsa, vuelve al frente de la cinta
        if (p != null && p.BolsaActual == null && !cola.Contains(p))
            cola.Insert(0, p);
    }

    Vector3 Posicion(float distanciaDesdeInicio, Product p)
    {
        Vector3 inicio = puntoInicio.position;
        return new Vector3(inicio.x + Direccion * distanciaDesdeInicio, inicio.y, p.transform.position.z);
    }

    static float Ancho(Product p)
    {
        var sr = p.SpriteRenderer;
        if (sr == null || sr.sprite == null) return 1f;
        // En la cinta los productos no están rotados
        return sr.sprite.bounds.size.x * Mathf.Abs(p.transform.lossyScale.x);
    }

    static void Mostrar(Product p, bool visible)
    {
        p.arrastrable = visible;
        if (p.SpriteRenderer != null) p.SpriteRenderer.enabled = visible;
    }
}