using UnityEngine;
using System.Collections.Generic;

public class Collectible : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Make sure the collider/collectible pickup is only triggered by the players body, and not an attack or similar
        if(!other.CompareTag("Player"))
        {
            return;
        }

        if (gameObject.CompareTag("RangedAttackUpgrade"))
        {
            var playerController = other.GetComponent<PlayerController>();
            playerController?.EnablePlayerRangedAttack();
            Debug.Log("Works");
        }
        Destroy(gameObject);
    }
}