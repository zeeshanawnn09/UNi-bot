using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("Cinemachine Cameras")]
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private CinemachineCamera freeLookCamera;

    [Header("Dolly Configuration")]
    [SerializeField] private CinemachineSplineDolly dollyComponent;
    [SerializeField] private float dollyStartPosition = 0f; // Point A
    [SerializeField] private float dollyEndPosition = 1f;   // Point B
    [SerializeField] private float dollySpeed = 0.5f;

    [Header("Camera Priorities")]
    [SerializeField] private int virtualCameraCinematicPriority = 10;
    [SerializeField] private int virtualCameraDefaultPriority = 1;
    [SerializeField] private int freeLookCameraPriority = 5;

    private Coroutine dollyMoveCoroutine;
    private bool isCinematicActive = false;

    private void Awake()
    {
        // Ensure Free Look has priority on start
        if (freeLookCamera != null)
            freeLookCamera.Priority = freeLookCameraPriority;

        if (virtualCamera != null)
            virtualCamera.Priority = virtualCameraDefaultPriority;

        // Get dolly component if not assigned
        if (dollyComponent == null && virtualCamera != null)
            dollyComponent = virtualCamera.GetComponent<CinemachineSplineDolly>();

        // Set dolly to start position
        if (dollyComponent != null)
            dollyComponent.CameraPosition = dollyStartPosition;
    }

    public void StartCinematic(float duration)
    {
        if (isCinematicActive) return;

        isCinematicActive = true;

        // Switch to virtual camera
        if (virtualCamera != null)
            virtualCamera.Priority = virtualCameraCinematicPriority;

        // Start dolly movement
        if (dollyMoveCoroutine != null)
            StopCoroutine(dollyMoveCoroutine);

        dollyMoveCoroutine = StartCoroutine(MoveDollyCart(duration));
    }

    public void EndCinematic()
    {
        if (!isCinematicActive) return;

        isCinematicActive = false;

        // Switch back to free look camera
        if (virtualCamera != null)
            virtualCamera.Priority = virtualCameraDefaultPriority;

        // Reset dolly to start position for next time
        if (dollyComponent != null)
            dollyComponent.CameraPosition = dollyStartPosition;

        // Stop dolly movement if still running
        if (dollyMoveCoroutine != null)
        {
            StopCoroutine(dollyMoveCoroutine);
            dollyMoveCoroutine = null;
        }
    }

    private IEnumerator MoveDollyCart(float duration)
    {
        if (dollyComponent == null) yield break;

        float elapsed = 0f;
        float startPos = dollyStartPosition;
        float endPos = dollyEndPosition;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // Smooth interpolation
            dollyComponent.CameraPosition = Mathf.Lerp(startPos, endPos, t);

            yield return null;
        }

        // Ensure we reach the exact end position
        dollyComponent.CameraPosition = endPos;
        dollyMoveCoroutine = null;
    }

    public bool IsCinematicActive => isCinematicActive;
}
