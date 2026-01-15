using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class CCFootstepAudio : MonoBehaviour
{
    [Serializable]
    public class TagFootstepSet
    {
        public string tag;           // e.g. "Grass", "Stone"
        public AudioClip[] clips;    // one or more clips for this surface
    }

    [Header("References")]
    [SerializeField] private CCProceduralAnimation proceduralAnimation;
    [SerializeField] private AudioSource audioSource;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float rayUpOffset = 0.15f;
    [SerializeField] private float rayDownDistance = 0.6f;

    [Header("Footstep Sets")]
    [SerializeField] private TagFootstepSet[] footstepSets;

    private void Reset()
    {
        audioSource = GetComponent<AudioSource>();
        proceduralAnimation = GetComponentInParent<CCProceduralAnimation>();
    }

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (proceduralAnimation == null)
            proceduralAnimation = GetComponentInParent<CCProceduralAnimation>();

        if (audioSource != null)
            audioSource.playOnAwake = false;
    }

    private void OnEnable()
    {
        if (proceduralAnimation != null)
            proceduralAnimation.OnStepFinished += HandleStepFinished;
    }

    private void OnDisable()
    {
        if (proceduralAnimation != null)
            proceduralAnimation.OnStepFinished -= HandleStepFinished;
    }

    // Matches EventHandler<Vector3>: (object sender, Vector3 footPos)
    private void HandleStepFinished(object sender, Vector3 footWorldPos)
    {
        if (audioSource == null) return;

        string tag = GetGroundTagAt(footWorldPos);
        AudioClip clip = GetClipForTag(tag);

        if (clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private string GetGroundTagAt(Vector3 footWorldPos)
    {
        Vector3 origin = footWorldPos + Vector3.up * rayUpOffset;

        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit,
                            rayDownDistance, groundMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider != null ? hit.collider.tag : null;
        }

        return null;
    }

    private AudioClip GetClipForTag(string tag)
    {
        if (string.IsNullOrEmpty(tag) || footstepSets == null)
            return null;

        for (int i = 0; i < footstepSets.Length; i++)
        {
            var set = footstepSets[i];
            if (set == null || set.clips == null || set.clips.Length == 0)
                continue;

            if (!string.Equals(set.tag, tag, StringComparison.OrdinalIgnoreCase))
                continue;

            int index = UnityEngine.Random.Range(0, set.clips.Length);
            return set.clips[index];
        }

        return null;
    }
}
