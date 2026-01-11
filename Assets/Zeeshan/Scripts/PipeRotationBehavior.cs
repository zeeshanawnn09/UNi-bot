using UnityEngine;

public class PipeRotationBehavior : MonoBehaviour
{
    [SerializeField] private float stepDegrees = 90f;

    private void Awake()
    {
        // Reset Z so all pipes start aligned
        Vector3 euler = transform.localEulerAngles;
        euler.z = 90f;
        transform.localRotation = Quaternion.Euler(euler);
    }

    // Called by the button to rotate this pipe by the configured step
    public void RotateOnce()
    {
        Vector3 euler = transform.localEulerAngles;
        euler.z = Mathf.Repeat(euler.z + stepDegrees, 360f);
        transform.localRotation = Quaternion.Euler(euler);
    }
}
