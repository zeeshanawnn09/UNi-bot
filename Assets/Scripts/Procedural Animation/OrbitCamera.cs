using UnityEngine;

public class OrbitCamera : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("The object the camera will orbit around (your player).")]
    public Transform target;

    [Tooltip("Offset from the target's position (e.g. eye height).")]
    public Vector3 targetOffset = new Vector3(0f, 1.7f, 0f);

    [Header("Distance")]
    public float distance = 5f;
    public float minDistance = 2f;
    public float maxDistance = 8f;

    [Header("Rotation")]
    public float mouseSensitivityX = 120f;
    public float mouseSensitivityY = 120f;

    [Tooltip("Min vertical angle (looking down).")]
    public float minVerticalAngle = -40f;

    [Tooltip("Max vertical angle (looking up).")]
    public float maxVerticalAngle = 70f;

    [Tooltip("Invert vertical mouse input.")]
    public bool invertY = false;

    [Header("Smoothing")]
    [Tooltip("Position smoothing time. 0 = no smoothing.")]
    public float positionSmoothTime = 0.05f;

    private float _yaw;
    private float _pitch;

    private Vector3 _currentPosition;
    private Vector3 _currentVelocity;

    private void Start()
    {
        if (target == null)
        {
            Debug.LogWarning("OrbitCamera has no target assigned.");
            return;
        }

        // Initialize yaw/pitch from current camera position
        Vector3 focusPoint = target.position + targetOffset;
        Vector3 dir = (transform.position - focusPoint).normalized;

        // horizontal angle (yaw)
        Vector3 flatDir = new Vector3(dir.x, 0f, dir.z);
        if (flatDir.sqrMagnitude > 0.0001f)
        {
            _yaw = Mathf.Atan2(flatDir.x, flatDir.z) * Mathf.Rad2Deg;
        }
        else
        {
            _yaw = transform.eulerAngles.y;
        }

        // vertical angle (pitch)
        _pitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg;
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

        // clamp distance
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        _currentPosition = transform.position;
    }

    private void LateUpdate()
    {
        if (target == null)
            return;

        // --- Input (simple mouse axis) ---
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        _yaw += mouseX * mouseSensitivityX * Time.deltaTime;

        float verticalSign = invertY ? 1f : -1f;
        _pitch += mouseY * mouseSensitivityY * Time.deltaTime * verticalSign;
        _pitch = Mathf.Clamp(_pitch, minVerticalAngle, maxVerticalAngle);

        // --- Distance clamping (if you later change it at runtime) ---
        distance = Mathf.Clamp(distance, minDistance, maxDistance);

        // --- Compute desired camera pose ---
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        Vector3 focusPoint = target.position + targetOffset;
        Vector3 desiredPosition = focusPoint + rotation * new Vector3(0f, 0f, -distance);

        // --- Smooth position ---
        if (positionSmoothTime > 0f)
        {
            _currentPosition = Vector3.SmoothDamp(
                _currentPosition,
                desiredPosition,
                ref _currentVelocity,
                positionSmoothTime
            );
        }
        else
        {
            _currentPosition = desiredPosition;
        }

        transform.position = _currentPosition;
        transform.rotation = rotation;
    }
}
