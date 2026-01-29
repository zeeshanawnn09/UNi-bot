using System.Collections;
using UnityEngine;

public class WindMill : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;
    [SerializeField] private string spinBoolName = "SPIN";

    [Header("Timing")]
    [SerializeField] private float delaySeconds = 5f;

    [Header("Optional")]
    [SerializeField] private bool autoStartOnEnable = false;

    private bool _started;

    private void OnEnable()
    {
        if (autoStartOnEnable)
            Trigger();
    }

    // Call this from a UI Button, trigger, or another script.
    public void Trigger()
    {
        if (_started) return;
        _started = true;

        if (animator != null)
            animator.SetBool(spinBoolName, true);

        StartCoroutine(LoadCutsceneAfterDelay());
    }

    private IEnumerator LoadCutsceneAfterDelay()
    {
        yield return new WaitForSeconds(delaySeconds);

        // Uses your SceneLoader, which records the previous scene before loading.
        SceneLoader.LoadCutscene();
    }
}
