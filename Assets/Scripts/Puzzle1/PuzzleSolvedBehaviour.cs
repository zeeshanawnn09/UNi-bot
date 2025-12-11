using System.Collections;
using UnityEngine;

public class PuzzleSolvedBehaviour : MonoBehaviour
{
    [Header("Puzzle Manager")]
    public PuzzleSceneOne puzzleSceneManager;

    [Header("Open Door Button")]
    public Transform buttonTransform;
    public float interactRadius = 1.5f;
    public LayerMask playerLayer;
    public KeyCode interactKey = KeyCode.E;

    [Header("Interaction UI")]
    public bool showPromptUI = true;
    public PromptUIController promptController;   // shared controller

    [Header("Doors To Open")]
    public GameObject[] doorsToOpen;
    public Vector3[] doorsRotation;

    [Header("Door Open Animation")]
    public float openDuration = 1.5f;

    private bool hasOpened = false;

    private Quaternion[] initialRotations;
    private Quaternion[] targetRotations;

    private bool wasPlayerInRange = false;

    private void Start()
    {
        if (doorsToOpen != null && doorsRotation != null)
        {
            int count = Mathf.Min(doorsToOpen.Length, doorsRotation.Length);
            initialRotations = new Quaternion[count];
            targetRotations = new Quaternion[count];

            for (int i = 0; i < count; i++)
            {
                if (doorsToOpen[i] == null) continue;

                initialRotations[i] = doorsToOpen[i].transform.rotation;
                targetRotations[i] = Quaternion.Euler(doorsRotation[i]);
            }
        }
    }

    private void Update()
    {
        if (buttonTransform == null)
            return;

        bool playerInRange = Physics.CheckSphere(
            buttonTransform.position,
            interactRadius,
            playerLayer
        );

        // toggle prompt only when state changes
        if (showPromptUI && promptController != null)
        {
            if (playerInRange && !wasPlayerInRange)
            {
                promptController.RequestShow();
            }
            else if (!playerInRange && wasPlayerInRange)
            {
                promptController.RequestHide();
            }
        }

        wasPlayerInRange = playerInRange;

        if (!playerInRange)
            return;

        if (puzzleSceneManager == null || !puzzleSceneManager.puzzleSolved)
            return;

        if (Input.GetKeyDown(interactKey) && !hasOpened)
        {
            hasOpened = true;
            StartCoroutine(OpenDoorsCoroutine());

            // puzzle no longer needs the prompt
            if (showPromptUI && promptController != null)
                promptController.RequestHide();
        }
    }

    private IEnumerator OpenDoorsCoroutine()
    {
        if (doorsToOpen == null || doorsRotation == null)
            yield break;

        int count = Mathf.Min(doorsToOpen.Length, doorsRotation.Length);
        if (count == 0)
            yield break;

        if (initialRotations == null || targetRotations == null ||
            initialRotations.Length != count || targetRotations.Length != count)
        {
            initialRotations = new Quaternion[count];
            targetRotations = new Quaternion[count];

            for (int i = 0; i < count; i++)
            {
                if (doorsToOpen[i] == null) continue;

                initialRotations[i] = doorsToOpen[i].transform.rotation;
                targetRotations[i] = Quaternion.Euler(doorsRotation[i]);
            }
        }

        float time = 0f;
        float duration = Mathf.Max(0.01f, openDuration);

        while (time < duration)
        {
            float t = time / duration;

            for (int i = 0; i < count; i++)
            {
                if (doorsToOpen[i] == null) continue;

                doorsToOpen[i].transform.rotation =
                    Quaternion.Slerp(initialRotations[i], targetRotations[i], t);
            }

            time += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (doorsToOpen[i] == null) continue;
            doorsToOpen[i].transform.rotation = targetRotations[i];
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (buttonTransform == null) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(buttonTransform.position, interactRadius);
    }
}
