using UnityEngine;
using UnityEngine.Events;

public class Button3D : MonoBehaviour
{
    [Header("Assign Buttons Here")]
    public GameObject[] buttonObjects;

    [Header("Button Detection Settings")]
    public float buttonRadius = 2f;
    public LayerMask playerLayer;
    public KeyCode interactKey = KeyCode.E;

    [System.Serializable]
    public class ButtonPressedEvent : UnityEvent<int> { }

    [Header("Events")]
    public ButtonPressedEvent onButtonPressed;

    [Header("Range Events (optional)")]
    public UnityEvent onEnterAnyButtonRange;
    public UnityEvent onExitAnyButtonRange;

    public bool AnyButtonInRange { get; private set; }

    private void Update()
    {
        CastRadialRaycast();
    }

    private void CastRadialRaycast()
    {
        if (buttonObjects == null || buttonObjects.Length == 0)
            return;

        bool anyButtonInRangeNow = false;

        for (int i = 0; i < buttonObjects.Length; i++)
        {
            GameObject button = buttonObjects[i];
            if (button == null)
                continue;

            
            if (!button.activeInHierarchy)
                continue;

            bool inButtonRange = Physics.CheckSphere(
                button.transform.position,
                buttonRadius,
                playerLayer
            );

            if (inButtonRange)
            {
                anyButtonInRangeNow = true;

                if (Input.GetKeyDown(interactKey))
                {
                    Debug.Log($"[Button3D] Button pressed index={i}");
                    onButtonPressed?.Invoke(i);
                }
            }
        }

        if (anyButtonInRangeNow != AnyButtonInRange)
        {
            AnyButtonInRange = anyButtonInRangeNow;

            if (AnyButtonInRange)
                onEnterAnyButtonRange?.Invoke();
            else
                onExitAnyButtonRange?.Invoke();
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (buttonObjects == null) return;

        Gizmos.color = Color.green;
        foreach (var button in buttonObjects)
        {
            if (button == null) continue;

            if (!button.activeInHierarchy) continue;

            Gizmos.DrawWireSphere(button.transform.position, buttonRadius);
        }
    }
}
