using UnityEngine;

[RequireComponent(typeof(ThirdPersonCameraController))]
public class ThirdPersonCameraCollision : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The main camera transform (child of this holder)")]
    [SerializeField] private Transform cameraTransform;          // Main Camera

    [Tooltip("Camera orbit controller on this holder")]
    [SerializeField] private ThirdPersonCameraController cameraController;

    [Header("Collision")]
    [Tooltip("Layers the camera should collide with (e.g. Geometry)")]
    [SerializeField] private LayerMask collisionLayers;

    [Tooltip("Radius for collision check around camera")]
    [SerializeField] private float cameraRadius = 0.3f;

    [Tooltip("Minimum distance camera can be from pivot")]
    [SerializeField] private float minCameraDistance = 0.3f;

    [Tooltip("Extra distance from hit point to avoid clipping")]
    [SerializeField] private float collisionOffset = 0.1f;

    [Tooltip("How quickly camera moves to new distance")]
    [SerializeField] private float distanceSmoothSpeed = 15f;

    private float currentDistance;

    private void Awake()
    {
        if (cameraController == null)
            cameraController = GetComponent<ThirdPersonCameraController>();

        if (cameraTransform == null && cameraController != null)
        {
            // Try to find a Camera child automatically
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
        }

        // If mask not set, fall back to a layer named "Geometry"
        if (collisionLayers == 0)
            collisionLayers = LayerMask.GetMask("Geometry");
    }

    private void Start()
    {
        if (cameraController != null)
            currentDistance = cameraController.DesiredDistance;
    }

    private void LateUpdate()
    {
        if (cameraController == null || cameraTransform == null)
            return;

        // Desired distance from the orbit controller (zoom)
        float desiredDistance = cameraController.DesiredDistance;

        // Pivot = this holder’s position (already set in ThirdPersonCameraController.Update)
        Vector3 pivot = transform.position;
        Vector3 direction = -transform.forward;

        // Ideal camera position without collision
        Vector3 desiredCameraPos = pivot + direction * desiredDistance;

        float targetDistance = desiredDistance;

        // SphereCast from pivot towards desired camera position
        RaycastHit hit;
        float maxCastDistance = desiredDistance + collisionOffset;

        if (Physics.SphereCast(
                pivot,
                cameraRadius,
                direction,
                out hit,
                maxCastDistance,
                collisionLayers,
                QueryTriggerInteraction.Ignore))
        {
            // Place camera just in front of the hit point
            targetDistance = Mathf.Max(minCameraDistance, hit.distance - collisionOffset);
        }

        // Smooth distance to avoid popping
        currentDistance = Mathf.Lerp(currentDistance, targetDistance, distanceSmoothSpeed * Time.deltaTime);

        // Apply final local position (camera always along -Z)
        cameraTransform.localPosition = new Vector3(0f, 0f, -currentDistance);
    }

#if UNITY_EDITOR
    // Optional: visual debug when selected
    private void OnDrawGizmosSelected()
    {
        if (cameraTransform == null) return;

        Gizmos.DrawWireSphere(transform.position, cameraRadius);
        Gizmos.DrawWireSphere(cameraTransform.position, cameraRadius);
    }
#endif
}
