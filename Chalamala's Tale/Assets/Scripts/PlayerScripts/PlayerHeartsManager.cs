using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

public class PlayerHeartsManager : MonoBehaviour
{
    public GameObject playerHeartPrefab;

    // Reference field for keeping track of players current and max health
    public PlayerHealth playerHealth;

    // List to keep track of each of the heart's status
    List<PlayerHeart> hearts = new List<PlayerHeart>();


    // Subscribe to the "OnPlayerDamaged" event, redraw all hearts if player is damaged
    private void OnEnable()
    {
        PlayerHealth.OnPlayerDamaged += DrawHearts;
    }

    private void OnDisable()
    {
        PlayerHealth.OnPlayerDamaged -= DrawHearts;
    }

    private void Start()
    {
        playerHealth = PlayerHealth.Instance;
        DrawHearts();
    }

    public void DrawHearts()
    {
        ClearHearts();

        // Check if max health is odd or even, add remainder in case of odd
        float maxHealthRemainder = playerHealth.maxHealth % 2;
        int heartsToMake = (int)((playerHealth.maxHealth / 2) + maxHealthRemainder);

        // Create the hearts
        for(int i = 0; i < heartsToMake; i++)
        {
            CreateEmptyHeart();
        }

        for(int i = 0; i < hearts.Count; i++)
        {
            int heartsRemainder = (int)Mathf.Clamp(playerHealth.currentHealth - (i*2), 0, 2);
            hearts[i].SetHeartImage((HeartStatus)heartsRemainder);
        }
    }

    // Method for creating new hearts, which are initially empty
    public void CreateEmptyHeart()
    {
        GameObject newHeart = Instantiate(playerHeartPrefab);
        // Set this game object to its parent (PlayerHeartsManager). dont quite understand why? (is from tutorial)
        newHeart.transform.SetParent(transform);

        // Get the actualy player heart component of the game object, and set its image to empty initially
        PlayerHeart heartComponent = newHeart.GetComponent<PlayerHeart>();
        heartComponent.SetHeartImage(HeartStatus.Empty);
        hearts.Add(heartComponent);
    }

    public void ClearHearts()
    {
        // Clear all existing hearts
        foreach(Transform t in transform)
        {
            Destroy(t.gameObject);
        }
        // Create a new list of hearts
        hearts = new List<PlayerHeart>();
    }
}