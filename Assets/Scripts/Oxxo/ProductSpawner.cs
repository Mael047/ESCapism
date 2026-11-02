using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Instancia prefabs de productos en la cinta, uno al lado del otro.
/// </summary>
public class ProductSpawner : MonoBehaviour
{
    [Header("Prefabs de productos")]
    public Product[] prefabs;

    [Header("Ubicación en la cinta")]
    [Tooltip("Posición del primer producto (junto al escáner).")]
    public Transform puntoInicio;

    [Tooltip("Hacia dónde se forman los siguientes productos: 1 = derecha, -1 = izquierda.")]
    public float direccion = 1f;

    public float espacioEntreProductos = 0.2f;

    [Tooltip("Padre de los productos en la cinta (opcional, con escala 1).")]
    public Transform contenedor;

    [Header("Referencias")]
    [Tooltip("Para escalar los productos al tamaño de celda de la bolsa.")]
    public BagManager bagManager;

    [Tooltip("Cinta transportadora. Si está asignada, ella ubica y mueve los productos.")]
    public ConveyorBelt cinta;

    [Header("Prueba")]
    [Tooltip("Productos que se generan al iniciar. 0 = ninguno.")]
    public int productosAlIniciar = 5;

    void Start()
    {
        if (productosAlIniciar > 0) GenerarCliente(productosAlIniciar);
    }

    /// <summary>Genera 'cantidad' productos aleatorios en fila sobre la cinta.</summary>
    public List<Product> GenerarCliente(int cantidad)
    {
        var nuevos = new List<Product>();
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("ProductSpawner: no hay prefabs asignados.", this);
            return nuevos;
        }

        Vector3 inicio = puntoInicio != null ? puntoInicio.position : transform.position;
        float cursor = 0f;

        for (int i = 0; i < cantidad; i++)
        {
            Product prefab = prefabs[Random.Range(0, prefabs.Length)];
            Product p = Instantiate(prefab, inicio, Quaternion.identity, contenedor);
            if (bagManager != null) p.AjustarTamano(bagManager.TamanoCelda);

            if (cinta != null)
            {
                // La cinta se encarga de ubicarlo y moverlo
                cinta.Agregar(p);
            }
            else
            {
                float anchoProducto = p.SpriteRenderer != null ? p.SpriteRenderer.bounds.size.x : 1f;
                p.transform.position = inicio + Vector3.right * direccion * (cursor + anchoProducto * 0.5f);
                cursor += anchoProducto + espacioEntreProductos;
            }

            nuevos.Add(p);
        }
        return nuevos;
    }

    [ContextMenu("Generar cliente de prueba")]
    void GenerarPrueba() => GenerarCliente(Mathf.Max(1, productosAlIniciar));
}