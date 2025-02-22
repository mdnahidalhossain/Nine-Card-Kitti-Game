using UnityEngine;

public class PauseManager : MonoBehaviour
{
    // Reference to the pause menu UI (optional, if you want to show a menu when paused)
    [SerializeField] private GameObject pauseGameIcon;
    [SerializeField] private GameObject resumeGameIcon;

    // Boolean to track if the game is paused
    private bool isPaused = false;


    // Method to pause and unpause the game
    public void TogglePause()
    {
        if (isPaused)
        {
            UnpauseGame();
        }
        else
        {
            PauseGame();
        }
    }

    // Method to pause the game
    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Pauses the game by setting the time scale to 0

        // Optionally, show a pause menu
        if (pauseGameIcon != null)
        {
            pauseGameIcon.SetActive(false);
            resumeGameIcon.SetActive(true);
        }
    }

    // Method to unpause the game
    private void UnpauseGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resumes the game by setting the time scale to 1

        // Hide the pause menu if you have one
        if (pauseGameIcon != null)
        {
            pauseGameIcon.SetActive(true);
            resumeGameIcon.SetActive(false);
        }
    }
}
