using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lector de códigos. Cuando el jugador pasa un producto (arrastrándolo) por encima
/// de la zona del lector, se registra en la caja.
/// Ponlo en tu imagen verde "Scaner" del Canvas.
/// </summary>
public class Scanner : MonoBehaviour
{
    [Header("Referencias")]
    public DragController dragController;
    public CashRegister caja;

    [Tooltip("Zona del lector. Si se deja vacío, usa este mismo objeto.")]
    public RectTransform zona;

    [Header("Feedback (opcional)")]
    [Tooltip("Imagen que destella al escanear. Si se deja vacío, usa la Image de la zona.")]
    public Image imagenLector;
    public Color colorDestello = Color.white;
    public float duracionDestello = 0.15f;

    [Tooltip("AudioSource con Play On Awake desactivado.")]
    public AudioSource audioSource;
    public AudioClip sonidoBeep;

    Camera camaraUI;
    Color colorOriginal;

    void Awake()
    {
        if (zona == null) zona = transform as RectTransform;
        if (zona == null)
        {
            Debug.LogWarning("Scanner: asigna una 'Zona' (un objeto del Canvas).", this);
            enabled = false;
            return;
        }

        if (imagenLector == null) imagenLector = zona.GetComponent<Image>();
        if (imagenLector != null) colorOriginal = imagenLector.color;

        // En Screen Space - Camera la UI necesita la cámara; en Overlay no (null)
        var canvas = zona.GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas = canvas.rootCanvas;
            if (canvas.renderMode != RenderMode.ScreenSpaceOverlay) camaraUI = canvas.worldCamera;
        }
    }

    void Update()
    {
        if (dragController == null || caja == null) return;

        Product p = dragController.Arrastrado;
        if (p == null || p.Escaneado) return;

        // ¿El centro del producto está sobre el lector?
        Vector2 posPantalla = dragController.camara.WorldToScreenPoint(p.transform.position);
        if (RectTransformUtility.RectangleContainsScreenPoint(zona, posPantalla, camaraUI))
            Escanear(p);
    }

    void Escanear(Product p)
    {
        if (!caja.Registrar(p)) return;

        if (audioSource != null && sonidoBeep != null) audioSource.PlayOneShot(sonidoBeep);

        if (imagenLector != null)
        {
            StopAllCoroutines();
            StartCoroutine(Destello());
        }
    }

    IEnumerator Destello()
    {
        imagenLector.color = colorDestello;
        yield return new WaitForSeconds(duracionDestello);
        imagenLector.color = colorOriginal;
    }
}