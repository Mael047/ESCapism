using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class PlayerDash : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Animator animator;
    [SerializeField] private string dashState = "Dash";
    [SerializeField] private string locomotionState = "Locomotion";
    [SerializeField] private float transitionDuration = 0.1f;
    [SerializeField] private float returnTransition = 0.1f;

    [Header("Sonido")]
    [SerializeField] private SoundData dashSound;

    public event System.Action DashStarted;   // para la voz (PlayerAudio)
    //
    [Header("Movimiento")]
    [SerializeField] private float dashSpeed = 10f;
    [SerializeField] private float dashDuration = 0.5f;
    [SerializeField] private AnimationCurve speedCurve = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.6f, 1f) , new Keyframe(1f, 0f));
    [SerializeField] private float cooldown = 0.5f;
    //

    [Header("Invulnerabilidad")]
    [Range(0f, 1f)][SerializeField] private float iFrameStart = 0.2f;
    [Range(0f, 1f)][SerializeField] private float iFrameEnd = 0.8f;
    //
    [Header("Ataque de Dash")]
    [Range(0f, 1f)][SerializeField] private float attackCancelPoint = 0.5f;
    private bool attackQueued;
    //
    [Header("VFX")]
    [SerializeField] private ParticleSystem dashTrail;


    private Player player;
    private PlayerCombat combat;
    private InputAction dashAction;
    private float timer;
    private float cooldownTimer;
    private PlayerHealth health;

    public bool IsDashing { get; private set; }
    public bool IsInvulnerable { get; private set; }
    public Vector3 DashDirection { get; private set; }
    public Vector3 Dashvelocity { get; private set; }

    private void Awake()
    {
        player = GetComponent<Player>();
        combat = GetComponent<PlayerCombat>();
        dashAction = InputSystem.actions.FindAction("Dash");
        health = GetComponent<PlayerHealth>();
    }

    private void OnEnable() => dashAction.Enable();
    private void OnDisable() => dashAction.Disable();

    private void Update()
    {
        if (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;
        }
        if (dashAction.WasPressedThisFrame() && !IsDashing && cooldownTimer <= 0f && !health.IsHurt)
        {
            StartDash();
        }

        if(IsDashing)
        {
            UpdateDash();
        }
    }

    private void StartDash()
    {
        if(combat.IsAttacking)
        {
            combat.InterruptAttack();
        }

        Vector3 inputDirection = player.GetInputDirection();
        DashDirection = inputDirection.sqrMagnitude > 0.01f ? inputDirection.normalized : transform.forward;

        IsDashing = true;
        timer = 0f;
        attackQueued = false;
        animator.CrossFadeInFixedTime(dashState, transitionDuration);
        if (dashTrail != null) dashTrail.Play(true);
        SFX.Play(dashSound, transform.position);
        DashStarted?.Invoke();

    }

    private void UpdateDash()
    {
        timer += Time.deltaTime;
        float t = timer /dashDuration;

        Dashvelocity = DashDirection * dashSpeed * speedCurve.Evaluate(t);
        IsInvulnerable = t >= iFrameStart && t <= iFrameEnd;

        if(attackQueued && t >= attackCancelPoint)
        {
            EndDash(returnToLocomotion: false);
            combat.StartDashAttack();
            return;
        }
        if(t >= 1f)
        {
            EndDash(returnToLocomotion: true);
        }
    }

    private void EndDash(bool returnToLocomotion)
    {
        if(dashTrail != null) dashTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        IsDashing = false;
        IsInvulnerable = false;
        Dashvelocity = Vector3.zero;
        attackQueued = false;
        cooldownTimer = cooldown;
        
        if(returnToLocomotion)
        {
            animator.CrossFadeInFixedTime(locomotionState, returnTransition);
        }
    }

    public void QueueAttack()
    {
        attackQueued = true;
    }

    public void InterruptDash()
    {
        if (!IsDashing) return;
        EndDash(returnToLocomotion: false);
    }

    private void OnDrawGizmos()
    {
        if (!IsDashing) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position,1f);
    }
}
