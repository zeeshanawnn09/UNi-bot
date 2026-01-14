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

    private bool hasTriggered = false;
    private Coroutine fadeRoutine;

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
    }

    private IEnumerator FadeAlphaRoutine()
    {
        // Use .materials so we can safely edit one slot at runtime
        var mats = targetRenderer.materials;
        if (mats == null || mats.Length == 0) yield break;

        if (materialIndex < 0 || materialIndex >= mats.Length) yield break;

        Material mat = mats[materialIndex];
        if (mat == null) yield break;

        // Assumes the material uses _Color (common in Standard/URP Lit etc.)
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
}
