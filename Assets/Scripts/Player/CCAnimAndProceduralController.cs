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
    [SerializeField] private string plantingBool = "Planting";
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

    private float _idleTimer = 0f;
    private bool _wasGrounded = true;

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
    }

    private void LateUpdate()
    {
        if (!animator || !bodyMovement) return;

        // --- READ movement state ---
        bool grounded = bodyMovement.IsGrounded;
        Vector3 v = bodyMovement.CurrentVelocity;
        float horizontalSpeed = new Vector3(v.x, 0f, v.z).magnitude;
        bool moving = horizontalSpeed > moveSpeedThreshold;

        // --- WRITE Animator values only ---
        SetBoolSafe(midAirBool, !grounded);

        // Jump trigger on takeoff (grounded -> not grounded)
        if (_wasGrounded && !grounded)
        {
            SetTriggerSafe(jumpedTrigger);
        }
        else if (!_wasGrounded && grounded)
        {
            // optional cleanup (doesn't hurt)
            ResetTriggerSafe(jumpedTrigger);
        }
        _wasGrounded = grounded;

        bool planting = GetBoolSafe(plantingBool);

        // Idle only when grounded, not moving, and not planting
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

        // --- RigBuilder on/off ---
        if (rigBuilder)
        {
            bool allowRig =
                moving &&
                !planting &&
                !idle &&
                (!rigOnlyWhenGrounded || grounded) &&
                !midAir;

            SetRigEnabled(allowRig);
        }
    }

    // 3D button calls this
    public void StartPlanting()
    {
        // Rig OFF immediately
        SetRigEnabled(false);

        // animator values
        SetBoolSafe(plantingBool, true);
        SetBoolSafe(idleBool, false);
        _idleTimer = 0f;
    }

    // Call this from an Animation Event at the end of Digging (recommended),
    // OR call it from your button when you want to stop.
    public void EndPlanting()
    {
        SetBoolSafe(plantingBool, false);
        _idleTimer = 0f;
        // Rig will re-enable automatically next LateUpdate if moving + grounded etc.
    }

    private void SetRigEnabled(bool enabled)
    {
        if (!rigBuilder) return;

        if (rigBuilder.enabled == enabled) return;

        rigBuilder.enabled = enabled;

        // When re-enabling, rebuild the rig so constraints update immediately.
        if (enabled)
            rigBuilder.Build();
    }

    // --- Safe Animator helpers (no other side effects) ---
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
