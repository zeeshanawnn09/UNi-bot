using System.Collections;
using UnityEngine;
using TMPro;

public class NPCDialogueTypewriter3D : MonoBehaviour
{
    // One NPC owns the dialogue at a time
    private static NPCDialogueTypewriter3D s_activeNPC;

    // One NPC owns the shared prompt at a time (closest in range)
    private static NPCDialogueTypewriter3D s_promptOwner;
    private static int s_promptFrame = -1;
    private static float s_promptBestDist = float.MaxValue;
    private static TMP_Text s_sharedPromptText;   // the actual shared TMP reference
    private static string s_sharedPromptMessage;  // last message set

    [Header("Detection (3D)")]
    public Transform interactionPoint;
    public float interactRadius = 2f;
    public LayerMask playerLayer;

    [Header("Input")]
    public KeyCode interactKey = KeyCode.E;
    public KeyCode closeKey = KeyCode.Escape;

    [Header("UI (Shared)")]
    public GameObject panel;
    public TMP_Text dialogueText;

    [Header("Prompt UI (Shared)")]
    public TMP_Text promptText; // assign SAME prompt TMP for all NPCs
    public string promptMessage = "Press E";

    [Header("Dialogue")]
    [TextArea(2, 5)]
    public string[] lines;
    public float textSpeed = 0.03f;
    public bool allowRepeat = true;

    [Header("Audio (plays while a line is typing)")]
    public AudioSource voiceSource;
    public AudioClip maleClip;
    public AudioClip femaleClip;
    public enum VoiceType { Male, Female }
    public VoiceType voiceType = VoiceType.Male;
    public bool loopWhileTyping = true;

    private int _lineIndex = 0;
    private bool _isTyping = false;
    private bool _seenAll = false;
    private Coroutine _typingRoutine;

    private void Reset()
    {
        interactionPoint = transform;
    }

    private void Awake()
    {
        if (interactionPoint == null) interactionPoint = transform;

        // Register shared prompt TMP once (first NPC that has it assigned)
        if (promptText != null && s_sharedPromptText == null)
        {
            s_sharedPromptText = promptText;
            s_sharedPromptText.gameObject.SetActive(false);
            s_sharedPromptText.text = promptMessage;
            s_sharedPromptMessage = promptMessage;
        }

        if (panel != null) panel.SetActive(false);
        if (dialogueText != null) dialogueText.text = "";
    }

    private void OnDisable()
    {
        if (s_activeNPC == this) s_activeNPC = null;
        if (s_promptOwner == this) s_promptOwner = null;
    }

    private void Update()
    {
        // If a dialogue is open, hide shared prompt and only let active NPC run
        if (s_activeNPC != null && IsPanelOpen())
        {
            SetSharedPromptVisible(false);

            if (s_activeNPC != this)
                return;
        }

        // Find player in range (3D)
        bool inRange = TryGetPlayerInRange(out Vector3 playerPos);

        // Pick prompt owner (closest NPC) ONLY when no dialogue is open
        if (s_activeNPC == null && !IsPanelOpen())
        {
            UpdateSharedPromptOwnerSelection(inRange, playerPos);
        }
        else
        {
            SetSharedPromptVisible(false);
        }

        bool iAmActive = (s_activeNPC == this);

        // Close dialogue (treat as all seen) - only active NPC
        if (iAmActive && IsPanelOpen() && Input.GetKeyDown(closeKey))
        {
            CloseDialogue(treatAsAllSeen: true);
            return;
        }

        if (Input.GetKeyDown(interactKey))
        {
            // Start dialogue (ONLY prompt owner when panel is closed)
            if (!IsPanelOpen())
            {
                if (!inRange) return;

                // If shared prompt exists, ONLY the selected prompt owner can start
                if (s_sharedPromptText != null && s_promptOwner != this) return;

                if (_seenAll && !allowRepeat) return;

                s_activeNPC = this;
                StartDialogue();
                SetSharedPromptVisible(false);
                return;
            }

            // Panel is open: only active NPC reaches here
            if (_isTyping) SkipTyping();
            else NextLineOrFinish();
        }
    }

    private bool IsPanelOpen()
    {
        return panel != null && panel.activeSelf;
    }

