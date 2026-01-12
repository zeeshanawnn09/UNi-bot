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
    [SerializeField] private string plantingTrigger = "Planting";   // CHANGED: now a Trigger
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

    // Procedural rig suppression window after planting starts (prevents rig fighting the planting anim)
    [Header("Planting")]
    [Tooltip("Seconds to keep RigBuilder disabled after Planting trigger fires (match/cover your planting clip length).")]
    [SerializeField] private float plantingRigDisableSeconds = 1.0f;

    private float _idleTimer = 0f;
    private bool _wasGrounded = true;

    private bool _plantingLock = false;
    private float _plantingLockTimer = 0f;

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
        _plantingLock = false;
        _plantingLockTimer = 0f;
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

        // --- Planting lock timer ---
        if (_plantingLock)
        {
            _plantingLockTimer -= Time.deltaTime;
            if (_plantingLockTimer <= 0f)
            {
                _plantingLock = false;
                _plantingLockTimer = 0f;
                // Rig can re-enable naturally below when conditions allow.
            }
        }

        // Idle only when grounded, not moving, and not in planting lock
        if (!grounded || moving || _plantingLock)
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
                !idle &&
                !_plantingLock &&
                (!rigOnlyWhenGrounded || grounded) &&
                !midAir;

            SetRigEnabled(allowRig);
        }
    }

    // Button calls this to start planting animation (TRIGGER)
    public void StartPlanting()
    {
        // Rig OFF immediately and lock it for a short duration so it can't fight the animation.
        _plantingLock = true;
        _plantingLockTimer = Mathf.Max(0f, plantingRigDisableSeconds);
        SetRigEnabled(false);

        // Fire trigger to play planting animation.
        ResetTriggerSafe(plantingTrigger); // ensures clean retrigger
        SetTriggerSafe(plantingTrigger);

        // Cancel idle immediately.
        SetBoolSafe(idleBool, false);
        _idleTimer = 0f;
    }

    // OPTIONAL: Call from an Animation Event at the end of the planting/dig clip
    // if you want the rig to be allowed again immediately (instead of waiting the timer).
    public void EndPlanting()
    {
        _plantingLock = false;
        _plantingLockTimer = 0f;
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
