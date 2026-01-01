using UnityEngine;

public class DoorBehavior : MonoBehaviour
{
    [SerializeField] private PipeRotationBehavior[] pipes;
    [SerializeField] private float targetPipeZ = 90f;
    [SerializeField] private float targetDoorY = 90f;
    [SerializeField] private float angleTolerance = 0.5f;

    private bool opened;

    private void Awake()
    {
        // Start with door aligned at Y = 0
        Vector3 euler = transform.localEulerAngles;
        euler.y = 0f;
        transform.localRotation = Quaternion.Euler(euler);
        opened = false;
    }

    private void Update()
    {
        if (opened) return;
        if (pipes == null || pipes.Length == 0) return;

        if (AllPipesAligned())
        {
            Vector3 euler = transform.localEulerAngles;
            euler.y = targetDoorY;
            transform.localRotation = Quaternion.Euler(euler);
            opened = true;
        }
    }

    private bool AllPipesAligned()
    {
        foreach (var pipe in pipes)
        {
            if (pipe == null) return false;
            float z = NormalizeAngle(pipe.transform.localEulerAngles.z);
            if (Mathf.Abs(z - targetPipeZ) > angleTolerance)
            {
                return false;
            }
        }
        return true;
    }

    private float NormalizeAngle(float degrees)
    {
        // Map any angle to [0,360)
        degrees %= 360f;
        if (degrees < 0f) degrees += 360f;
        return degrees;
    }
}
