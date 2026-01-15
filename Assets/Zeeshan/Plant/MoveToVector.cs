using UnityEngine;

public class MoveToVector : MonoBehaviour
{
    public Transform target;
    public Vector3 from;
    public Vector3 to;
    public float duration = 2f;

    private float t;
    private bool moving;

    // Call this from a button or other script
    public void Move()
    {
        if (!target) return;

        target.position = from;
        t = 0f;
        moving = true;
    }

    void Update()
    {
        if (!moving) return;

        if (duration <= 0f)
        {
            target.position = to;
            moving = false;
            return;
        }

        t += Time.deltaTime / duration;
        if (t >= 1f)
        {
            t = 1f;
            target.position = to;
            moving = false; // stops automatically
            return;
        }

        target.position = Vector3.Lerp(from, to, t);
    }
}
