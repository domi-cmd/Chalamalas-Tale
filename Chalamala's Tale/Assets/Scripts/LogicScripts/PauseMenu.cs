using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
public class PauseMenu : MonoBehaviour 
{
    public GameObject pauseMenu;
    public GameObject settingMenu;
    public static bool IsPaused = false;
    [SerializeField] PlayerController player;
        
    void Start()
    {
        pauseMenu.SetActive(false);
        settingMenu.SetActive(false);

    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (IsPaused){
                ResumeGame();
            } else
            {
                PauseGame();
            }
        }
    }
    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        IsPaused = true;
        player.SetPlayerMovement(false);
        
    }
    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        settingMenu.SetActive(false);
        Time.timeScale = 1f;
        IsPaused = false;
        player.SetPlayerMovement(true);

    }
}
