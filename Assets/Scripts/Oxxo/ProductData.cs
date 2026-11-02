using UnityEngine;

/// <summary>
/// Datos de un producto: nombre, precio y forma en la grilla de la bolsa.
/// Crear con: clic derecho en Project > Create > Cajero > Producto
/// </summary>
[CreateAssetMenu(menuName = "Cajero/Producto", fileName = "NuevoProducto")]
public class ProductData : ScriptableObject
{
    public string nombre = "Producto";

    [Tooltip("Precio en unidades enteras (pesos o centavos). No uses float para evitar errores al calcular el cambio.")]
    public int precio = 1000;

    [Tooltip("Celdas que ocupa el producto. Ejemplos:\n" +
             "1x1: (0,0)\n" +
             "2x1 horizontal: (0,0) (1,0)\n" +
             "1x2 vertical: (0,0) (0,1)\n" +
             "L: (0,0) (1,0) (0,1)")]
    public Vector2Int[] forma = { Vector2Int.zero };
}