using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    public static event Action OnPlayerDamaged;
    public static event Action OnPlayerDeath;

    public float currentHealth, maxHealth;

    public static PlayerHealth Instance;
    public int deathCounter;


    private void Awake(){
        if(Instance != null && Instance != this)
        {
            Debug.Log("Duplicate player destroyed");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        currentHealth = maxHealth;
        Debug.Log($"Player health initialized: {currentHealth}");
        deathCounter = 0;
        Debug.Log($"number of deaths: {deathCounter}");
    }


    public void TakeDamage(float damageAmount){
        currentHealth -= damageAmount;
        Debug.Log($"Health after damage: {currentHealth}");
        OnPlayerDamaged?.Invoke();

        if(currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log("You died!");
            OnPlayerDeath?.Invoke();
            Resurrect();
        }
    }


    // handles the respwan after dying (max health brought back only after the player is in the correct room to avoid asynch errors) 
    public void Resurrect()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (currentScene.name == "easy_fight" || currentScene.name == "dragon_killing_you")
        {
            // to not mess up with room positions, we assign it automatically
            BasicGridManager.Instance.currentRow = 2;
            BasicGridManager.Instance.currentCol = 0;
            SceneManager.LoadScene("Tutorial_first_scene");
        
        } else
        // Else we are past the tutorial (and we respawn if first cell of big grid)
        {
            // to not mess up with room positions, we assign it automatically
            GridManager.Instance.currentRow = 3;
            GridManager.Instance.currentCol = 3;
            SceneManager.LoadScene("Room");
            
            
        }

        deathCounter += 1;

    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Restore health AFTER scene is fully loaded
        currentHealth = maxHealth;

        // Unsubscribe so it doesn't fire again accidentally
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}