using System;
using UnityEngine;

[DisallowMultipleComponent]
public class CCFootstepAudio : MonoBehaviour
{
    [Serializable]
    public struct TagSound
    {
        public string tag;      // e.g. "Stone", "Wood"
        public AudioClip clip;
    }

    [Header("References")]
    [SerializeField] private CCProceduralAnimation proc;

    [Header("Clips")]
    [SerializeField] private AudioClip defaultClip;   // used for Untagged / no match
    [SerializeField] private TagSound[] tagSounds;

    [Header("Ground Detection")]
    [SerializeField] private LayerMask groundMask = 0;  // leave 0 to use proc's layerMask
    [SerializeField] private float rayUp = 0.5f;
    [SerializeField] private float rayDown = 2.0f;

    [Header("Playback")]
    [SerializeField] private float volume = 1f;
    [SerializeField] private float minInterval = 0.08f;
    [SerializeField] private bool debugLogs = true;

    private float _lastPlayTime;

    private void Awake()
    {
        if (!proc) proc = GetComponent<CCProceduralAnimation>();

        // If you didn't set groundMask here, use the same mask the procedural script uses.
        if (groundMask.value == 0 && proc != null)
            groundMask = proc.GetLayerMask();
    }

    private void OnEnable()
    {
        if (proc != null)
            proc.OnStepFinished += HandleStep;
    }

    private void OnDisable()
    {
        if (proc != null)
            proc.OnStepFinished -= HandleStep;
    }

    private void HandleStep(Vector3 footPos)
    {
        if (Time.time - _lastPlayTime < minInterval)
            return;

        _lastPlayTime = Time.time;

        string groundTag = ResolveGroundTag(footPos);
        AudioClip clip = PickClip(groundTag);

        if (clip == null)
        {
            if (debugLogs) Debug.LogWarning("[CCFootstepAudio] No clip assigned (defaultClip is null and no tag match).", this);
            return;
        }

        if (debugLogs)
            Debug.Log($"[CCFootstepAudio] Step groundTag={groundTag} clip={clip.name}", this);

        AudioSource.PlayClipAtPoint(clip, footPos, volume);
    }

    private string ResolveGroundTag(Vector3 footPos)
    {
        Vector3 start = footPos + Vector3.up * rayUp;

        if (Physics.Raycast(start, Vector3.down, out RaycastHit hit, rayUp + rayDown, groundMask, QueryTriggerInteraction.Ignore))
            return hit.collider.tag;

        return "Untagged";
    }

    private AudioClip PickClip(string tag)
    {
        bool isUntagged = string.IsNullOrEmpty(tag) || tag == "Untagged";
        if (isUntagged)
            return defaultClip;

        for (int i = 0; i < tagSounds.Length; i++)
        {
            if (!string.IsNullOrEmpty(tagSounds[i].tag) && tagSounds[i].tag == tag)
                return tagSounds[i].clip != null ? tagSounds[i].clip : defaultClip;
        }

        return defaultClip;
    }
}
