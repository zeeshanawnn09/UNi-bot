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
    [SerializeField] private string plantingBool = "Planting";
    [SerializeField] private string midAirBool = "MidAir";
    [SerializeField] private string idleBool = "Idle";
    [SerializeField] private string jumpedTrigger = "Jumped";

    [Header("Idle")]
    [Tooltip("How long the player must be not moving (while grounded) before Idle becomes true.")]
    [SerializeField] private float idleDelaySeconds = 1.0f;

    [Tooltip("Horizontal speed below this counts as not moving.")]
    [SerializeField] private float moveSpeedThreshold = 0.15f;

    [Header("Rig Enable Rule")]
    [Tooltip("If true, rig will only run while grounded.")]
    [SerializeField] private bool rigOnlyWhenGrounded = true;

    [Tooltip("If true, rig only runs while moving (same behavior as your old script).")]
    [SerializeField] private bool rigOnlyWhenMoving = true;

    private float _idleTimer = 0f;
    private bool _wasGrounded = true;

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

        if (!animator) Debug.LogError("AnimAndRigController_RB: Missing Animator (child).");
        if (!bodyMovement) Debug.LogError("AnimAndRigController_RB: Missing RBBodyMovement (same GO).");
        if (!rigBuilder) Debug.LogWarning("AnimAndRigController_RB: Missing RigBuilder (child).");
    }

    private void Start()
    {
        if (bodyMovement) _wasGrounded = bodyMovement.IsGrounded;
        _idleTimer = 0f;
    }

    private void LateUpdate()
    {
        if (!animator || !bodyMovement) return;

        // RBBodyMovement updates in FixedUpdate, so in LateUpdate we're reading the latest cached state.
        bool grounded = bodyMovement.IsGrounded;

        Vector3 v = bodyMovement.CurrentVelocity;
        float horizontalSpeed = new Vector3(v.x, 0f, v.z).magnitude;
        bool moving = horizontalSpeed > moveSpeedThreshold;

        // --- Animator values ---
        SetBoolSafe(midAirBool, !grounded);

        // Jumped trigger when grounded -> not grounded (also fires when walking off ledges)
        if (_wasGrounded && !grounded)
        {
            SetTriggerSafe(jumpedTrigger);
        }
        else if (!_wasGrounded && grounded)
        {
            ResetTriggerSafe(jumpedTrigger);
        }
        _wasGrounded = grounded;

        bool planting = GetBoolSafe(plantingBool);

        // Idle only when grounded, not moving, not planting
        if (!grounded || moving || planting)
        {
            _idleTimer = 0f;
            SetBoolSafe(idleBool, false);
        }
        else
        {
            _idleTimer += Time.deltaTime;
            if (_idleTimer >= idleDelaySeconds)
                SetBoolSafe(idleBool, true);
        }

        bool idle = GetBoolSafe(idleBool);
        bool midAir = !grounded;

        // --- Rig on/off ---
        if (rigBuilder)
        {
            bool allowRig = !planting && !idle && !midAir;

            if (rigOnlyWhenGrounded)
                allowRig &= grounded;

            if (rigOnlyWhenMoving)
                allowRig &= moving;

            SetRigEnabled(allowRig);
        }
    }

    // Button calls this
    public void StartPlanting()
    {
        // Rig OFF immediately
        SetRigEnabled(false);

        // Animator values
        SetBoolSafe(plantingBool, true);
        SetBoolSafe(idleBool, false);
        _idleTimer = 0f;
    }

    // Call from Animation Event at end of planting/digging, or from button
    public void EndPlanting()
    {
        SetBoolSafe(plantingBool, false);
        _idleTimer = 0f;
        // Rig will re-enable automatically based on rules next LateUpdate
    }

    private void SetRigEnabled(bool enabled)
    {
        if (!rigBuilder) return;
        if (rigBuilder.enabled == enabled) return;

        rigBuilder.enabled = enabled;

        // When re-enabling, rebuild so constraints update immediately.
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
