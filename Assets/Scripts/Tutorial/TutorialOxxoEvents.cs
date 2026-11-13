using UnityEngine;

// Puente entre la caja del Oxxo y el tutorial.
// Escucha los eventos de CashRegister y avisa al tutorial, sin tocar el código de la caja.
// En los pasos con Advance = Action usa estos Action Id:
//   escanear    → se registró un producto en la caja
//   abrir_caja  → se abrió la caja (para explicar el cambio)
//   cerrar_caja → se cerró la caja
//   cobrar    → el cliente pagó
//   cambio    → se entregó el cambio
public class TutorialOxxoEvents : MonoBehaviour
{
    [SerializeField] private CashRegister cashRegister;

    private bool wasOpen;

    private void Awake()
    {
        if (cashRegister == null) cashRegister = FindAnyObjectByType<CashRegister>();
    }

    private void OnEnable()
    {
        if (cashRegister == null) return;
        cashRegister.OnProductoRegistrado += OnScanned;
        cashRegister.OnPagoRecibido += OnPaid;
        cashRegister.OnCambioEntregado += OnChange;
    }

    private void OnDisable()
    {
        if (cashRegister == null) return;
        cashRegister.OnProductoRegistrado -= OnScanned;
        cashRegister.OnPagoRecibido -= OnPaid;
        cashRegister.OnCambioEntregado -= OnChange;
    }

    // CashRegister no tiene evento al abrir la caja, así que se revisa cada frame
    private void Update()
    {
        if (cashRegister == null) return;

        bool open = cashRegister.CajaAbierta;
        if (open == wasOpen) return;
        wasOpen = open;
        TutorialManager.Notify(open ? "abrir_caja" : "cerrar_caja");
    }

    private void OnScanned(Product product) => TutorialManager.Notify("escanear");
    private void OnPaid(int amount) => TutorialManager.Notify("cobrar");
    private void OnChange(int paid, int change) => TutorialManager.Notify("cambio");
}
