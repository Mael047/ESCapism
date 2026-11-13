using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tarjeta del tutorial. Diséñala como quieras (fondo, fuentes, tamaños, layout)
// mientras estas referencias sigan asignadas.
public class TutorialCard : MonoBehaviour
{
    [Tooltip("\"Paso X de N\" (opcional)")]
    public TMP_Text counter;
    [Tooltip("Barra que avanza. Si es una Image tipo Filled usa Fill Amount; si no, se estira de izquierda a derecha (opcional)")]
    public RectTransform progressFill;
    public TMP_Text title;
    public TMP_Text body;
    public Button nextButton;

    [Tooltip("Pone el título en mayúsculas")]
    public bool uppercaseTitle = true;

    public RectTransform Rect => (RectTransform)transform;

    public void SetProgress(float value)
    {
        if (progressFill == null) return;

        Image image = progressFill.GetComponent<Image>();
        if (image != null && image.type == Image.Type.Filled)
            image.fillAmount = value;
        else
            progressFill.anchorMax = new Vector2(value, progressFill.anchorMax.y);
    }
}
