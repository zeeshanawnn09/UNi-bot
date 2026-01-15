using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(RBBodyMovement))]
public class RBAudioPlayer : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip jumpClip;
    public AudioClip landClip;

    [Header("Slide Audio")]
    [SerializeField] private string slideGroundTag = "Slide";
    [SerializeField] private float slideMinSpeed = 3f;   // min horizontal speed to trigger slide sound
    [SerializeField] private AudioClip slideClip;

    private AudioSource _audio;
    private RBBodyMovement _movement;

    private bool _wasOnSlide;

    void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _movement = GetComponent<RBBodyMovement>();

        _audio.playOnAwake = false;

        _wasOnSlide = IsOnSlideNow();
    }

    void FixedUpdate()
    {
        // --- Jump / Land ---
        if (_movement.JustJumped && jumpClip != null)
        {
            _audio.PlayOneShot(jumpClip);
        }

        if (_movement.JustLanded && landClip != null)
        {
            _audio.PlayOneShot(landClip);
        }

        // --- Slide enter sound ---
        bool onSlideNow = IsOnSlideNow();
        float horizontalSpeed = new Vector3(
            _movement.CurrentVelocity.x,
            0f,
            _movement.CurrentVelocity.z
        ).magnitude;

        // Play slide sound ON ENTER slide, and only if speed is high enough
        if (!_wasOnSlide && onSlideNow && horizontalSpeed >= slideMinSpeed && slideClip != null)
        {
            _audio.PlayOneShot(slideClip);
        }

        _wasOnSlide = onSlideNow;
    }

    private bool IsOnSlideNow()
    {
        // Requires RBBodyMovement.CurrentGroundTag from the version we added earlier
        string tag = _movement.CurrentGroundTag;
        return !string.IsNullOrEmpty(tag) && tag == slideGroundTag;
    }
}
