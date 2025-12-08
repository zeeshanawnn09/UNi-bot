using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerInput))]
public class ThirdPersonPlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 10f;

    [Header("References")]
    [Tooltip("Player -> CameraHolder -> Camera")]
    [SerializeField] private Transform cameraHolder;

    private CharacterController controller;
    private PlayerInput playerInput;
    private InputAction moveAction;

    private Vector3 moveVelocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();

        // Input System action named "Move" (Vector2)
        moveAction = playerInput.actions["Move"];
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
    }

    private void Update()
    {
        if (cameraHolder == null || moveAction == null) return;

        Vector2 moveInput = moveAction.ReadValue<Vector2>(); // (x = A/D, y = W/S)

        // Camera-relative directions (ignore vertical)
        Vector3 camForward = cameraHolder.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Vector3 camRight = cameraHolder.right;
        camRight.y = 0f;
        camRight.Normalize();

        // Desired move direction
        Vector3 targetDirection = camForward * moveInput.y + camRight * moveInput.x;

        if (targetDirection.sqrMagnitude > 0.0001f)
        {
            targetDirection.Normalize();
            moveVelocity = targetDirection * moveSpeed;

            // Rotate player to face movement direction
            Quaternion targetRot = Quaternion.LookRotation(targetDirection);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
        else
        {
            moveVelocity = Vector3.zero;
        }

        // SimpleMove applies gravity internally
        controller.SimpleMove(moveVelocity);
    }
}
