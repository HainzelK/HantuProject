using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
   public void StartGame()
    {
        SceneManager.LoadScene("tesHitFeedback"); //load scene
    }

    // public void QuitGame()
    // public void QuitGame()
    // {
    //     Application.Quit();
    //     Debug.Log("Quit Game"); 
    // }
}
