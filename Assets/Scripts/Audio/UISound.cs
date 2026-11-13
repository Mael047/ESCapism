using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Sonido al pasar el mouse y al hacer clic en un botón o tarjeta.
// Ponlo en el mismo objeto del Button (ej. en los prefabs de las tarjetas de la tienda).
public class UISound : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private SoundData hover;
    [SerializeField] private SoundData click;

    private Selectable selectable;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        Button button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(() => SFX.Play(click));
    }

    private bool Interactable => selectable == null || selectable.IsInteractable();

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (Interactable) SFX.Play(hover);
    }
}
