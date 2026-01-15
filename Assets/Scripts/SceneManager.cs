using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    [Header("Scene to Load")]
    [Tooltip("Enter the exact scene name (must be added in Build Settings)")]
    [SerializeField] private string sceneToLoad = "SceneMapTest";

    [Header("OR use Build Index")]
    [Tooltip("Set to -1 to use scene name above, otherwise uses this build index")]
    [SerializeField] private int sceneBuildIndex = 1;

    /// <summary>
    /// Call this method from your Start Game button's OnClick() event
    /// </summary>
    public void LoadScene()
    {
        if (sceneBuildIndex >= 0)
        {
            // Load by build index
            if (sceneBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                SceneManager.LoadScene(sceneBuildIndex);
            }
            else
            {
                Debug.LogError($"Invalid build index {sceneBuildIndex}. Check File > Build Settings.");
            }
        }
        else if (!string.IsNullOrEmpty(sceneToLoad))
        {
            // Load by scene name
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            Debug.LogError("No scene specified! Set either Scene Name or Build Index in the Inspector.");
        }
    }

    /// <summary>
    /// Load a specific scene by name (can be called from other scripts or buttons)
    /// </summary>
    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogError("Scene name is empty!");
        }
    }

    /// <summary>
    /// Load a specific scene by build index (can be called from other scripts or buttons)
    /// </summary>
    public void LoadSceneByIndex(int buildIndex)
    {
        if (buildIndex >= 0 && buildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(buildIndex);
        }
        else
        {
            Debug.LogError($"Invalid build index {buildIndex}. Valid range: 0 to {SceneManager.sceneCountInBuildSettings - 1}");
        }
    }

    /// <summary>
    /// Quit the application (for Quit button)
    /// </summary>
    public void QuitGame()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
