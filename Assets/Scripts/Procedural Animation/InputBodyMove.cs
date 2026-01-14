using UnityEngine;
using UnityEngine.InputSystem;

public class InputBodyMove : MonoBehaviour
{
    [Header("Character Input Values")]
    public Vector2 move;
    public bool jump;
    public bool sprint;

    [Header("Movement Settings")]
    public bool analogMovement = true;

    [Header("Mouse Cursor Settings")]
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;

    [Header("Optional: PlayerInput (for reliable Sprint release)")]
    [SerializeField] private PlayerInput playerInput;

    private InputAction _sprintAction;

    private void Awake()
    {
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();

        if (playerInput != null && playerInput.actions != null)
            _sprintAction = playerInput.actions.FindAction("Sprint", throwIfNotFound: false);
    }

    private void OnEnable()
    {
        // Subscribe directly so we always get canceled (release)
        if (_sprintAction != null)
        {
            _sprintAction.performed += OnSprintPerformed;
            _sprintAction.canceled += OnSprintCanceled;

            if (!_sprintAction.enabled)
                _sprintAction.Enable();
        }
    }

    private void OnDisable()
    {
        if (_sprintAction != null)
        {
            _sprintAction.performed -= OnSprintPerformed;
            _sprintAction.canceled -= OnSprintCanceled;
        }

        // Safety: avoid stuck inputs if object disables mid-hold
        sprint = false;
        jump = false;
        move = Vector2.zero;
    }

    // --- PlayerInput "Send Messages" callbacks (kept for compatibility) ---

    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }

    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }

    // Keep this so Send Messages can find the method (prevents MissingMethodException).
    // If your setup DOES call it on release, this will work too.
    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }

    // --- Reliable sprint callbacks (performed/canceled) ---

    private void OnSprintPerformed(InputAction.CallbackContext ctx)
    {
        SprintInput(true);
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        SprintInput(false);
    }

    // --- Setters ---

    public void MoveInput(Vector2 newMoveDirection)
    {
        move = newMoveDirection;
    }

    public void JumpInput(bool newJumpState)
    {
        jump = newJumpState;
    }

    public void SprintInput(bool newSprintState)
    {
        sprint = newSprintState;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    }
}
