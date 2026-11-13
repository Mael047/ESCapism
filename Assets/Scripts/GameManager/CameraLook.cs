using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

[RequireComponent(typeof(CinemachineOrbitalFollow))]
public class CameraLook : MonoBehaviour
{
    [Header("Mouse (grados por píxel)")]
    [SerializeField] private float mouseSensitivityX = 0.15f;
    [SerializeField] private float mouseSensitivityY = 0.1f;

    [Header("Mando (grados por segundo)")]
    [SerializeField] private float stickSpeedX = 180f;
    [SerializeField] private float stickSpeedY = 100f;

    [Header("Opciones")]
    [SerializeField] private bool invertY = false;

    private CinemachineOrbitalFollow orbital;
    private InputAction lookAction;

    private void Awake()
    {
        orbital = GetComponent<CinemachineOrbitalFollow>();
        lookAction = InputSystem.actions.FindAction("Look");
    }

    private void OnEnable() => lookAction.Enable();
    private void OnDisable() => lookAction.Disable();

    private void Update()
    {
        Vector2 look = lookAction.ReadValue<Vector2>();
        if (look == Vector2.zero) return;

        float deltaX, deltaY;

        // ¿Qué dispositivo movió la cámara este frame?
        if (lookAction.activeControl?.device is Mouse)
        {
            // Mouse: ya es desplazamiento, NO se multiplica por deltaTime
            float sens = GameSettings.MouseSensitivity;   // multiplicador del menú de opciones
            deltaX = look.x * mouseSensitivityX * sens;
            deltaY = look.y * mouseSensitivityY * sens;
        }
        else
        {
            // Stick: es velocidad, SÍ se multiplica por deltaTime
            float sens = GameSettings.StickSensitivity;   // multiplicador del menú de opciones
            deltaX = look.x * stickSpeedX * sens * Time.deltaTime;
            deltaY = look.y * stickSpeedY * sens * Time.deltaTime;
        }

        if (invertY) deltaY = -deltaY;

        // Horizontal: girar alrededor del jugador
        orbital.HorizontalAxis.Value = orbital.HorizontalAxis.ClampValue(
            orbital.HorizontalAxis.Value + deltaX);

        // Vertical: subir/bajar respetando el rango del Orbital Follow
        orbital.VerticalAxis.Value = orbital.VerticalAxis.ClampValue(
            orbital.VerticalAxis.Value - deltaY);
    }
}