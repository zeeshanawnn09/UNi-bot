using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
[RequireComponent(typeof(InputBodyMove))]
public class RBBodyMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;
    public float acceleration = 20f;
    public float airAcceleration = 8f;
    public float rotationSpeed = 12f;

    [Header("Deceleration / Sliding")]
    [Tooltip("How quickly the character slows down on ground when there is NO input. Set low (or 0) for sliding/ice.")]
    public float groundDeceleration = 0f;

    [Tooltip("How quickly the character slows down in air when there is NO input (usually 0).")]
    public float airDeceleration = 0f;

    [Tooltip("If true, when there is no input we will NOT actively cancel planar velocity (lets physics + friction handle it).")]
    public bool allowSlidingWhenNoInput = true;

    [Header("Jump")]
    public float jumpHeight = 1.2f;

    [Header("Ground Check")]
    public LayerMask groundLayers = ~0;
    public float groundCheckDistance = 0.15f;
    public float groundCheckSkin = 0.02f;
    public float coyoteTime = 0.08f;

    [Header("References")]
    public Transform cameraTransform;

    [Header("Procedural Animation Values (Walk)")]
    public float walkVelocityMultiplier = 0.4f;
    public float walkCycleSpeed = 1f;
    public float walkVelocityClamp = 4f;

    [Header("Procedural Animation Values (Sprint)")]
    public float sprintVelocityMultiplier = 0.65f;
    public float sprintCycleSpeed = 1.5f;
    public float sprintVelocityClamp = 6f;

    public bool IsGrounded { get; private set; }
    public Vector3 CurrentVelocity { get; private set; }

    // Values RBProceduralAnimation reads (live)
    public float ProcVelocityMultiplier => (_input != null && _input.sprint) ? sprintVelocityMultiplier : walkVelocityMultiplier;
    public float ProcCycleSpeed => (_input != null && _input.sprint) ? sprintCycleSpeed : walkCycleSpeed;
    public float ProcVelocityClamp => (_input != null && _input.sprint) ? sprintVelocityClamp : walkVelocityClamp;

    Rigidbody _rb;
    CapsuleCollider _cap;
    InputBodyMove _input;

    float _lastGroundedTime;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _cap = GetComponent<CapsuleCollider>();
        _input = GetComponent<InputBodyMove>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        _rb.freezeRotation = true;
        _rb.useGravity = true;
    }

    void FixedUpdate()
    {
        IsGrounded = GroundCheck();
        if (IsGrounded) _lastGroundedTime = Time.time;

        MoveAndRotate();
        HandleJump();

#if UNITY_6000_0_OR_NEWER
        CurrentVelocity = _rb.linearVelocity;
#else
        CurrentVelocity = _rb.velocity;
#endif
    }

    void MoveAndRotate()
    {
        Vector2 moveInput = _input.move;

        float inputMagnitude = _input.analogMovement
            ? Mathf.Clamp01(moveInput.magnitude)
            : (moveInput.sqrMagnitude > 0.0001f ? 1f : 0f);

        bool hasInput = inputMagnitude > 0.0001f;

        float maxSpeed = (_input.sprint ? sprintSpeed : walkSpeed) * inputMagnitude;

        Vector3 moveDir;
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward; camForward.y = 0f; camForward.Normalize();
            Vector3 camRight = cameraTransform.right; camRight.y = 0f; camRight.Normalize();
            moveDir = camRight * moveInput.x + camForward * moveInput.y;
        }
        else
        {
            moveDir = new Vector3(moveInput.x, 0f, moveInput.y);
        }

        // Rotate only when there's meaningful input direction
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
            Quaternion newRot = Quaternion.Slerp(_rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(newRot);
        }

        Vector3 desiredPlanarVel = hasInput ? moveDir.normalized * maxSpeed : Vector3.zero;

#if UNITY_6000_0_OR_NEWER
        Vector3 v = _rb.linearVelocity;
