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
    public GameObject deathMenu;
    public GameObject player;
    public static bool isDead =false;
    [SerializeField] private GameObject gravestonePrefab;   


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
            isDead = true;
            Debug.Log("You died!");
            OnPlayerDeath?.Invoke();
            /*
            if (gravestonePrefab != null)
            {
                Instantiate(gravestonePrefab, transform.position, Quaternion.identity);
            }
            */

            //gameObject.SetActive(false);
            deathMenu.SetActive(true);
        }
    }

    public bool Heal(float healAmount)
    {
        if (healAmount <= 0f || currentHealth >= maxHealth)
        {
            return false;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + healAmount);
        OnPlayerDamaged?.Invoke();
        return true;
    }

    public void IncreaseMaxHealth(float amount, bool healAddedAmount = true)
    {
        if (amount <= 0f)
        {
            return;
        }

        maxHealth += amount;

        if (healAddedAmount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        }
        else
        {
            currentHealth = Mathf.Min(currentHealth, maxHealth);
        }

        OnPlayerDamaged?.Invoke();
    }


    // handles the respwan after dying (max health brought back only after the player is in the correct room to avoid asynch errors) 
    public void Resurrect()
    {
        Debug.Log("respawning");
        isDead=false;
        player.SetActive(true);
        Scene currentScene = SceneManager.GetActiveScene();
        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
        // if player dies during the first fight, the tutorial restarts
        if (currentScene.name == "easy_fight")
        {
            // to not mess up with room positions, we assign it automatically
            BasicGridManager.Instance.currentRow = 2;
            BasicGridManager.Instance.currentCol = 0;
            SceneManager.LoadScene("Tutorial_first_scene");
        
        } else
        // Else (dragon_killing_you or already in real game grid) we go past the tutorial (and we respawn if first cell of big grid)
        {
            // to not mess up with room positions, we assign it automatically
            GridManager.Instance.currentRow = 3;
            GridManager.Instance.currentCol = 3;
            
            SceneManager.sceneLoaded += RedrawMinimapAfterRespawn;
            //GridManager.Instance.GenerateGrid();

            SceneManager.LoadScene("Room");
            
            
        }

    deathCounter += 1;

    }

    private void RedrawMinimapAfterRespawn(Scene scene, LoadSceneMode mode)
    {
        Minimap minimap = FindFirstObjectByType<Minimap>();

        Debug.Log("Minimap found? " + (minimap != null));

        if(minimap != null)
        {
            minimap.Draw();
        }

        SceneManager.sceneLoaded -= RedrawMinimapAfterRespawn;
    }



    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {

        // Restore health AFTER scene is fully loaded
        currentHealth = maxHealth;
        deathMenu.SetActive(false);

        if (scene.name == "Room")
        {
            transform.position = Vector3.zero; // after being killed by dragon you get respawned at the center to avoid being pushed out of the camera
        }

        // refresh heats UI (ensuring that they are full after respwn)
        OnPlayerDamaged?.Invoke();

        // Unsubscribe so it doesn't fire again accidentally
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}