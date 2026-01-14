using UnityEngine;

public class PlantDigController : MonoBehaviour
{
    [SerializeField] private GameObject diggingGameObject;
    [SerializeField] private GameObject plantingGameObject;
    [SerializeField] private RBAnimAndRigController animController;
    [SerializeField] private PromptUIController promptUIController;
    [SerializeField] private CameraController cameraController;

    private void Awake()
    {
        // On awake: digging is enabled, planting is disabled
        if (diggingGameObject != null)
            diggingGameObject.SetActive(true);

        if (plantingGameObject != null)
            plantingGameObject.SetActive(false);
    }

    private void Start()
    {
        // Subscribe to animation events
        if (animController != null)
        {
            animController.OnActionStarting += OnActionStarting;
            animController.OnActionCompleted += OnActionCompleted;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe to prevent memory leaks
        if (animController != null)
        {
            animController.OnActionStarting -= OnActionStarting;
            animController.OnActionCompleted -= OnActionCompleted;
        }
    }

    private void OnActionStarting(string triggerName, float duration)
    {
        // Start cinematic camera when digging or planting starts
        if (triggerName == "Digging" || triggerName == "Planting")
        {
            if (cameraController != null)
                cameraController.StartCinematic(duration);
        }
    }

    private void OnActionCompleted(string triggerName)
    {
        // End cinematic camera
        if (cameraController != null)
            cameraController.EndCinematic();

        // When digging animation completes, swap the game objects
        if (triggerName == "Digging")
        {
            if (diggingGameObject != null)
                diggingGameObject.SetActive(false);

            if (plantingGameObject != null)
                plantingGameObject.SetActive(true);

            // Hide the prompt UI
            if (promptUIController != null)
                promptUIController.RequestHide();
        }
    }
}
