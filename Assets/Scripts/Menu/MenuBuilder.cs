using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class MenuBuilder : MonoBehaviour
{
    [System.Serializable]
    public class Entry
    {
        public string label = "Jugar";
        public UnityEvent onClick;
    }

    [Header("Botones")]
    public Entry[] entries;

    [Header("Posición y tamaño (en píxeles del canvas)")]
    public Vector2 position = new Vector2(140f, -380f); // desde la esquina superior izquierda
    public float width = 420f;
    public float height = 70f;
    public float spacing = 12f;
    public float textPadding = 30f;

    [Header("Estilo")]
    public TMP_FontAsset font;
    public float fontSize = 36f;
    public Color textColor = Color.black;
    public Color textHoverColor = Color.white;
    public Color bandColor = new Color(0.96f, 0.45f, 0.71f, 1f); // rosa
    public Color lineColor = Color.black;
    public float lineThickness = 3f;
    public float animSpeed = 12f;

    void Start()
    {
        for (int i = 0; i < entries.Length; i++)
            CreateButton(entries[i], i);
    }

    RectTransform New(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    void CreateButton(Entry e, int i)
    {
        // Contenedor del botón (recibe el mouse)
        var root = New("Btn_" + e.label, transform);
        root.anchorMin = root.anchorMax = new Vector2(0f, 1f);
        root.pivot = new Vector2(0f, 1f);
        root.sizeDelta = new Vector2(width, height);
        root.anchoredPosition = position + new Vector2(0f, -i * (height + spacing));
        var hit = root.gameObject.AddComponent<Image>();
        hit.color = new Color(0, 0, 0, 0);

        // Banda (crece de izquierda a derecha)
        var band = New("Banda", root);
        Stretch(band);
        band.pivot = new Vector2(0f, 0.5f);
        band.localScale = new Vector3(0f, 1f, 1f);
        var bandImg = band.gameObject.AddComponent<Image>();
        bandImg.color = bandColor;
        bandImg.raycastTarget = false;

        // Línea superior
        var top = New("LineaArriba", band);
        top.anchorMin = new Vector2(0f, 1f);
        top.anchorMax = new Vector2(1f, 1f);
        top.pivot = new Vector2(0.5f, 1f);
        top.sizeDelta = new Vector2(0f, lineThickness);
        top.anchoredPosition = Vector2.zero;
        var topImg = top.gameObject.AddComponent<Image>();
        topImg.color = lineColor;
        topImg.raycastTarget = false;

        // Línea inferior
        var bot = New("LineaAbajo", band);
        bot.anchorMin = new Vector2(0f, 0f);
        bot.anchorMax = new Vector2(1f, 0f);
        bot.pivot = new Vector2(0.5f, 0f);
        bot.sizeDelta = new Vector2(0f, lineThickness);
        bot.anchoredPosition = Vector2.zero;
        var botImg = bot.gameObject.AddComponent<Image>();
        botImg.color = lineColor;
        botImg.raycastTarget = false;

        // Texto
        var txt = New("Texto", root);
        Stretch(txt);
        var tmp = txt.gameObject.AddComponent<TextMeshProUGUI>();
        tmp.text = e.label;
        if (font != null) tmp.font = font;
        tmp.fontSize = fontSize;
        tmp.color = textColor;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.margin = new Vector4(textPadding, 0, 0, 0);
        tmp.raycastTarget = false;

        // Lógica
        var mb = root.gameObject.AddComponent<MenuButton>();
        mb.band = band;
        mb.label = tmp;
        mb.onClick = e.onClick;
        mb.normalColor = textColor;
        mb.hoverColor = textHoverColor;
        mb.speed = animSpeed;
    }
}