using UnityEngine;

public class PlayerMeleeSwipeAttackArea : MonoBehaviour
{
    // Damage dealt by the attack
    private int damage = 3;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        collider.gameObject.GetComponentInParent<EnemyChasing>().TakeDamage(damage);

        /**
        // If an enemy is in the trigger collider area, check if it has a health component
        enemyHealth = collider.GetComponent<currentHealth>();

        if(enemyHealth > 0)
        {
            // Make the enemy take damage
            enemyHealth.TakeDamage(damage);
        }
        **/
        
        
        Debug.Log("Enemy hit!");
    }
    
}
