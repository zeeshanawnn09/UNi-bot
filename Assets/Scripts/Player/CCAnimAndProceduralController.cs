using UnityEngine;
using UnityEngine.Animations.Rigging;

[DisallowMultipleComponent]
public class CCAnimAndProceduralController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private CCBodyMovement bodyMovement;
    [SerializeField] private RigBuilder rigBuilder;

    [Header("Animator Params (must match your Animator)")]
    [SerializeField] private string plantingTrigger = "Planting"; // Trigger
    [SerializeField] private string midAirBool = "MidAir";
    [SerializeField] private string idleBool = "Idle";
    [SerializeField] private string jumpedTrigger = "Jumped";

    [Header("Idle")]
    [Tooltip("How long the player must be not moving (while grounded) before Idle becomes true.")]
    [SerializeField] private float idleDelaySeconds = 1.0f;

    [Tooltip("Horizontal speed below this counts as not moving.")]
    [SerializeField] private float moveSpeedThreshold = 0.15f;

    [Header("RigBuilder Enable Rule")]
    [Tooltip("If true, RigBuilder will only run while grounded.")]
    [SerializeField] private bool rigOnlyWhenGrounded = true;

    [Header("Planting (Timer Lock)")]
    [Tooltip("How long planting lasts. While active: movement OFF, rig OFF, button spam ignored.")]
    [SerializeField] private float plantingDurationSeconds = 1.2f;

    private float _idleTimer = 0f;
    private bool _wasGrounded = true;

    private bool _isPlanting = false;
    private float _plantingTimer = 0f;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        bodyMovement = GetComponent<CCBodyMovement>();
        rigBuilder = GetComponentInChildren<RigBuilder>();
    }

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!bodyMovement) bodyMovement = GetComponent<CCBodyMovement>();
        if (!rigBuilder) rigBuilder = GetComponentInChildren<RigBuilder>();

        if (!animator) Debug.LogError("CCAnimAndProceduralController: Missing Animator (child).");
        if (!bodyMovement) Debug.LogError("CCAnimAndProceduralController: Missing CCBodyMovement (same GO).");
        if (!rigBuilder) Debug.LogWarning("CCAnimAndProceduralController: Missing RigBuilder (child).");
    }

    private void Start()
    {
        if (bodyMovement) _wasGrounded = bodyMovement.IsGrounded;

        _idleTimer = 0f;
        _isPlanting = false;
        _plantingTimer = 0f;

        SetBoolSafe(idleBool, false);
    }

    private void LateUpdate()
    {
        if (!animator || !bodyMovement) return;

        bool grounded = bodyMovement.IsGrounded;

        // --- Planting timer ---
        if (_isPlanting)
        {
            _plantingTimer -= Time.deltaTime;
            if (_plantingTimer <= 0f)
            {
                _isPlanting = false;
                _plantingTimer = 0f;

                if (bodyMovement) bodyMovement.enabled = true;
            }
        }

        // --- READ movement state (may be stale while bodyMovement disabled; planting overrides anyway) ---
        Vector3 v = bodyMovement.CurrentVelocity;
        float horizontalSpeed = new Vector3(v.x, 0f, v.z).magnitude;
        bool moving = horizontalSpeed > moveSpeedThreshold;

        // --- Animator values ---
        SetBoolSafe(midAirBool, !grounded);

        if (_wasGrounded && !grounded)
            SetTriggerSafe(jumpedTrigger);
        else if (!_wasGrounded && grounded)
            ResetTriggerSafe(jumpedTrigger);

        _wasGrounded = grounded;

        // --- Idle (your 1..5 loop), but never while planting ---
        bool idle = GetBoolSafe(idleBool);

        if (_isPlanting)
        {
            _idleTimer = 0f;
            if (idle) SetBoolSafe(idleBool, false);
        }
        else if (!grounded || moving)
        {
            _idleTimer = 0f;
            if (idle) SetBoolSafe(idleBool, false);
        }
        else
        {
            // grounded + stopped
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

        // --- RigBuilder on/off ---
        if (rigBuilder)
        {
            // Disable rig only when:
            // - planting
            // - midAir (if rigOnlyWhenGrounded)
            // - idle (after timer)
            bool allowRig = true;

            if (_isPlanting) allowRig = false;

            if (rigOnlyWhenGrounded && !grounded) allowRig = false;

            if (idle) allowRig = false;

            SetRigEnabled(allowRig);
        }
    }

    // Button calls this (PUBLIC)
    public void StartPlanting()
    {
        if (_isPlanting) return; // blocks spam / restart

        // optional: only allow planting while grounded
        if (bodyMovement && !bodyMovement.IsGrounded) return;

        _isPlanting = true;
        _plantingTimer = Mathf.Max(0.01f, plantingDurationSeconds);

        // stop movement
        if (bodyMovement) bodyMovement.enabled = false;

        // cancel idle immediately
        SetBoolSafe(idleBool, false);
        _idleTimer = 0f;

        // rig OFF immediately
        SetRigEnabled(false);

        // fire planting trigger
        SetTriggerSafe(plantingTrigger);
    }

    private void SetRigEnabled(bool enabled)
    {
        if (!rigBuilder) return;
        if (rigBuilder.enabled == enabled) return;

        rigBuilder.enabled = enabled;

        if (enabled)
            rigBuilder.Build();
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
