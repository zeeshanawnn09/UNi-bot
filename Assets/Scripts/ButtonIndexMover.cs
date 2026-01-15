using System.Collections;
using UnityEngine;

public class ButtonIndexMover : MonoBehaviour
{
    [Header("Button3D hookup")]
    [SerializeField] private Button3D button3D;   // assign Plant Button Holder here
    [SerializeField] private int buttonIndex = 0; // which Button3D index triggers THIS plant

    [Header("Positions")]
    public Vector3 pos1; // from
    public Vector3 pos2; // to

    [Header("Timing")]
    [Tooltip("Wait this long after E is pressed before movement starts.")]
    public float startDelay = 0.25f;

    [Tooltip("How long the movement takes (lerp duration).")]
    public float duration = 0.35f;

    [Header("Space")]
    public bool useLocalSpace = false;

    [Header("Behavior")]
    [Tooltip("If true: pressing toggles between pos1 and pos2. If false: always moves to pos2.")]
    public bool toggle = false;

    private Coroutine running;
    private bool atPos2;

    private void Reset()
    {
        // default pos1 to current position
        if (useLocalSpace) pos1 = transform.localPosition;
        else pos1 = transform.position;
    }

    private void OnEnable()
    {
        if (button3D != null)
            button3D.onButtonPressed.AddListener(OnButtonPressed);
    }

    private void OnDisable()
    {
        if (button3D != null)
            button3D.onButtonPressed.RemoveListener(OnButtonPressed);
    }

    private void OnButtonPressed(int index)
    {
        if (index != buttonIndex) return;

        Vector3 from, to;

        if (toggle)
        {
            if (atPos2) { from = pos2; to = pos1; }
            else { from = pos1; to = pos2; }
        }
        else
        {
            from = pos1;
            to = pos2;
        }

        if (running != null) StopCoroutine(running);
        running = StartCoroutine(DelayedMoveRoutine(from, to));

        atPos2 = (to == pos2);
    }

    private IEnumerator DelayedMoveRoutine(Vector3 from, Vector3 to)
    {
        // ensure known start position
        if (useLocalSpace) transform.localPosition = from;
        else transform.position = from;

        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        float dur = Mathf.Max(0.0001f, duration);
        float time = 0f;

        while (time < dur)
        {
            time += Time.deltaTime;
            float a = Mathf.Clamp01(time / dur);

            Vector3 p = Vector3.Lerp(from, to, a);
            if (useLocalSpace) transform.localPosition = p;
            else transform.position = p;

            yield return null;
        }

        if (useLocalSpace) transform.localPosition = to;
        else transform.position = to;

        running = null;
    }
}