    private bool TryGetPlayerInRange(out Vector3 playerPos)
    {
        playerPos = Vector3.zero;

        Vector3 center = interactionPoint != null ? interactionPoint.position : transform.position;

        Collider[] hits = Physics.OverlapSphere(
            center,
            interactRadius,
            playerLayer,
            QueryTriggerInteraction.Collide
        );

        if (hits == null || hits.Length == 0)
            return false;

        // Use the closest hit as player position reference (works even if multiple colliders)
        float best = float.MaxValue;
        int bestIndex = 0;
        for (int i = 0; i < hits.Length; i++)
        {
            float d = (hits[i].transform.position - center).sqrMagnitude;
            if (d < best)
            {
                best = d;
                bestIndex = i;
            }
        }

        playerPos = hits[bestIndex].transform.position;
        return true;
    }

    private void UpdateSharedPromptOwnerSelection(bool inRange, Vector3 playerPos)
    {
        // Reset selection once per frame
        if (Time.frameCount != s_promptFrame)
        {
            s_promptFrame = Time.frameCount;
            s_promptOwner = null;
            s_promptBestDist = float.MaxValue;
            SetSharedPromptVisible(false);
        }

        if (s_sharedPromptText == null) return;
        if (!inRange) return;
        if (_seenAll && !allowRepeat) return;

        Vector3 center = interactionPoint != null ? interactionPoint.position : transform.position;
        float dist = Vector3.Distance(playerPos, center);

        if (dist < s_promptBestDist)
        {
            s_promptBestDist = dist;
            s_promptOwner = this;

            s_sharedPromptMessage = promptMessage;
            s_sharedPromptText.text = s_sharedPromptMessage;
            SetSharedPromptVisible(true);
        }
    }

    private void SetSharedPromptVisible(bool visible)
    {
        if (s_sharedPromptText == null) return;
        if (s_sharedPromptText.gameObject.activeSelf != visible)
            s_sharedPromptText.gameObject.SetActive(visible);
    }

    private void StartDialogue()
    {
        if (panel == null || dialogueText == null)
        {
            CloseDialogue(treatAsAllSeen: true);
            return;
        }

        if (lines == null || lines.Length == 0)
        {
            CloseDialogue(treatAsAllSeen: true);
            return;
        }

        if (allowRepeat) _seenAll = false;

        _lineIndex = 0;
        panel.SetActive(true);
        StartTypingLine(lines[_lineIndex]);
    }

    private void StartTypingLine(string line)
    {
        StopTypingRoutine();
        dialogueText.text = "";
        _typingRoutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(string line)
    {
        _isTyping = true;
        PlayVoice();

        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSeconds(textSpeed);
        }

        StopVoice();
        _isTyping = false;
        _typingRoutine = null;
    }

    private void SkipTyping()
    {
        if (!_isTyping) return;

        StopTypingRoutine();
        dialogueText.text = (_lineIndex >= 0 && lines != null && _lineIndex < lines.Length) ? lines[_lineIndex] : "";
        StopVoice();
        _isTyping = false;
    }

    private void NextLineOrFinish()
    {
        if (lines == null || lines.Length == 0)
        {
            CloseDialogue(treatAsAllSeen: true);
            return;
        }

        if (_lineIndex < lines.Length - 1)
        {
            _lineIndex++;
            StartTypingLine(lines[_lineIndex]);
        }
        else
        {
            CloseDialogue(treatAsAllSeen: true);
        }
    }

    private void CloseDialogue(bool treatAsAllSeen)
    {
        StopTypingRoutine();
        StopVoice();
        _isTyping = false;

        if (dialogueText != null) dialogueText.text = "";
        if (panel != null) panel.SetActive(false);

        if (treatAsAllSeen)
        {
            _seenAll = true;
            _lineIndex = (lines != null) ? lines.Length : 0;
        }
        else
        {
            _lineIndex = 0;
        }

        if (s_activeNPC == this) s_activeNPC = null;
    }

    private void StopTypingRoutine()
    {
        if (_typingRoutine != null)
        {
            StopCoroutine(_typingRoutine);
            _typingRoutine = null;
        }
    }

    private void PlayVoice()
    {
        if (voiceSource == null) return;

        AudioClip clip = (voiceType == VoiceType.Male) ? maleClip : femaleClip;
        if (clip == null) return;

        voiceSource.clip = clip;
        voiceSource.loop = loopWhileTyping;
        voiceSource.Stop();
        voiceSource.Play();
    }

    private void StopVoice()
    {
        if (voiceSource == null) return;
        if (voiceSource.isPlaying) voiceSource.Stop();
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 center = interactionPoint != null ? interactionPoint.position : transform.position;
        Gizmos.DrawWireSphere(center, interactRadius);
    }
}
