using UnityEngine;

public class InkGrow : MonoBehaviour
{
    public float minScale = 1f;
    public float maxScale = 1.12f;
    public float period = 10f;    // segundos por ciclo (más alto = más lento)

    RectTransform rt;

    void Start()
    {
        rt = GetComponent<RectTransform>();
    }

    void Update()
    {
        // va de 0 a 1 y vuelve, suavemente
        float t = (Mathf.Sin(Time.time * (Mathf.PI * 2f / period) - Mathf.PI / 2f) + 1f) * 0.5f;
        float s = Mathf.Lerp(minScale, maxScale, t);
        rt.localScale = new Vector3(s, s, 1f);
    }
}