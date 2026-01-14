using UnityEngine;

public class CCBodyMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;
    public float rotationSpeed = 10f;
    public float acceleration = 12f;
    public float deceleration = 16f;

    [Header("Jump & Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -9.81f;

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

    public Vector3 CurrentVelocity { get; private set; }
    public bool IsGrounded => _controller != null && _controller.isGrounded;

    // Values CCProceduralAnimation reads (live)
    public float ProcVelocityMultiplier => (_input != null && _input.sprint) ? sprintVelocityMultiplier : walkVelocityMultiplier;
    public float ProcCycleSpeed => (_input != null && _input.sprint) ? sprintCycleSpeed : walkCycleSpeed;
    public float ProcVelocityClamp => (_input != null && _input.sprint) ? sprintVelocityClamp : walkVelocityClamp;

    private CharacterController _controller;
    private InputBodyMove _input;

    private float _currentSpeed;
    private float _verticalVelocity;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<InputBodyMove>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        if (_controller == null)
            Debug.LogError("CCBodyMovement needs a CharacterController on the same GameObject.");
        if (_input == null)
            Debug.LogError("CCBodyMovement needs a InputBodyMove on the same GameObject.");
    }

    private void Update()
    {
        if (_controller == null || _input == null) return;

        HandleJumpAndGravity();
        HandleMovement();
    }

    private void HandleJumpAndGravity()
    {
        // isGrounded is updated after the last Move call
        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -2f; // keeps the controller snapped to ground

        if (_controller.isGrounded && _input.jump)
        {
            _verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            _input.jump = false;
        }

        _verticalVelocity += gravity * Time.deltaTime;
    }

    private void HandleMovement()
    {
        Vector2 moveInput = _input.move;

        float inputMagnitude = _input.analogMovement
            ? Mathf.Clamp01(moveInput.magnitude)
            : (moveInput.sqrMagnitude > 0.0001f ? 1f : 0f);

        float baseSpeed = _input.sprint ? sprintSpeed : walkSpeed;
        float targetSpeed = baseSpeed * inputMagnitude;

        float accel = (targetSpeed > _currentSpeed) ? acceleration : deceleration;
        _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, accel * Time.deltaTime);

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

        if (moveDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        Vector3 horizontal = (moveDir.sqrMagnitude > 0.0001f)
            ? moveDir.normalized * _currentSpeed
            : Vector3.zero;

        Vector3 velocity = horizontal + Vector3.up * _verticalVelocity;

        // Move expects a delta (meters this frame)
        _controller.Move(velocity * Time.deltaTime);

        CurrentVelocity = _controller.velocity;
    }
}
