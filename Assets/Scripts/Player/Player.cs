using UnityEngine;
using UnityEngine.InputSystem;

public class Player : MonoBehaviour
{

    [Header("Referencias de Velocidad y Salto")]
    public float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 10f;
    [SerializeField] private float attackTurnSpeed = 20f;

    [Header("Animación")]
    [SerializeField] private Animator animator;
    [SerializeField] private PlayerCombat combat;
    [SerializeField] private PlayerDash dash;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    //Referencias Privadas
    private Rigidbody rb;
    private Transform cam;
    private InputAction moveAction;
    private Vector2 moveInput;
    private PlayerHealth health;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        moveAction = InputSystem.actions.FindAction("Move");
        cam = Camera.main.transform;
        health = GetComponent<PlayerHealth>();
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
        moveInput = (combat.IsAttacking || dash.IsDashing || health.IsHurt)
        ? Vector2.zero
        : moveAction.ReadValue<Vector2>();

        //Normalizar la entrada del movimiento para las animaciones, lo máximo es 1
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        float speed = horizontalVelocity.magnitude / moveSpeed;
        animator.SetFloat(SpeedHash, speed , 0.1f , Time.deltaTime);
    }

    //Fixed para la física jeje 
    private void FixedUpdate()
    {
        Vector3 move;
        if (health.IsHurt)
        {
            // Durante la reacción: solo el empuje, sin girar
            move = health.KnockbackVelocity;
        }
        else if (dash.IsDashing)
        {
            // Durante el dash: el dash decide movimiento y orientación
            move = dash.Dashvelocity;
            RotateTowards(dash.DashDirection, 50f);
        }
        else if (combat.IsAttacking)
        {
            // Durante el ataque: el combate decide movimiento y orientación
            move = combat.AttackVelocity;
            RotateTowards(combat.AttackDirection, attackTurnSpeed);
        }
        else
        {
            move = CameraRelative(moveInput) * moveSpeed;
            RotateTowards(move, rotationSpeed);
        }
        
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);
    }

    public Vector3 GetInputDirection()
    {
        return CameraRelative(moveAction.ReadValue<Vector2>());
    }
    
    public Vector3 CameraRelative(Vector2 input)
    {
        Vector3 camForward = cam.forward;
        Vector3 camRight = cam.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();
        return camForward * input.y + camRight * input.x;
    }

    private void RotateTowards(Vector3 direction, float speed)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude < 0.01f) return;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, speed * Time.fixedDeltaTime));

    }

    private void OnDrawGizmos()
    {
      
    }

}


