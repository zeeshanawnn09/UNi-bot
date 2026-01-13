using UnityEngine;

public class DoorBehavior : MonoBehaviour
{
    [SerializeField] private PipeRotationBehavior[] pipes;
    [SerializeField] private float targetPipeZ = 90f;
    [SerializeField] private float targetDoorX = 17f;
    [SerializeField] private float angleTolerance = 0.5f;

    private bool opened;

    private void Awake()
    {
        opened = false;
    }

    private void Update()
    {
        if (opened) return;
        if (pipes == null || pipes.Length == 0) return;

        if (AllPipesAligned())
        {
            Debug.Log("All pipes aligned! Moving door to X = " + targetDoorX);
            Vector3 pos = transform.localPosition;
            pos.x = targetDoorX;
            transform.localPosition = pos;
            opened = true;
        }
    }

    private bool AllPipesAligned()
    {
        foreach (var pipe in pipes)
        {
            if (pipe == null) return false;
            float z = NormalizeAngle(pipe.transform.localEulerAngles.z);

            // Check if pipe is at 0/360/-360 degrees (all normalize to 0)
            bool atZero = Mathf.Abs(z) <= angleTolerance || Mathf.Abs(z - 360f) <= angleTolerance;

            if (!atZero)
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