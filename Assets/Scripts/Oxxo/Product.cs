using UnityEngine;

/// <summary>
/// Producto que se puede arrastrar a una bolsa.
/// Al agregar este componente, Unity añade solo el SpriteRenderer y el BoxCollider2D.
/// Solo arrastra la imagen al campo "Imagen". El sprite debe tener el pivote en el centro.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class Product : MonoBehaviour
{
    [Header("Datos")]
    public ProductData data;

    [Tooltip("Imagen del producto. Se pone sola en el SpriteRenderer y ajusta el collider.")]
    public Sprite imagen;

    [Header("Comportamiento")]
    [Tooltip("Si es false no se puede arrastrar (por ejemplo, si aún no se ha escaneado).")]
    public bool arrastrable = true;

    [Tooltip("Escala el sprite para que mida exactamente las celdas que ocupa en la grilla.")]
    public bool ajustarTamanoACelda = true;

    SpriteRenderer spriteRenderer;

    // Forma actual (cambia al rotar), normalizada para que la celda mínima sea (0,0)
    public Vector2Int[] FormaActual { get; private set; }
    // Ancho y alto en celdas de la caja que contiene la forma
    public Vector2Int Tamano { get; private set; }
    public BagGrid BolsaActual { get; private set; }
    public Vector2Int OrigenEnBolsa { get; private set; }
    public int Precio => data != null ? data.precio : 0;
    public string Nombre => data != null ? data.nombre : name;

    /// <summary>True cuando ya pasó por el lector. Un producto se cobra una sola vez.</summary>
    public bool Escaneado { get; private set; }
    public void MarcarEscaneado() => Escaneado = true;
    public SpriteRenderer SpriteRenderer => spriteRenderer;

    Color colorBase = Color.white;
    int ordenBase;

    // Estado antes de arrastrar (para devolverlo si se suelta en un lugar inválido)
    Vector3 posGuardada;
    Quaternion rotGuardada;
    Transform padreGuardado;
    Vector2Int[] formaGuardada;
    Vector2Int tamanoGuardado;
    BagGrid bolsaGuardada;
    Vector2Int origenGuardado;

    // Se ejecuta en el editor al cambiar algo en el Inspector: así ves la imagen sin dar Play
    void OnValidate() => AplicarImagen();

    void AplicarImagen()
    {
        if (imagen == null) return;

        var sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = imagen;

        var box = GetComponent<BoxCollider2D>();
        if (box != null)
        {
            box.size = imagen.bounds.size;
            box.offset = imagen.bounds.center;
        }
    }

    void Awake()
    {
        AplicarImagen();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            colorBase = spriteRenderer.color;
            ordenBase = spriteRenderer.sortingOrder;
        }

        bool tieneForma = data != null && data.forma != null && data.forma.Length > 0;
        AplicarForma(tieneForma ? data.forma : new[] { Vector2Int.zero });
    }

    /// <summary>Escala el sprite para que ocupe Tamano x tamanoCelda. Llamar recién instanciado (sin rotar).</summary>
    public void AjustarTamano(float tamanoCelda)
    {
        if (!ajustarTamanoACelda || spriteRenderer == null || spriteRenderer.sprite == null) return;

        Vector2 tamSprite = spriteRenderer.sprite.bounds.size;
        Vector2 objetivo = (Vector2)Tamano * tamanoCelda;
        transform.localScale = new Vector3(objetivo.x / tamSprite.x, objetivo.y / tamSprite.y, 1f);
    }

    /// <summary>Rota 90° antihorario la forma y el visual.</summary>
    public void Rotar()
    {
        var rotada = new Vector2Int[FormaActual.Length];
        for (int i = 0; i < FormaActual.Length; i++)
            rotada[i] = new Vector2Int(-FormaActual[i].y, FormaActual[i].x);

        AplicarForma(rotada);
        transform.Rotate(0f, 0f, 90f);
    }

    void AplicarForma(Vector2Int[] celdas)
    {
        int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
        foreach (var c in celdas)
        {
            minX = Mathf.Min(minX, c.x); minY = Mathf.Min(minY, c.y);
            maxX = Mathf.Max(maxX, c.x); maxY = Mathf.Max(maxY, c.y);
        }

        var normalizada = new Vector2Int[celdas.Length];
        var min = new Vector2Int(minX, minY);
        for (int i = 0; i < celdas.Length; i++) normalizada[i] = celdas[i] - min;

        FormaActual = normalizada;
        Tamano = new Vector2Int(maxX - minX + 1, maxY - minY + 1);
    }

    // Lo llama BagGrid al colocar o quitar el producto
    public void AsignarBolsa(BagGrid bolsa, Vector2Int origen)
    {
        BolsaActual = bolsa;
        OrigenEnBolsa = origen;
    }

    public void GuardarEstado()
    {
        posGuardada = transform.position;
        rotGuardada = transform.rotation;
        padreGuardado = transform.parent;
        formaGuardada = FormaActual;
        tamanoGuardado = Tamano;
        bolsaGuardada = BolsaActual;
        origenGuardado = OrigenEnBolsa;
    }

    public void RestaurarEstado()
    {
        transform.SetPositionAndRotation(posGuardada, rotGuardada);
        FormaActual = formaGuardada;
        Tamano = tamanoGuardado;

        if (bolsaGuardada != null) bolsaGuardada.Colocar(this, origenGuardado);
        else transform.SetParent(padreGuardado, true);
    }

    // --- Visual ---
    public void Tintar(Color c)
    {
        if (spriteRenderer != null) spriteRenderer.color = colorBase * c;
    }

    public void QuitarTinte()
    {
        if (spriteRenderer != null) spriteRenderer.color = colorBase;
    }

    /// <summary>Dibuja el producto por encima de todo mientras se arrastra.</summary>
    public void Elevar(bool arriba)
    {
        if (spriteRenderer != null) spriteRenderer.sortingOrder = arriba ? ordenBase + 100 : ordenBase;
    }
}