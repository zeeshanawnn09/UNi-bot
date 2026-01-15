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

    float moveT;
    float pauseTimer;
    bool moving;
    bool doorsPaused;

    // Hook this to your UI Button OnClick()
    public void Play()
    {
        if (!elevator) return;

        // Reset state
        moveT = 0f;
        pauseTimer = 0f;
        moving = true;
        doorsPaused = false;

        // Start position
        elevator.position = elevatorPos1;

        // As soon as elevator starts moving: Close = true
        SetBool(leftDoor, closeParam, true);
        SetBool(rightDoor, closeParam, true);

        // Optional: make sure other params are in a safe state
        SetBool(leftDoor, openParam, false);
        SetBool(rightDoor, openParam, false);
        ResetTrigger(leftDoor, copenParam);
        ResetTrigger(rightDoor, copenParam);

        // Ensure doors are unpaused at start
        SetPaused(leftDoor, false);
        SetPaused(rightDoor, false);
    }

    void Update()
    {
        if (!moving) return;

        // Door pause timer (pause ALL door animations after X seconds)
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
        // Unpause door animations
        SetPaused(leftDoor, false);
        SetPaused(rightDoor, false);

        // Trigger Copen
        SetTrigger(leftDoor, copenParam);
        SetTrigger(rightDoor, copenParam);

        // Set Open = false so it returns to Idle (your Idle looks "door open")
        SetBool(leftDoor, openParam, false);
        SetBool(rightDoor, openParam, false);

        // Usually also turn off Close once opening starts
        SetBool(leftDoor, closeParam, false);
        SetBool(rightDoor, closeParam, false);
    }

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
