using UnityEngine;

public class PromptUIController : MonoBehaviour
{
    [Header("Prompt UI Object")]
    public GameObject promptUI;

    // how many systems want this visible
    private int activeRequests = 0;

    private void Awake()
    {
        if (promptUI != null)
            promptUI.SetActive(false);
    }

    public void RequestShow()
    {
        activeRequests++;
        UpdateVisibility();
    }

    public void RequestHide()
    {
        activeRequests = Mathf.Max(0, activeRequests - 1);
        UpdateVisibility();
    }

    private void UpdateVisibility()
    {
        if (promptUI != null)
            promptUI.SetActive(activeRequests > 0);
    }

    // Convenience hooks for UnityEvents
    public void OnEnterRange() => RequestShow();
    public void OnExitRange() => RequestHide();
}
