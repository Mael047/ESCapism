using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using TMPro;

public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [HideInInspector] public RectTransform band;
    [HideInInspector] public TMP_Text label;
    [HideInInspector] public UnityEvent onClick;
    [HideInInspector] public Color normalColor, hoverColor;
    [HideInInspector] public float speed = 12f;

    float target = 0f;

    void Update()
    {
        float cur = band.localScale.x;
        float next = Mathf.Lerp(cur, target, Time.unscaledDeltaTime * speed);
        band.localScale = new Vector3(next, 1f, 1f);
        label.color = Color.Lerp(normalColor, hoverColor, next);
    }

    public void OnPointerEnter(PointerEventData e) { target = 1f; }
    public void OnPointerExit(PointerEventData e)  { target = 0f; }
    public void OnPointerClick(PointerEventData e) { onClick?.Invoke(); }
}