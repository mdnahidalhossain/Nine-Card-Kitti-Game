using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    // Reference to the pause menu UI (optional, if you want to show a menu when paused)
    [SerializeField] private GameObject pauseGameIcon;
    [SerializeField] private GameObject resumeGameIcon;
    [SerializeField] private GameObject pauseGamePanel;

    [SerializeField] private DeckManager deckManager;

    // Boolean to track if the game is paused
    private bool isPaused = false;


    public void PlayAgain()
    {
        //restarts the game
        SceneManager.LoadScene(0);
        Draggable.EnableDragging();
    }


    // Method to pause and unpause the game
    public void TogglePause()
    {
        if (isPaused)
        {
            deckManager.ButtonClickSound();
            UnpauseGame();
            Debug.Log("Game is unpaused.");
            pauseGamePanel.SetActive(false);
        }
        else
        {
            deckManager.ButtonClickSound();
            PauseGame();
            Debug.Log("Game is paused.");
            pauseGamePanel.SetActive(true);
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
