using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class PlayerHealth : MonoBehaviour
{
    public static event Action OnPlayerDamaged;
    public static event Action OnPlayerDeath;

    public float currentHealth, maxHealth;

    public static PlayerHealth Instance;


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
        }
    }
}