using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(InputBodyMove))]
public class RBBodyMovement : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 2f;
    public float sprintSpeed = 5f;
    public float acceleration = 20f;
    public float airAcceleration = 8f;
    public float rotationSpeed = 12f;

    [Header("Jump")]
    public float jumpHeight = 1.2f;

    [Header("Ground Check")]
    public LayerMask groundLayers;
    public Transform groundCheck;
    public float groundCheckRadius = 0.25f;

    [Header("References")]
    public Transform cameraTransform;

    private Rigidbody _rb;
    private InputBodyMove _input;

    public bool IsGrounded { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _input = GetComponent<InputBodyMove>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        // Recommended for character-style RB movement
        _rb.freezeRotation = true;
    }

    private void FixedUpdate()
    {
        GroundCheck();
        MoveAndRotate();
        HandleJump();
    }

    private void GroundCheck()
    {
        Vector3 p = groundCheck ? groundCheck.position : (transform.position + Vector3.down * 0.9f);
        IsGrounded = Physics.CheckSphere(p, groundCheckRadius, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void MoveAndRotate()
    {
        Vector2 moveInput = _input.move;

        float inputMagnitude = _input.analogMovement
            ? Mathf.Clamp01(moveInput.magnitude)
            : (moveInput.sqrMagnitude > 0.0001f ? 1f : 0f);

        float baseSpeed = _input.sprint ? sprintSpeed : walkSpeed;
        float targetSpeed = baseSpeed * inputMagnitude;

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
            Quaternion newRot = Quaternion.Slerp(_rb.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
            _rb.MoveRotation(newRot);
        }

        Vector3 desiredHorizontalVel = (moveDir.sqrMagnitude > 0.0001f)
            ? moveDir.normalized * targetSpeed
            : Vector3.zero;

        Vector3 currentVel = _rb.linearVelocity;
        Vector3 currentHorizontal = new Vector3(currentVel.x, 0f, currentVel.z);

        float accel = IsGrounded ? acceleration : airAcceleration;
        Vector3 newHorizontal = Vector3.MoveTowards(
            currentHorizontal,
            desiredHorizontalVel,
            accel * Time.fixedDeltaTime
        );

        _rb.linearVelocity = new Vector3(newHorizontal.x, currentVel.y, newHorizontal.z);
    }

    private void HandleJump()
    {
        if (!IsGrounded) return;
        if (!_input.jump) return;

        // v = sqrt(2 * h * g) where g is positive magnitude
        float g = Mathf.Abs(Physics.gravity.y);
        float jumpVel = Mathf.Sqrt(2f * jumpHeight * g);

        Vector3 v = _rb.linearVelocity;
        v.y = jumpVel;
        _rb.linearVelocity = v;

        _input.jump = false; // consume jump
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 p = groundCheck ? groundCheck.position : (transform.position + Vector3.down * 0.9f);
        Gizmos.DrawWireSphere(p, groundCheckRadius);
    }
}
