using UnityEngine;

[DisallowMultipleComponent]
public class AnimAndProceduralController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private CCBodyMovement bodyMovement;
    [SerializeField] private CCProceduralAnimation procedural;

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

    [Header("Procedural Enable Rule")]
    [Tooltip("If true, procedural feet will only run while grounded.")]
    [SerializeField] private bool proceduralOnlyWhenGrounded = true;

    private float _idleTimer = 0f;
    private bool _wasGrounded = true;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        bodyMovement = GetComponent<CCBodyMovement>();
        procedural = GetComponentInChildren<CCProceduralAnimation>();
    }

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!bodyMovement) bodyMovement = GetComponent<CCBodyMovement>();
        if (!procedural) procedural = GetComponentInChildren<CCProceduralAnimation>();

        if (!animator) Debug.LogError("AnimAndProceduralController: Missing Animator (child).");
        if (!bodyMovement) Debug.LogError("AnimAndProceduralController: Missing CCBodyMovement (same GO).");
        if (!procedural) Debug.LogError("AnimAndProceduralController: Missing CCProceduralAnimation (child).");
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

        // --- Procedural on/off (optional but allowed as you said) ---
        if (procedural)
        {
            bool allowProcedural =
                moving &&
                !planting &&
                !idle &&
                (!proceduralOnlyWhenGrounded || grounded) &&
                !midAir;

            if (procedural.enabled != allowProcedural)
                procedural.enabled = allowProcedural;
        }
    }

    // 3D button calls this
    public void StartPlanting()
    {
        // procedural OFF immediately
        if (procedural) procedural.enabled = false;

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
        // procedural will re-enable automatically next LateUpdate if moving + grounded etc.
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
