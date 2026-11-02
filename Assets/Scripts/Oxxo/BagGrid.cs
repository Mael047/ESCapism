using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Una bolsa con grilla. La posición del objeto es la ESQUINA INFERIOR IZQUIERDA de la grilla.
/// Mantén rotación 0 y escala 1 en este objeto.
/// </summary>
public class BagGrid : MonoBehaviour
{
    [Header("Grilla")]
    [Min(1)] public int ancho = 4;
    [Min(1)] public int alto = 5;
    [Min(0.01f)] public float tamanoCelda = 0.5f;

    [Header("Visual de celdas (opcional)")]
    [Tooltip("Sprite para dibujar cada celda, por ejemplo Create > 2D > Sprites > Square.")]
    public Sprite spriteCelda;
    public Color colorCelda = new Color(1f, 1f, 1f, 0.25f);
    public int ordenCeldas = -10;

    Product[,] celdas;
    readonly List<Product> productos = new List<Product>();

    public IReadOnlyList<Product> Productos => productos;
    public int TotalCeldas => ancho * alto;
    public int CeldasOcupadas { get; private set; }
    public float Ocupacion => (float)CeldasOcupadas / TotalCeldas;
    public bool EstaVacia => productos.Count == 0;

    Vector3 Origen => transform.position;

    void Awake()
    {
        celdas = new Product[ancho, alto];
        CrearVisualCeldas();
    }

    void CrearVisualCeldas()
    {
        if (spriteCelda == null) return;

        Vector2 tamSprite = spriteCelda.bounds.size;
        for (int x = 0; x < ancho; x++)
            for (int y = 0; y < alto; y++)
            {
                var go = new GameObject($"Celda {x},{y}");
                go.transform.SetParent(transform, false);
                go.transform.position = CentroCelda(new Vector2Int(x, y));
                go.transform.localScale = new Vector3(tamanoCelda * 0.92f / tamSprite.x, tamanoCelda * 0.92f / tamSprite.y, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = spriteCelda;
                sr.color = colorCelda;
                sr.sortingOrder = ordenCeldas;
            }
    }

    // --- Conversión mundo <-> grilla ---

    public Vector3 CentroCelda(Vector2Int c) =>
        Origen + new Vector3((c.x + 0.5f) * tamanoCelda, (c.y + 0.5f) * tamanoCelda, 0f);

    /// <summary>Posición en el mundo del centro del producto si su esquina inferior izquierda está en 'origen'.</summary>
    public Vector3 PosicionProducto(Product p, Vector2Int origen)
    {
        Vector2 centro = ((Vector2)origen + (Vector2)p.Tamano * 0.5f) * tamanoCelda;
        return Origen + new Vector3(centro.x, centro.y, 0f);
    }

    /// <summary>Celda de origen más cercana para un producto cuyo centro está en 'centroMundo'.</summary>
    public Vector2Int OrigenDesdePosicion(Product p, Vector3 centroMundo)
    {
        Vector2 local = (Vector2)(centroMundo - Origen) / tamanoCelda;
        Vector2 esquina = local - (Vector2)p.Tamano * 0.5f;
        return Vector2Int.RoundToInt(esquina);
    }

    public bool EstaCerca(Vector3 posMundo, float margen)
    {
        Vector2 local = posMundo - Origen;
        return local.x >= -margen && local.y >= -margen
            && local.x <= ancho * tamanoCelda + margen
            && local.y <= alto * tamanoCelda + margen;
    }

    // --- Lógica de la grilla ---

    public bool PuedeColocar(Product p, Vector2Int origen)
    {
        foreach (var c in p.FormaActual)
        {
            var pos = origen + c;
            if (pos.x < 0 || pos.y < 0 || pos.x >= ancho || pos.y >= alto) return false;

            var ocupante = celdas[pos.x, pos.y];
            if (ocupante != null && ocupante != p) return false;
        }
        return true;
    }

    public bool Colocar(Product p, Vector2Int origen)
    {
        if (!PuedeColocar(p, origen)) return false;
        if (p.BolsaActual != null) p.BolsaActual.Quitar(p);

        foreach (var c in p.FormaActual) celdas[origen.x + c.x, origen.y + c.y] = p;
        CeldasOcupadas += p.FormaActual.Length;
        productos.Add(p);

        p.transform.SetParent(transform, true);
        Vector3 pos = PosicionProducto(p, origen);
        pos.z = p.transform.position.z;
        p.transform.position = pos;
        p.AsignarBolsa(this, origen);
        return true;
    }

    public void Quitar(Product p)
    {
        if (p.BolsaActual != this) return;

        foreach (var c in p.FormaActual)
        {
            var pos = p.OrigenEnBolsa + c;
            if (celdas[pos.x, pos.y] == p) celdas[pos.x, pos.y] = null;
        }
        CeldasOcupadas -= p.FormaActual.Length;
        productos.Remove(p);
        p.AsignarBolsa(null, Vector2Int.zero);
    }

    public void Vaciar(bool destruirProductos = true)
    {
        foreach (var p in productos.ToArray())
        {
            Quitar(p);
            if (destruirProductos) Destroy(p.gameObject);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.9f);
        Vector3 o = transform.position;
        for (int x = 0; x <= ancho; x++)
            Gizmos.DrawLine(o + new Vector3(x * tamanoCelda, 0f), o + new Vector3(x * tamanoCelda, alto * tamanoCelda));
        for (int y = 0; y <= alto; y++)
            Gizmos.DrawLine(o + new Vector3(0f, y * tamanoCelda), o + new Vector3(ancho * tamanoCelda, y * tamanoCelda));
    }
}