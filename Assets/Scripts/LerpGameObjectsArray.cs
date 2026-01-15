using UnityEngine;

public class ElevatorDoorsSequence : MonoBehaviour
{
    [Header("Elevator")]
    public Transform elevator;
    public Vector3 elevatorPos1;
    public Vector3 elevatorPos2;
    public float elevatorDuration = 2f;

    [Header("Doors (Animators)")]
    public Animator leftDoor;
    public Animator rightDoor;

    [Header("Animator params")]
    public string openParam = "Open";
    public string closeParam = "Close";
    public string copenParam = "Copen"; // trigger

    [Header("Door timing")]
    [Tooltip("Seconds after elevator starts moving to pause ALL door animations")]
    public float pauseDoorsAfterSeconds = 2f;

    [Header("Elevator Audio")]
    [SerializeField] private AudioSource elevatorAudioSource;
    [SerializeField] private AudioClip moveClip;
    [SerializeField] private float audioStartTime = 5f;  // seconds
    [SerializeField] private float audioEndTime = 20f; // seconds

    float moveT;
    float pauseTimer;
    bool moving;
    bool doorsPaused;

    Coroutine elevatorAudioRoutine;

    void Awake()
    {
        if (elevatorAudioSource != null)
            elevatorAudioSource.playOnAwake = false;
    }

    // Hook this to your UI Button OnClick()
    public void Play()
    {
        if (!elevator) return;

        moveT = 0f;
        pauseTimer = 0f;
        moving = true;
        doorsPaused = false;

        elevator.position = elevatorPos1;

        SetBool(leftDoor, closeParam, true);
        SetBool(rightDoor, closeParam, true);

        SetBool(leftDoor, openParam, false);
        SetBool(rightDoor, openParam, false);
        ResetTrigger(leftDoor, copenParam);
        ResetTrigger(rightDoor, copenParam);

        SetPaused(leftDoor, false);
        SetPaused(rightDoor, false);

        StartElevatorAudioSegment();
    }

    void Update()
    {
        if (!moving) return;

        // Door pause
        pauseTimer += Time.deltaTime;
        if (!doorsPaused && pauseTimer >= pauseDoorsAfterSeconds)
        {
            doorsPaused = true;
            SetPaused(leftDoor, true);
            SetPaused(rightDoor, true);
        }

        // Elevator movement
        if (elevatorDuration <= 0f)
        {
            elevator.position = elevatorPos2;
            moving = false;
            OnArrived();
            return;
        }

        moveT += Time.deltaTime / elevatorDuration;
        if (moveT >= 1f)
        {
            moveT = 1f;
            elevator.position = elevatorPos2;
            moving = false;
            OnArrived();
            return;
        }

        elevator.position = Vector3.Lerp(elevatorPos1, elevatorPos2, moveT);
    }

    void OnArrived()
    {
        StopElevatorAudioSegment();

        SetPaused(leftDoor, false);
        SetPaused(rightDoor, false);

        SetTrigger(leftDoor, copenParam);
        SetTrigger(rightDoor, copenParam);

        SetBool(leftDoor, openParam, false);
        SetBool(rightDoor, openParam, false);

        SetBool(leftDoor, closeParam, false);
        SetBool(rightDoor, closeParam, false);
    }

    // -------- AUDIO SEGMENT 5s → 20s --------

    void StartElevatorAudioSegment()
    {
        if (elevatorAudioSource == null || moveClip == null) return;

        if (elevatorAudioRoutine != null)
            StopCoroutine(elevatorAudioRoutine);

        elevatorAudioRoutine = StartCoroutine(PlayElevatorSegmentRoutine());
    }

    void StopElevatorAudioSegment()
    {
        if (elevatorAudioRoutine != null)
        {
            StopCoroutine(elevatorAudioRoutine);
            elevatorAudioRoutine = null;
        }

        if (elevatorAudioSource != null)
            elevatorAudioSource.Stop();
    }

    System.Collections.IEnumerator PlayElevatorSegmentRoutine()
    {
        float clipLen = moveClip.length;

        float start = Mathf.Clamp(audioStartTime, 0f, clipLen);
        float end = Mathf.Clamp(audioEndTime, start, clipLen);

        elevatorAudioSource.loop = false;
        elevatorAudioSource.clip = moveClip;
        elevatorAudioSource.time = start;
        elevatorAudioSource.Play();

        // Run until time reaches end of segment or clip stops
        while (elevatorAudioSource.isPlaying && elevatorAudioSource.time < end)
        {
            yield return null;
        }

        elevatorAudioSource.Stop();
        elevatorAudioRoutine = null;
    }

    // -------- Helpers --------
    void SetPaused(Animator a, bool paused)
    {
        if (!a) return;
        a.speed = paused ? 0f : 1f;
    }

    void SetBool(Animator a, string param, bool value)
    {
        if (!a || string.IsNullOrEmpty(param)) return;
        a.SetBool(param, value);
    }

    void SetTrigger(Animator a, string param)
    {
        if (!a || string.IsNullOrEmpty(param)) return;
        a.SetTrigger(param);
    }

    void ResetTrigger(Animator a, string param)
    {
        if (!a || string.IsNullOrEmpty(param)) return;
        a.ResetTrigger(param);
    }
}
