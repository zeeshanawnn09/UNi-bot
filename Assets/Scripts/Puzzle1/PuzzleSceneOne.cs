using UnityEngine;

public class PuzzleSceneOne : MonoBehaviour
{
    [Header("Puzzle Lights (index must match buttons)")]
    [SerializeField] private Light[] puzzleLights;

    [Header("Toggle Objects (same size + same index as puzzleLights)")]
    [SerializeField] private GameObject[] TGreenLight;
    [SerializeField] private GameObject[] TRedLight;

    [Header("Button Manager (same order as lights)")]
    public Button3D buttonManager;

    [Header("Correct Button Order")]
    [SerializeField] private int[] correctOrder;

    [Header("Light Colors")]
    public Color idleColor = Color.red;
    public Color activeColor = Color.green;

    [Header("Solved Indicator")]
    public Light solvedLight;

    [Header("Solved Object")]
    [SerializeField] private GameObject PuzzleSolvedObject;

    [HideInInspector] public bool puzzleSolved = false;

    private int currentStep = 0;
    private bool[] buttonUsed;

    private void Awake()
    {
        if (puzzleLights != null)
            buttonUsed = new bool[puzzleLights.Length];

        InitLights();

        if (solvedLight != null)
            solvedLight.enabled = false;

        if (PuzzleSolvedObject != null)
            PuzzleSolvedObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (buttonManager != null)
            buttonManager.onButtonPressed.AddListener(OnButtonPressed);
    }

    private void OnDisable()
    {
        if (buttonManager != null)
            buttonManager.onButtonPressed.RemoveListener(OnButtonPressed);
    }

    private void InitLights()
    {
        if (puzzleLights == null) return;

        for (int i = 0; i < puzzleLights.Length; i++)
        {
            if (puzzleLights[i] == null) continue;

            puzzleLights[i].enabled = true;
            puzzleLights[i].color = idleColor;

            ApplyToggleObjects(i, idleColor);
        }
    }

    public void OnButtonPressed(int pressedIndex)
    {
        if (puzzleSolved || correctOrder == null || correctOrder.Length == 0)
            return;

        if (puzzleLights == null || pressedIndex < 0 || pressedIndex >= puzzleLights.Length)
            return;

        if (buttonUsed != null && buttonUsed[pressedIndex])
            return;

        if (currentStep < 0 || currentStep >= correctOrder.Length)
            return;

        int expectedIndex = correctOrder[currentStep];

        if (pressedIndex == expectedIndex)
        {
            SetLightColor(pressedIndex, activeColor);

            if (buttonUsed != null && pressedIndex >= 0 && pressedIndex < buttonUsed.Length)
                buttonUsed[pressedIndex] = true;

            currentStep++;

            if (currentStep >= correctOrder.Length)
            {
                puzzleSolved = true;

                if (solvedLight != null)
                    solvedLight.enabled = true;

                if (PuzzleSolvedObject != null)
                    PuzzleSolvedObject.SetActive(true);
            }
        }
        else
        {
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

        ApplyToggleObjects(index, color);
    }

    private void ApplyToggleObjects(int index, Color puzzleColor)
    {
        // exact compare as requested
        bool isRed = puzzleColor == idleColor;
        bool isGreen = puzzleColor == activeColor;

        if (!isRed && !isGreen) return;

        if (TRedLight != null && index >= 0 && index < TRedLight.Length && TRedLight[index] != null)
            TRedLight[index].SetActive(isRed);

        if (TGreenLight != null && index >= 0 && index < TGreenLight.Length && TGreenLight[index] != null)
            TGreenLight[index].SetActive(isGreen);
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

            ApplyToggleObjects(i, idleColor);

            if (buttonUsed != null && i < buttonUsed.Length)
                buttonUsed[i] = false;
        }

        if (solvedLight != null)
            solvedLight.enabled = false;

        if (PuzzleSolvedObject != null)
            PuzzleSolvedObject.SetActive(false);

        puzzleSolved = false;
    }
}
