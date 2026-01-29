using UnityEngine;
using UnityEngine.Animations.Rigging;   // only needed if you use RigBuilder

[RequireComponent(typeof(CharacterController))]
public class CCMovingPlatformRide : MonoBehaviour
{
    [Tooltip("Tag used by moving platforms/elevators.")]
    public string platformTag = "Elevator";

    [Header("Optional")]
    [SerializeField] private RigBuilder rigBuilder;   // drag your RigBuilder here if you want it toggled

    private CharacterController controller;
    private Transform currentPlatform;
    private Vector3 lastPlatformPosition;
    private bool riding;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // If we were riding but are no longer grounded, stop riding so jump works normally
        if (riding && !controller.isGrounded)
        {
            StopRiding();
        }
    }

    private void LateUpdate()
    {
        if (!riding || currentPlatform == null)
            return;

        // Follow the platform by applying its delta position
        Vector3 platformDelta = currentPlatform.position - lastPlatformPosition;
        if (platformDelta.sqrMagnitude > 0f)
        {
            controller.Move(platformDelta);
        }

        lastPlatformPosition = currentPlatform.position;
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // Only care about the elevator tag
        if (!hit.collider.CompareTag(platformTag))
            return;

        // Only when standing on top (moving downward onto it)
        if (hit.moveDirection.y < -0.5f)
        {
            StartRiding(hit.collider.transform);
        }
    }

    private void StartRiding(Transform platform)
    {
        currentPlatform = platform;
        lastPlatformPosition = currentPlatform.position;
        riding = true;

        if (rigBuilder != null)
            rigBuilder.enabled = false;
    }

    private void StopRiding()
    {
        riding = false;
        currentPlatform = null;

        if (rigBuilder != null)
            rigBuilder.enabled = true;
    }

    // Optional external call if you teleport/respawn
    public void ForceStopRiding()
    {
        StopRiding();
    }
}
