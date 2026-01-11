using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Scene Names")]
    [SerializeField] private string OpenWorldSceneName;
    [SerializeField] private string LobbySceneName;
    [SerializeField] private string Puzzle1SceneName;
    [SerializeField] private string Puzzle2SceneName;
    [SerializeField] private string RoofTopSceneName;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void LoadOpenWorld() => LoadByName(Instance.OpenWorldSceneName);
    public static void LoadLobby() => LoadByName(Instance.LobbySceneName);
    public static void LoadPuzzle1() => LoadByName(Instance.Puzzle1SceneName);
    public static void LoadPuzzle2() => LoadByName(Instance.Puzzle2SceneName);
    public static void LoadRoofTop() => LoadByName(Instance.RoofTopSceneName);

    private static void LoadByName(string sceneName)
    {
        if (Instance == null)
        {
            Debug.LogError("SceneLoader not found. Add it once in the first scene.");
            return;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("Scene name is empty on SceneLoader.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
