using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class EyeFollow : MonoBehaviour
{
    public RectTransform pupil;      // la imagen del iris (PupIzq / PupDer)
    public RectTransform eyeCenter;  // punto vacío en el centro del ojo
    public float maxDistance = 8f;
    public float smooth = 10f;

    Vector2 startPos;

    void Start()
    {
        startPos = pupil.anchoredPosition;
    }

    Vector2 GetMouse()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    void Update()
    {
        Vector2 eyeScreen = RectTransformUtility.WorldToScreenPoint(null, eyeCenter.position);
        Vector2 dir = GetMouse() - eyeScreen;

        float strength = Mathf.Clamp01(dir.magnitude / 400f);
        Vector2 target = startPos + dir.normalized * maxDistance * strength;

        pupil.anchoredPosition = Vector2.Lerp(pupil.anchoredPosition, target, Time.deltaTime * smooth);
    }
}