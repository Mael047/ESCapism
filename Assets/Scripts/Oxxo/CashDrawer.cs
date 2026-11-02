using UnityEngine;

/// <summary>
/// El cajón de la caja. Ponlo en el panel que se activa cuando abres la caja
/// (el que tiene los billetes y monedas).
/// - Al abrirse el panel: la pantalla azul cambia a modo pago (total, paga con, cambio, entregas).
/// - Al cerrarse el panel: la pantalla vuelve a la lista de productos escaneados.
/// - Opcional: cierra el panel solo cuando entregas el cambio.
/// </summary>
public class CashDrawer : MonoBehaviour
{
    [Tooltip("Si se deja vacío, busca la CashRegister de la escena.")]
    public CashRegister caja;

    [Tooltip("Cierra el cajón automáticamente al entregar el cambio.")]
    public bool cerrarAlEntregar = true;

    void OnEnable()
    {
        if (caja == null) caja = FindAnyObjectByType<CashRegister>();
        if (caja == null) return;

        caja.OnCambioEntregado += AlEntregar;
        caja.AbrirCaja();
    }

    void OnDisable()
    {
        if (caja == null) return;

        caja.OnCambioEntregado -= AlEntregar;
        caja.CerrarCaja();
    }

    void AlEntregar(int entregado, int esperado)
    {
        if (cerrarAlEntregar) gameObject.SetActive(false);
    }
}