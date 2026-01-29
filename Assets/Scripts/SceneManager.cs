using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Scene Build Indices (File > Build Settings order)")]
    [SerializeField] private int MainMenuSceneIndex = 0;
    [SerializeField] private int OpenWorldSceneIndex = 1;
    [SerializeField] private int LobbySceneIndex = 2;
    [SerializeField] private int Puzzle1SceneIndex = 3;
    [SerializeField] private int RoofTopSceneIndex = 4;
    [SerializeField] private int CutsceneSceneIndex = 5;

    [Header("Debug / Info")]
    [SerializeField] private int previousSceneBuildIndex = -1; // view in inspector
    [SerializeField] private int currentSceneBuildIndex = -1;  // view in inspector

    public static int PreviousSceneBuildIndex => Instance ? Instance.previousSceneBuildIndex : -1;
    public static int CurrentSceneBuildIndex => Instance ? Instance.currentSceneBuildIndex : -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log($"[SceneLoader] Duplicate in scene {SceneManager.GetActiveScene().buildIndex}, destroying this one.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;
        Debug.Log($"[SceneLoader] Awake in scene {currentSceneBuildIndex}", this);

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Debug.Log("[SceneLoader] OnDestroy, unsubscribing.", this);
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
        }
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        // IMPORTANT: do NOT overwrite previousSceneBuildIndex here.
        // We already set it manually in LoadByIndex before calling LoadScene.
        currentSceneBuildIndex = newScene.buildIndex;

        Debug.Log($"[SceneLoader] Scene changed prev={previousSceneBuildIndex} -> current={currentSceneBuildIndex}", this);
    }

    public static void LoadMainMenu() => LoadByIndex(Instance.MainMenuSceneIndex);
    public static void LoadOpenWorld() => LoadByIndex(Instance.OpenWorldSceneIndex);
    public static void LoadLobby() => LoadByIndex(Instance.LobbySceneIndex);
    public static void LoadPuzzle1() => LoadByIndex(Instance.Puzzle1SceneIndex);
    public static void LoadRoofTop() => LoadByIndex(Instance.RoofTopSceneIndex);
    public static void LoadCutscene() => LoadByIndex(Instance.CutsceneSceneIndex);

    // This is what your CutsceneSceneController calls:
    public static void LoadByIndexPublic(int buildIndex) => LoadByIndex(buildIndex);

    private static void LoadByIndex(int buildIndex)
    {
        if (Instance == null)
        {
            Debug.LogError("[SceneLoader] SceneLoader not found. Add it once in the first scene.");
            return;
        }

        int sceneCount = SceneManager.sceneCountInBuildSettings;
        if (buildIndex < 0 || buildIndex >= sceneCount)
        {
            Debug.LogError($"[SceneLoader] Invalid buildIndex {buildIndex}. Valid range: 0 to {sceneCount - 1}.", Instance);
            return;
        }

        int current = SceneManager.GetActiveScene().buildIndex;

        // Record previous here and DO NOT touch it in OnActiveSceneChanged.
        Instance.previousSceneBuildIndex = current;

        Debug.Log($"[SceneLoader] LoadByIndex {current} -> {buildIndex}", Instance);

        SceneManager.LoadScene(buildIndex);
    }
}
