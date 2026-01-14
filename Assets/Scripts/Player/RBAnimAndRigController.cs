using UnityEngine;
using UnityEngine.Animations.Rigging;

[DisallowMultipleComponent]
public class RBAnimAndRigController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private RBBodyMovement bodyMovement;
    [SerializeField] private RigBuilder rigBuilder;

    [Header("Animator Params (must match your Animator)")]
    [SerializeField] private string plantingTrigger = "Planting"; // Trigger
    [SerializeField] private string diggingTrigger = "Digging";  // Trigger
    [SerializeField] private string midAirBool = "MidAir";
    [SerializeField] private string idleBool = "Idle";
    [SerializeField] private string jumpedTrigger = "Jumped";

    [Header("Idle")]
    [Tooltip("How long the player must be not moving (while grounded) before Idle becomes true.")]
    [SerializeField] private float idleDelaySeconds = 1.0f;

    [Tooltip("Horizontal speed below this counts as not moving.")]
    [SerializeField] private float moveSpeedThreshold = 0.15f;

    [Header("Rig Rules")]
    [Tooltip("If true, rig will only run while grounded (midAir disables rig).")]
    [SerializeField] private bool rigOnlyWhenGrounded = true;

    [Header("Action Durations (Different Timers)")]
    [SerializeField] private float plantingDurationSeconds = 1.2f;
    [SerializeField] private float diggingDurationSeconds = 1.6f;

    private float _idleTimer = 0f;
    private bool _wasGrounded = true;

    private bool _isAction = false;
    private float _actionTimer = 0f;
    private string _currentActionTrigger = "";

    // Events for action lifecycle
    public System.Action<string, float> OnActionStarting; // (triggerName, duration)
    public System.Action<string> OnActionCompleted;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        bodyMovement = GetComponent<RBBodyMovement>();
        rigBuilder = GetComponentInChildren<RigBuilder>();
    }

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!bodyMovement) bodyMovement = GetComponent<RBBodyMovement>();
        if (!rigBuilder) rigBuilder = GetComponentInChildren<RigBuilder>();

        if (!animator) Debug.LogError("RBAnimAndRigController: Missing Animator (child).");
        if (!bodyMovement) Debug.LogError("RBAnimAndRigController: Missing RBBodyMovement (same GO).");
        if (!rigBuilder) Debug.LogWarning("RBAnimAndRigController: Missing RigBuilder (child).");
    }

    private void Start()
    {
        if (bodyMovement) _wasGrounded = bodyMovement.IsGrounded;
        _idleTimer = 0f;
        SetBoolSafe(idleBool, false);

        _isAction = false;
        _actionTimer = 0f;
    }

    private void LateUpdate()
    {
        if (!animator || !bodyMovement) return;

        bool grounded = bodyMovement.IsGrounded;

        // --- Action timer ---
        if (_isAction)
        {
            _actionTimer -= Time.deltaTime;
            if (_actionTimer <= 0f)
            {
                _isAction = false;
                _actionTimer = 0f;

                if (bodyMovement) bodyMovement.enabled = true;

                // Notify that the action has completed
                OnActionCompleted?.Invoke(_currentActionTrigger);
            }
        }

        // --- Animator values (midair + jumped) ---
        SetBoolSafe(midAirBool, !grounded);

        if (_wasGrounded && !grounded)
            SetTriggerSafe(jumpedTrigger);
        else if (!_wasGrounded && grounded)
            ResetTriggerSafe(jumpedTrigger);

        _wasGrounded = grounded;

        // --- Movement read ---
        Vector3 v = bodyMovement.CurrentVelocity;
        float horizontalSpeed = new Vector3(v.x, 0f, v.z).magnitude;
        bool moving = horizontalSpeed > moveSpeedThreshold;

        // --- Idle state machine (never while action) ---
        bool idle = GetBoolSafe(idleBool);

        if (_isAction || !grounded || moving)
        {
            _idleTimer = 0f;
            if (idle) SetBoolSafe(idleBool, false);
        }
        else
        {
            if (!idle)
            {
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= idleDelaySeconds)
                {
                    SetBoolSafe(idleBool, true);
                    _idleTimer = idleDelaySeconds;
                }
            }
        }

        idle = GetBoolSafe(idleBool);

        // --- Rig on/off ---
        if (rigBuilder)
        {
            bool allowRig = true;

            if (_isAction) allowRig = false;
            if (rigOnlyWhenGrounded && !grounded) allowRig = false;
            if (idle) allowRig = false;

            SetRigEnabled(allowRig);
        }
    }

    // Button calls this (PUBLIC)
    public void StartPlanting()
    {
        StartAction(plantingTrigger, plantingDurationSeconds);
    }

    // Button calls this (PUBLIC)
    public void StartDigging()
    {
        StartAction(diggingTrigger, diggingDurationSeconds);
    }

    private void StartAction(string triggerName, float durationSeconds)
    {
        if (_isAction) return; // blocks spam
        if (bodyMovement && !bodyMovement.IsGrounded) return; // optional grounded gate

        _currentActionTrigger = triggerName;
        float duration = Mathf.Max(0.01f, durationSeconds);

        // Notify listeners BEFORE action starts
        OnActionStarting?.Invoke(triggerName, duration);

        _isAction = true;
        _actionTimer = duration;

        if (bodyMovement) bodyMovement.enabled = false;

        SetBoolSafe(idleBool, false);
        _idleTimer = 0f;

        SetRigEnabled(false);

        SetTriggerSafe(triggerName);
    }

    private void SetRigEnabled(bool enabled)
    {
        if (!rigBuilder) return;
        if (rigBuilder.enabled == enabled) return;

        rigBuilder.enabled = enabled;
        if (enabled) rigBuilder.Build();
    }

    // --- Safe Animator helpers ---
    private bool HasParam(string name, AnimatorControllerParameterType type)
    {
        if (string.IsNullOrEmpty(name) || !animator) return false;
        foreach (var p in animator.parameters)
            if (p.type == type && p.name == name)
                return true;
        return false;
    }

    private bool GetBoolSafe(string name)
    {
        return animator && HasParam(name, AnimatorControllerParameterType.Bool) && animator.GetBool(name);
    }

    private void SetBoolSafe(string name, bool value)
    {
        if (animator && HasParam(name, AnimatorControllerParameterType.Bool))
            animator.SetBool(name, value);
    }

    private void SetTriggerSafe(string name)
    {
        if (animator && HasParam(name, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(name);
    }

    private void ResetTriggerSafe(string name)
    {
        if (animator && HasParam(name, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(name);
    }
}
