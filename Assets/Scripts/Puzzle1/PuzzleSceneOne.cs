using UnityEngine;

public class PuzzleSceneOne : MonoBehaviour
{
    [Header("Puzzle Lights (index must match buttons)")]
    [SerializeField] private Light[] puzzleLights;

    [Header("Button Manager (same order as lights)")]
    public Button3D buttonManager;

    [Header("Correct Button Order")]
    [Tooltip("Indices into buttonObjects / puzzleLights, e.g. 0,2,1 for a 3-button puzzle.")]
    [SerializeField] private int[] correctOrder;

    [Header("Light Colors")]
    public Color idleColor = Color.red;     // default red
    public Color activeColor = Color.green; // turns green when correct

    [Header("Solved Indicator")]
    public Light solvedLight;               // turns on when puzzle is solved

    [HideInInspector]
    public bool puzzleSolved = false;

    private int currentStep = 0;
    private bool[] buttonUsed;

    private void Awake()
    {
        if (puzzleLights != null)
            buttonUsed = new bool[puzzleLights.Length];

        InitLights();

        // Make sure solved light starts off
        if (solvedLight != null)
            solvedLight.enabled = false;
    }

    private void OnEnable()
    {
        if (buttonManager != null)
        {
            buttonManager.onButtonPressed.AddListener(OnButtonPressed);
        }
    }

    private void OnDisable()
    {
        if (buttonManager != null)
        {
            buttonManager.onButtonPressed.RemoveListener(OnButtonPressed);
        }
    }

    private void InitLights()
    {
        if (puzzleLights == null) return;

        for (int i = 0; i < puzzleLights.Length; i++)
        {
            if (puzzleLights[i] == null) continue;

            puzzleLights[i].enabled = true;
            puzzleLights[i].color = idleColor;
        }
    }

    // Called from Button3D via UnityEvent
    public void OnButtonPressed(int pressedIndex)
    {
        Debug.Log($"[PuzzleSceneOne] Button pressed index={pressedIndex}");

        if (puzzleSolved || correctOrder == null || correctOrder.Length == 0)
            return;

        if (puzzleLights == null || pressedIndex < 0 || pressedIndex >= puzzleLights.Length)
            return;

        // Already used in sequence, ignore
        if (buttonUsed != null && buttonUsed[pressedIndex])
            return;

        if (currentStep < 0 || currentStep >= correctOrder.Length)
            return;

        int expectedIndex = correctOrder[currentStep];

        if (pressedIndex == expectedIndex)
        {
            // Correct button
            SetLightColor(pressedIndex, activeColor);

            if (buttonUsed != null && pressedIndex >= 0 && pressedIndex < buttonUsed.Length)
                buttonUsed[pressedIndex] = true;

            currentStep++;

            if (currentStep >= correctOrder.Length)
            {
                puzzleSolved = true;
                Debug.Log("[PuzzleSceneOne] Puzzle solved!");

                // Enable solved light
                if (solvedLight != null)
                    solvedLight.enabled = true;
            }
        }
        else
        {
            // Wrong button
            Debug.LogWarning("[PuzzleSceneOne] Wrong button pressed. Resetting puzzle.");
            ResetPuzzle();
        }
    }

    private void SetLightColor(int index, Color color)
    {
        if (puzzleLights == null) return;
        if (index < 0 || index >= puzzleLights.Length) return;

        Light light = puzzleLights[index];
        if (light == null) return;

        light.enabled = true;
        light.color = color;
    }

    private void ResetPuzzle()
    {
        currentStep = 0;

        if (puzzleLights == null) return;

        for (int i = 0; i < puzzleLights.Length; i++)
        {
            if (puzzleLights[i] == null) continue;

            puzzleLights[i].enabled = true;
            puzzleLights[i].color = idleColor;

            if (buttonUsed != null && i < buttonUsed.Length)
                buttonUsed[i] = false;
        }

        // Also turn off solved light on reset, just in case you reuse this
        if (solvedLight != null)
            solvedLight.enabled = false;
    }
}
