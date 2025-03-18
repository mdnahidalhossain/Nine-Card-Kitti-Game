using UnityEngine;
using UnityEngine.SceneManagement;

public class RandomSceneManager : MonoBehaviour
{
    // List of scene names (excluding the menu scene)
    [SerializeField] private string[] gameScenes;

    public void LoadRandomScene()
    {
        if (gameScenes.Length == 0)
        {
            Debug.LogError("No scenes assigned in the SceneLoader script.");
            return;
        }

        // Pick a random scene from the list
        string randomScene = gameScenes[Random.Range(0, gameScenes.Length)];
        SceneManager.LoadScene(randomScene);
    }
}
