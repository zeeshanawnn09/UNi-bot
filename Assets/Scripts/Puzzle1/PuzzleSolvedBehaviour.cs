using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

public class PuzzleSolvedBehaviour : MonoBehaviour
{
    [Header("Fade Material Alpha")]
    [Tooltip("Renderer whose material alpha will be changed.")]
    [SerializeField] private Renderer targetRenderer;

    [Tooltip("Which material slot to edit (0 = first).")]
    [SerializeField] private int materialIndex = 0;

    [SerializeField] private float startAlpha = 0.7f;
    [SerializeField] private float endAlpha = 1.0f;
    [SerializeField] private float durationSeconds = 9.0f;

    [Header("Timeline")]
    [SerializeField] private PlayableDirector timelineDirector;

    [Header("Elevator Buttons")]
    [SerializeField] private GameObject ElevGreenBtn;
    [SerializeField] private GameObject ElevRedBtn;

    [Header("TV Static Flicker")]
    [SerializeField] private GameObject TVStatic;
    [Tooltip("Minimum time between TVStatic toggles.")]
    [SerializeField] private float tvStaticMinInterval = 0.05f;
    [Tooltip("Maximum time between TVStatic toggles (clamped to 0.5s max).")]
    [SerializeField] private float tvStaticMaxInterval = 0.5f;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;   // single AudioSource
    [SerializeField] private AudioClip startClip;       // plays once
    [SerializeField] private AudioClip loopClip;        // then loops

    private bool hasTriggered = false;
    private Coroutine fadeRoutine;
    private Coroutine audioRoutine;
    private Coroutine tvStaticRoutine;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource != null)
            audioSource.playOnAwake = false;

        // Make sure TVStatic starts in a known state (off is usually nicer)
        if (TVStatic != null)
            TVStatic.SetActive(false);

        // Clamp max interval to never exceed 0.5s
        tvStaticMaxInterval = Mathf.Min(tvStaticMaxInterval, 0.5f);
    }

    // Call this from your button UnityEvent
    public void TriggerSolvedBehaviour()
    {
        if (hasTriggered) return;
        hasTriggered = true;

        // 1) Fade alpha
        if (targetRenderer != null)
        {
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(FadeAlphaRoutine());
        }

        // 2) Play timeline
        if (timelineDirector != null)
            timelineDirector.Play();

        // 3) Enable/Disable buttons
        if (ElevGreenBtn != null) ElevGreenBtn.SetActive(true);
        if (ElevRedBtn != null) ElevRedBtn.SetActive(false);

        // 4) Audio sequence
        if (audioRoutine != null) StopCoroutine(audioRoutine);
        audioRoutine = StartCoroutine(PlaySolvedAudioSequence());

        // 5) Start TV static flicker (on/off at random intervals <= 0.5s)
        if (tvStaticRoutine != null) StopCoroutine(tvStaticRoutine);
        if (TVStatic != null)
            tvStaticRoutine = StartCoroutine(TVStaticFlickerRoutine());
    }

    private IEnumerator FadeAlphaRoutine()
    {
        var mats = targetRenderer.materials;
        if (mats == null || mats.Length == 0) yield break;
        if (materialIndex < 0 || materialIndex >= mats.Length) yield break;

        Material mat = mats[materialIndex];
        if (mat == null) yield break;

        Color c = mat.color;
        c.a = startAlpha;
        mat.color = c;

        float t = 0f;
        while (t < durationSeconds)
        {
            t += Time.deltaTime;
            float u = durationSeconds <= 0f ? 1f : Mathf.Clamp01(t / durationSeconds);

            Color cc = mat.color;
            cc.a = Mathf.Lerp(startAlpha, endAlpha, u);
            mat.color = cc;

            yield return null;
        }

        Color final = mat.color;
        final.a = endAlpha;
        mat.color = final;
    }

    private IEnumerator PlaySolvedAudioSequence()
    {
        if (audioSource == null) yield break;

        // First clip: play once, no loop
        if (startClip != null)
        {
            audioSource.loop = false;
            audioSource.clip = startClip;
            audioSource.Play();
            yield return new WaitForSeconds(startClip.length);
        }

        // Second clip: play in loop
        if (loopClip != null)
        {
            audioSource.loop = true;
            audioSource.clip = loopClip;
            audioSource.Play();
        }
    }

    private IEnumerator TVStaticFlickerRoutine()
    {
        if (TVStatic == null) yield break;

        // Ensure sane values
        float minInterval = Mathf.Max(0.0f, tvStaticMinInterval);
        float maxInterval = Mathf.Clamp(tvStaticMaxInterval, minInterval, 0.5f);

        // Flicker forever after puzzle solved
        while (true)
        {
            // Toggle active state
            TVStatic.SetActive(!TVStatic.activeSelf);

            // Wait a random time, never more than 0.5s
            float wait = Random.Range(minInterval, maxInterval);
            yield return new WaitForSeconds(wait);
        }
    }
}
