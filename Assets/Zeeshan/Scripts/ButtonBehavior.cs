using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ButtonBehavior : MonoBehaviour
{
    [SerializeField] private GameObject promptUI; // Assign a UI text/panel with "Press E to rotate"
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private PipeRotationBehavior targetPipe; // Pipe to rotate when pressing the key
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    private bool playerInside;

    private void OnValidate()
    {
        // Ensure this collider is set as a trigger so enter/exit events fire
        Collider col = GetComponent<Collider>();
        if (col != null && !col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    private void Awake()
    {
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
        else
        {
            Debug.LogWarning("ButtonBehavior: promptUI is not assigned.", this);
        }
    }

    private void Update()
    {
        if (!playerInside) return;
        if (targetPipe == null) return;

        if (Input.GetKeyDown(interactKey))
        {
            targetPipe.RotateOnce();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (promptUI != null)
        {
            promptUI.SetActive(true);
        }
        playerInside = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
        playerInside = false;
    }
}
