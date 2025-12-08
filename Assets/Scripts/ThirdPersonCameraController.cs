using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraController : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The player transform")]
    [SerializeField] private Transform target;          // Player
    [Tooltip("The main camera transform (child of this holder)")]
    [SerializeField] private Transform cameraTransform; // Main Camera
    [Tooltip("Player's PlayerInput (can be on the Player object)")]
    [SerializeField] private PlayerInput playerInput;   // PlayerInput

    [Header("Orbit Settings")]
    [SerializeField] private float distance = 4f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 8f;

    [Tooltip("Mouse look sensitivity for orbit")]
    [SerializeField] private float lookSensitivity = 120f;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Pivot Offset (relative to player)")]
    [Tooltip("Where the camera orbits around relative to player origin")]
    [SerializeField] private Vector3 pivotOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Zoom Settings")]
    [Tooltip("How strongly scroll affects zoom (distance change per scroll unit)")]
    [SerializeField] private float zoomScrollSensitivity = 0.5f;

    private InputAction lookAction;
    private InputAction zoomAction;

    private float yaw;
    private float pitch;

    // Public read-only access so the collision script knows the target distance
    public float DesiredDistance => distance;

    private void Awake()
    {
        if (playerInput == null)
        {
            playerInput = GetComponentInParent<PlayerInput>();
        }

        if (playerInput != null)
        {
            lookAction = playerInput.actions["Look"];  // Vector2
            zoomAction = playerInput.actions["Zoom"];  // float (Axis)
        }

        if (target != null)
        {
            Vector3 angles = target.eulerAngles;
            yaw = angles.y;
            pitch = 0f;
        }
        else
        {
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;
        }

        distance = Mathf.Clamp(distance, minDistance, maxDistance);
    }

    private void Update()
    {
        if (target == null || cameraTransform == null) return;

        HandleLook();
        HandleZoom();

        // Position the holder at pivot above the player
        transform.position = target.position + pivotOffset;
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Keep camera orientation, but NOT distance (collision script controls distance)
        cameraTransform.localRotation = Quaternion.identity;
    }

    private void HandleLook()
    {
        if (lookAction == null) return;

        Vector2 lookInput = lookAction.ReadValue<Vector2>();

        yaw += lookInput.x * lookSensitivity * Time.deltaTime;
        pitch -= lookInput.y * lookSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void HandleZoom()
    {
        if (zoomAction == null) return;

        float scroll = zoomAction.ReadValue<float>();

        if (Mathf.Abs(scroll) > 0.0001f)
        {
            distance -= scroll * zoomScrollSensitivity;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }
    }
}
