using UnityEngine;

public class Breathe : MonoBehaviour
{
    public float amplitude = 3f;   // píxeles que sube y baja
    public float speed = 1.2f;     // más alto = respira más rápido
    public float phase = 0f;       // desfase para que no respiren idéntico

    RectTransform rt;
    Vector2 startPos;

    void Start()
    {
        rt = GetComponent<RectTransform>();
        startPos = rt.anchoredPosition;
    }

    void Update()
    {
        float y = Mathf.Sin(Time.time * speed + phase) * amplitude;
        rt.anchoredPosition = startPos + new Vector2(0f, y);
    }
}