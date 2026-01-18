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
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        currentSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;

        SceneManager.activeSceneChanged += OnActiveSceneChanged;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
    }

    private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
    {
        previousSceneBuildIndex = oldScene.buildIndex;
        currentSceneBuildIndex = newScene.buildIndex;
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
            Debug.LogError("SceneLoader not found. Add it once in the first scene.");
            return;
        }

        int sceneCount = SceneManager.sceneCountInBuildSettings;
        if (buildIndex < 0 || buildIndex >= sceneCount)
        {
            Debug.LogError($"Invalid buildIndex {buildIndex}. Valid range: 0 to {sceneCount - 1}. Check Build Settings order.");
            return;
        }

        // record prev immediately (so it’s correct even before activeSceneChanged fires)
        Instance.previousSceneBuildIndex = SceneManager.GetActiveScene().buildIndex;

        SceneManager.LoadScene(buildIndex);
    }
}
