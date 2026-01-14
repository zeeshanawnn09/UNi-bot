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

        sprint = false;
        jump = false;
        move = Vector2.zero;
    }

    public void OnMove(InputValue value)
    {
        MoveInput(value.Get<Vector2>());
    }

    public void OnJump(InputValue value)
    {
        JumpInput(value.isPressed);
    }

    public void OnSprint(InputValue value)
    {
        SprintInput(value.isPressed);
    }

    private void OnSprintPerformed(InputAction.CallbackContext ctx)
    {
        SprintInput(true);
    }

    private void OnSprintCanceled(InputAction.CallbackContext ctx)
    {
        SprintInput(false);
    }

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
