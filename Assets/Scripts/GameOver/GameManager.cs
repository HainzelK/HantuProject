using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;


public class GameManager : MonoBehaviour
{
    public GameObject gameOverUI;
    public GameObject winScreenUI;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GameOver()
    {
        Debug.Log("Game Over Triggered");
        gameOverUI.SetActive(true);
        Time.timeScale = 0f; // Pause the game
        Debug.Log("GameOver: Game Paused");
        // Additional game over logic can be added here
    }

    public void WinScreen()
    {
        Debug.Log("Win Screen Triggered");
        winScreenUI.SetActive(true);
        Time.timeScale = 0f; // Pause the game
        Debug.Log("Win: Game Paused");
    }

    public void RestartGame()
    {
        Debug.Log("Restarting Game...");
        Time.timeScale = 1f; // Resume the game
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnHome()
    {
        Debug.Log("Returning to Main Menu...");
        Time.timeScale = 1f; // Resume the game
        SceneManager.LoadScene("MainMenu");
    }
}
