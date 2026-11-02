using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Ponlo en cada billete o moneda (que ya son Buttons).
/// Al hacer clic, suma su valor a lo que el jugador va a entregar.
/// </summary>
[RequireComponent(typeof(Button))]
public class CashButton : MonoBehaviour
{
    [Tooltip("Si se deja vacío, busca la CashRegister de la escena.")]
    public CashRegister caja;

    [Tooltip("Valor en la misma unidad que los precios.\nCon centavos: $1 = 100, $5 = 500, $20 = 2000, 50 centavos = 50.")]
    public int valor = 100;

    void Awake()
    {
        if (caja == null) caja = FindAnyObjectByType<CashRegister>();
        GetComponent<Button>().onClick.AddListener(() => caja.AgregarDinero(valor));
    }
}