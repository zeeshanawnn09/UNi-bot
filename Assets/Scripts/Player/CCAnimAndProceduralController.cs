using UnityEngine;
using UnityEngine.Animations.Rigging;

[DisallowMultipleComponent]
public class CCAnimAndProceduralController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator animator;
    [SerializeField] private CCBodyMovement bodyMovement;
    [SerializeField] private RigBuilder rigBuilder;

    [Header("Camera Dolly / Cinematic")]
    [SerializeField] private CameraController cameraController;
    [Tooltip("If <= 0, will use plantingLockSeconds as the dolly duration.")]
    [SerializeField] private float plantingDollyDuration = -1f;

    [Header("Animator Params (must match your Animator)")]
    [SerializeField] private string plantingTrigger = "Planting";
    [SerializeField] private string plantingStateName = "Planting";
    [SerializeField] private string midAirBool = "MidAir";        // Bool
    [SerializeField] private string idleBool = "Idle";            // Bool
    [SerializeField] private string jumpedTrigger = "Jumped";     // Trigger

    [Header("Idle")]
    [SerializeField] private float idleDelaySeconds = 1.0f;
    [SerializeField] private float moveSpeedThreshold = 0.15f;

    [Header("RigBuilder Enable Rule")]
    [SerializeField] private bool rigOnlyWhenGrounded = true;

    [Header("Planting Lock (Timer)")]
    [Tooltip("Minimum time to keep movement OFF + rig OFF after starting planting.")]
    [SerializeField] private float plantingLockSeconds = 1.2f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip diggingAudioClip;

    private float _idleTimer = 0f;
    private bool _wasGrounded = true;

    // planting is handled purely by this lock, not by an Animator Bool
    private bool _isPlantingLock = false;
    private float _plantingLockTimer = 0f;

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        bodyMovement = GetComponent<CCBodyMovement>();
        rigBuilder = GetComponentInChildren<RigBuilder>();
        audioSource = GetComponentInChildren<AudioSource>();
    }

    private void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (!bodyMovement) bodyMovement = GetComponent<CCBodyMovement>();
        if (!rigBuilder) rigBuilder = GetComponentInChildren<RigBuilder>();

        if (!animator) Debug.LogError("CCAnimAndProceduralController: Missing Animator (child).", this);
        if (!bodyMovement) Debug.LogError("CCAnimAndProceduralController: Missing CCBodyMovement (same GO).", this);
        if (!rigBuilder) Debug.LogWarning("CCAnimAndProceduralController: Missing RigBuilder (child).", this);

        if (audioSource != null) audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (bodyMovement) _wasGrounded = bodyMovement.IsGrounded;

        _idleTimer = 0f;
        SetBoolSafe(idleBool, false);

        _isPlantingLock = false;
        _plantingLockTimer = 0f;
    }

    private void LateUpdate()
    {
        if (!animator || !bodyMovement) return;

        bool grounded = bodyMovement.IsGrounded;

        // --- planting lock timer ---
        if (_isPlantingLock)
        {
            _plantingLockTimer -= Time.deltaTime;
            if (_plantingLockTimer <= 0f)
            {
                _isPlantingLock = false;
                _plantingLockTimer = 0f;

                // re-enable movement
                if (bodyMovement) bodyMovement.enabled = true;

                // END Dolly / cinematic here
                if (cameraController != null)
                    cameraController.EndCinematic();
            }
        }

        // --- movement read ---
        Vector3 v = bodyMovement.CurrentVelocity;
        float horizontalSpeed = new Vector3(v.x, 0f, v.z).magnitude;
        bool moving = horizontalSpeed > moveSpeedThreshold;

        // --- animator values ---
        SetBoolSafe(midAirBool, !grounded);

        if (_wasGrounded && !grounded)
            SetTriggerSafe(jumpedTrigger);
        else if (!_wasGrounded && grounded)
            ResetTriggerSafe(jumpedTrigger);

        _wasGrounded = grounded;

        bool planting = _isPlantingLock;

        // --- Idle (never while planting/lock) ---
        bool idle = GetBoolSafe(idleBool);

        if (planting || !grounded || moving)
        {
            _idleTimer = 0f;
            if (idle) SetBoolSafe(idleBool, false);
        }
        else
        {
            if (!idle)
            {
                _idleTimer += Time.deltaTime;
                if (_idleTimer >= idleDelaySeconds)
                {
                    SetBoolSafe(idleBool, true);
                    _idleTimer = idleDelaySeconds;
                }
            }
        }

        idle = GetBoolSafe(idleBool);

        // --- RigBuilder on/off ---
        if (rigBuilder)
        {
            bool allowRig = true;

            if (planting) allowRig = false;
            if (rigOnlyWhenGrounded && !grounded) allowRig = false;
            if (idle) allowRig = false;

            SetRigEnabled(allowRig);
        }
    }

    // Called from Button3D event
    public void StartPlantingSequence()
    {
        if (!animator)
            return;

        // only allow while grounded
        if (bodyMovement && !bodyMovement.IsGrounded)
            return;

        // --- lock movement + rig (but allow re-entry) ---
        _isPlantingLock = true;
        _plantingLockTimer = Mathf.Max(0.01f, plantingLockSeconds);

        if (bodyMovement) bodyMovement.enabled = false;
        SetRigEnabled(false);

        SetBoolSafe(idleBool, false);
        _idleTimer = 0f;

        // camera dolly
        if (cameraController != null)
        {
            float dur = plantingDollyDuration > 0f
                ? plantingDollyDuration
                : plantingLockSeconds;

            cameraController.StartCinematic(dur);
        }

        // optional sound
        if (audioSource != null && diggingAudioClip != null)
            audioSource.PlayOneShot(diggingAudioClip);

        // --- FORCE the animation to restart every time ---

        // 1) re-fire the trigger
        animator.ResetTrigger(plantingTrigger);
        animator.SetTrigger(plantingTrigger);

        // 2) hard crossfade to the planting state (base layer, time = 0)
        if (!string.IsNullOrEmpty(plantingTrigger))
            animator.CrossFadeInFixedTime(plantingTrigger, 0.1f, 0, 0f);
    }


    private void SetRigEnabled(bool enabled)
    {
        if (!rigBuilder) return;
        if (rigBuilder.enabled == enabled) return;

        rigBuilder.enabled = enabled;
        if (enabled) rigBuilder.Build();
    }

    // --- Safe Animator helpers ---
    private bool HasParam(string name, AnimatorControllerParameterType type)
    {
        if (string.IsNullOrEmpty(name) || !animator) return false;
        foreach (var p in animator.parameters)
        {
            if (p.type == type && p.name == name)
                return true;
        }
        return false;
    }

    private bool GetBoolSafe(string name)
    {
        return animator && HasParam(name, AnimatorControllerParameterType.Bool) && animator.GetBool(name);
    }

    private void SetBoolSafe(string name, bool value)
    {
        if (animator && HasParam(name, AnimatorControllerParameterType.Bool))
            animator.SetBool(name, value);
    }

    private void SetTriggerSafe(string name)
    {
        if (animator && HasParam(name, AnimatorControllerParameterType.Trigger))
            animator.SetTrigger(name);
    }

    private void ResetTriggerSafe(string name)
    {
        if (animator && HasParam(name, AnimatorControllerParameterType.Trigger))
            animator.ResetTrigger(name);
    }
}
