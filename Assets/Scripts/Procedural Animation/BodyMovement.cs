using UnityEngine;

public class BodyMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;
    public float rotationSpeed = 10f;

    [Tooltip("How fast you accelerate to target speed")]
    public float acceleration = 12f;

    [Tooltip("How fast you slow down when releasing input")]
    public float deceleration = 16f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -15f;
    public float groundCheckRadius = 0.3f;
    public float groundCheckOffset = 0.2f;
    public LayerMask groundLayers;

    [Header("References")]
    [Tooltip("Camera whose forward/right will be used for movement. If null, will try Camera.main.")]
    public Transform cameraTransform;

    private CharacterController _controller;
    private BodyMoveInput _input;

    private float _verticalVelocity;
    private bool _grounded;

    private float _currentSpeed; // smoothed horizontal speed

    // Optional: exposed for debug/other scripts, doesn't affect ProceduralAnimation
    public Vector3 CurrentVelocity { get; private set; }

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<BodyMoveInput>();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (_controller == null)
            Debug.LogError("BodyMovement needs a CharacterController on the same GameObject.");
        if (_input == null)
            Debug.LogError("BodyMovement needs a BodyMoveInput on the same GameObject.");
    }

    private void Update()
    {
        if (_controller == null || _input == null) return;

        GroundCheck();
        HandleJumpAndGravity();
        HandleMovement();
    }

    private void GroundCheck()
    {
        Vector3 spherePos = transform.position + Vector3.down * groundCheckOffset;
        _grounded = Physics.CheckSphere(
            spherePos,
            groundCheckRadius,
            groundLayers,
            QueryTriggerInteraction.Ignore
        );

        if (_grounded && _verticalVelocity < 0f)
        {
            // small downward force keeps controller grounded
            _verticalVelocity = -2f;
        }
    }

    private void HandleMovement()
    {
        Vector2 moveInput = _input.move;

        // magnitude of stick / WASD input
        float inputMagnitude = _input.analogMovement
            ? Mathf.Clamp01(moveInput.magnitude)
            : (moveInput.sqrMagnitude > 0.0001f ? 1f : 0f);

        // choose base speed (walk vs sprint)
        float baseSpeed = _input.sprint ? sprintSpeed : walkSpeed;
        float targetSpeed = baseSpeed * inputMagnitude;

        // smooth speed (accel / decel)
        float accel = (targetSpeed > _currentSpeed) ? acceleration : deceleration;
        _currentSpeed = Mathf.MoveTowards(
            _currentSpeed,
            targetSpeed,
            accel * Time.deltaTime
        );

        // ===== camera-relative world-space direction =====
        Vector3 moveDir = Vector3.zero;

        if (cameraTransform != null)
        {
            // flatten camera forward/right onto XZ plane
            Vector3 camForward = cameraTransform.forward;
            camForward.y = 0f;
            camForward.Normalize();

            Vector3 camRight = cameraTransform.right;
            camRight.y = 0f;
            camRight.Normalize();

            // input.y = forward/back, input.x = left/right relative to camera
            moveDir = camRight * moveInput.x + camForward * moveInput.y;
        }
        else
        {
            // fallback: world-space axes
            moveDir = new Vector3(moveInput.x, 0f, moveInput.y);
        }
        // ================================================

        // rotate towards movement direction (only if there's meaningful input)
        if (moveDir.sqrMagnitude > 0.001f)
        {
            Vector3 flatDir = moveDir.normalized;
            Quaternion targetRot = Quaternion.LookRotation(flatDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }

        // horizontal + vertical velocity (still world space)
        Vector3 horizontalVelocity = moveDir.normalized * _currentSpeed;
        Vector3 velocity = horizontalVelocity + Vector3.up * _verticalVelocity;

        CurrentVelocity = velocity;

        _controller.Move(velocity * Time.deltaTime);
    }

    private void HandleJumpAndGravity()
    {
        if (_grounded)
        {
            if (_input.jump)
            {
                // v = sqrt(h * -2 * g)
                _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                _input.jump = false; // consume jump
            }
        }

        // gravity every frame
        _verticalVelocity += gravity * Time.deltaTime;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = _grounded ? new Color(0, 1, 0, 0.4f) : new Color(1, 0, 0, 0.4f);
        Vector3 spherePos = transform.position + Vector3.down * groundCheckOffset;
        Gizmos.DrawSphere(spherePos, groundCheckRadius);
    }
}
