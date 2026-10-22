using System.Text.Json.Serialization;
using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{

    [Header("Referencias de Velocidad y Salto")]
    public float moveSpeed = 2f;
    public float jumpForce = 3f;

    //Para saber si esta tocando el suelo 
    [Header("Detección del suelo")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Cámara")]
    [SerializeField] private float rotationSpeed = 10f;
    private Transform cam; 

    //Referencias Privadas
    private Rigidbody rb;
    private InputAction moveAction;
    private InputAction jumpAction;


    private Vector2 moveInput;
    bool isGrounded;
    bool jumpRequested;


    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        moveAction = InputSystem.actions.FindAction("Move");
        jumpAction = InputSystem.actions.FindAction("Jump");
        cam = Camera.main.transform;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    private void OnEnable()
    {
        moveAction.Enable();
        jumpAction.Enable();
    }

    private void OnDisable()
    {
        moveAction.Disable();
        jumpAction.Disable();
    } 

    private void Update()
    {
        moveInput = moveAction.ReadValue<Vector2>();

        if(jumpAction.WasPressedThisFrame())
        {
            jumpRequested = true;
        }
    }

    //Fixed para la física jeje 
    private void FixedUpdate()
    {
        //comprobación del suelo 
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundLayer);


        //Movimiento 
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;   // ignorar la inclinación de la cámara
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 move = (camForward * moveInput.y + camRight * moveInput.x) * moveSpeed;
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

        // Girar al jugador hacia donde se mueve
        if (move.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(new Vector3(move.x, 0f, move.z));
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        //Salto
        if (jumpRequested && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        jumpRequested = false;
    }

    private void OnDrawGizmos()
    {
        if (groundCheck == null) return;
        Gizmos.color = isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }

}


