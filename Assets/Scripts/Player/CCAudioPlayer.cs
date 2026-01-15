using UnityEngine;

[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(CCBodyMovement))]
public class CCAudioPlayer : MonoBehaviour
{
    [Header("Clips")]
    [SerializeField] private AudioClip jumpClip;
    [SerializeField] private AudioClip landClip;

    private AudioSource _audio;
    private CCBodyMovement _movement;

    private void Awake()
    {
        _audio = GetComponent<AudioSource>();
        _movement = GetComponent<CCBodyMovement>();

        if (_audio != null)
            _audio.playOnAwake = false;
    }

    private void Update()
    {
        if (_audio == null || _movement == null) return;

        if (_movement.JustJumped && jumpClip != null)
        {
            _audio.PlayOneShot(jumpClip);
        }

        if (_movement.JustLanded && landClip != null)
        {
            _audio.PlayOneShot(landClip);
        }
    }
}
