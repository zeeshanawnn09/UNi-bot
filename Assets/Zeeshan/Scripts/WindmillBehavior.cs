using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class WindmillBehavior : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    [SerializeField] private GameObject promptUI; // UI element to show "Press E to start windmill"
    [SerializeField] private Transform windmillTransform; // Reference to windmill (leave empty if this script is ON the windmill)
    
    private bool hasStartedRotating = false;
    private bool playerInRange = false;

    void Start()
    {
        // Hide the prompt at start
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Check if E key is pressed and player is in range
        if (Input.GetKeyDown(KeyCode.E) && !hasStartedRotating && playerInRange)
        {
            hasStartedRotating = true;
            
            // Hide the prompt
            if (promptUI != null)
            {
                promptUI.SetActive(false);
            }
            
            StartCoroutine(RotateAndSwitchScene());
        }

        // Only rotate if E has been pressed
        if (hasStartedRotating)
        {
            Transform targetTransform = windmillTransform != null ? windmillTransform : transform;
            targetTransform.Rotate(0f, 10f * Time.deltaTime, 0f, Space.Self);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player entered the trigger
        if (other.CompareTag("Player") && !hasStartedRotating)
        {
            playerInRange = true;
            
            // Show the prompt
            if (promptUI != null)
            {
                promptUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Check if the player left the trigger
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            
            // Hide the prompt
            if (promptUI != null)
            {
                promptUI.SetActive(false);
            }
        }
    }

    private IEnumerator RotateAndSwitchScene()
    {
        // Wait for 5 seconds while windmill rotates
        yield return new WaitForSeconds(5f);

        // Switch to the next scene if specified
        if (!string.IsNullOrEmpty(nextSceneName))
        {
            SceneManager.LoadScene(nextSceneName);
        }
    }
}
