using System.Text.Json.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{

    [Header("Referencias de Velocidad y Salto")]
    public float moveSpeed = 2f;
    public float jumpForce = 3f;

    [Header("Animación")]
    [SerializeField] private Animator animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");


    [Header("Cámara")]
    [SerializeField] private float rotationSpeed = 10f;
    private Transform cam;

    [SerializeField] private PlayerCombat combat;

    //Referencias Privadas
    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        moveAction = InputSystem.actions.FindAction("Move");
        cam = Camera.main.transform;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
 
    private void OnEnable() => moveAction.Enable();
    private void OnDisable() => moveAction.Disable();

    private void Update()
    {
        //Por si esta atacando, no se puede mover
        moveInput = combat.IsAttacking ? Vector2.zero : moveAction.ReadValue<Vector2>();

        //Normalizar la entrada del movimiento para las animaciones, lo máximo es 1
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizontalVelocity.magnitude / moveSpeed;
        animator.SetFloat(SpeedHash, speed , 0.1f , Time.deltaTime);
    }

    //Fixed para la física jeje 
    private void FixedUpdate()
    {

        //Movimiento 
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;   // ignorar la inclinación de la cámara
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move;

        if (combat.IsAttacking)
        {
            // Durante el ataque, el combate decide el movimiento
            move = combat.AttackVelocity;
        }
        else
        {
            move = (camForward * moveInput.y + camRight * moveInput.x) * moveSpeed;
        }

        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

        // Solo gira libremente cuando no ataca
        if (!combat.IsAttacking && move.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(move.x, 0f, move.z));
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }
    }

    private void OnDrawGizmos()
    {
      
    }

}


