using UnityEngine;

public class QuitGameButton : MonoBehaviour
{
    // Call this from a UI Button OnClick
    public void QuitGame()
    {
#if UNITY_EDITOR
        // Stop play mode in the Editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // Close the built game
        Application.Quit();
#endif
    }
}