#else
        Vector3 v = _rb.velocity;
#endif
        Vector3 planar = Vector3.ProjectOnPlane(v, Vector3.up);

        float accel = IsGrounded ? acceleration : airAcceleration;
        float decel = IsGrounded ? groundDeceleration : airDeceleration;

        // If no input and sliding is allowed, do NOT apply "braking" force.
        if (!hasInput && allowSlidingWhenNoInput)
        {
            if (decel > 0f)
            {
                Vector3 decelDelta = -planar;
                Vector3 decelAccel = Vector3.ClampMagnitude(
                    decelDelta / Mathf.Max(Time.fixedDeltaTime, 0.00001f),
                    decel
                );
                _rb.AddForce(decelAccel, ForceMode.Acceleration);
            }

            return;
        }

        // With input (or if sliding disabled): accelerate toward desired velocity
        Vector3 delta = desiredPlanarVel - planar;

        float maxAccelThisFrame = hasInput ? accel : decel;
        if (maxAccelThisFrame < 0f) maxAccelThisFrame = 0f;

        Vector3 accelVec = Vector3.ClampMagnitude(
            delta / Mathf.Max(Time.fixedDeltaTime, 0.00001f),
            maxAccelThisFrame
        );

        _rb.AddForce(accelVec, ForceMode.Acceleration);
    }

    void HandleJump()
    {
        bool canJump = IsGrounded || (Time.time - _lastGroundedTime) <= coyoteTime;
        if (!canJump) return;
        if (!_input.jump) return;

        float g = Mathf.Abs(Physics.gravity.y);
        float jumpVel = Mathf.Sqrt(2f * jumpHeight * g);

#if UNITY_6000_0_OR_NEWER
        Vector3 v = _rb.linearVelocity;
#else
        Vector3 v = _rb.velocity;
#endif
        if (v.y < 0f) v.y = 0f;
        v.y = jumpVel;

#if UNITY_6000_0_OR_NEWER
        _rb.linearVelocity = v;
#else
        _rb.velocity = v;
#endif

        _input.jump = false;
    }

    bool GroundCheck()
    {
        Vector3 scale = transform.lossyScale;

        float radius = Mathf.Max(0.001f, _cap.radius * Mathf.Max(scale.x, scale.z));
        float height = Mathf.Max(radius * 2f, _cap.height * scale.y);

        Vector3 centerWorld = transform.TransformPoint(_cap.center);

        float half = height * 0.5f - radius;
        Vector3 p1 = centerWorld + Vector3.up * half;
        Vector3 p2 = centerWorld - Vector3.up * half;

        float castRadius = Mathf.Max(0.001f, radius - groundCheckSkin);

        return Physics.CapsuleCast(
            p1, p2,
            castRadius,
            Vector3.down,
            out _,
            groundCheckDistance,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    void OnDrawGizmosSelected()
    {
        var cap = GetComponent<CapsuleCollider>();
        if (cap == null) return;

        Vector3 scale = transform.lossyScale;

        float radius = Mathf.Max(0.001f, cap.radius * Mathf.Max(scale.x, scale.z));
        float height = Mathf.Max(radius * 2f, cap.height * scale.y);

        Vector3 centerWorld = transform.TransformPoint(cap.center);

        float half = height * 0.5f - radius;
        Vector3 p1 = centerWorld + Vector3.up * half;
        Vector3 p2 = centerWorld - Vector3.up * half;

        float castRadius = Mathf.Max(0.001f, radius - groundCheckSkin);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(p1, castRadius);
        Gizmos.DrawWireSphere(p2, castRadius);
        Gizmos.DrawLine(p1, p2);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(p1 + Vector3.down * groundCheckDistance, castRadius);
        Gizmos.DrawWireSphere(p2 + Vector3.down * groundCheckDistance, castRadius);
        Gizmos.DrawLine(p1 + Vector3.down * groundCheckDistance, p2 + Vector3.down * groundCheckDistance);
    }
}
