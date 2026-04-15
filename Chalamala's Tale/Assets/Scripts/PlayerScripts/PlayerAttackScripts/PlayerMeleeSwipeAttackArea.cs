using UnityEngine;

public class PlayerMeleeSwipeAttackArea : MonoBehaviour
{
    // Damage dealt by the attack
    private int damage = 3;

    private void OnTriggerEnter2D(Collider2D collider)
    {
        // Check first whether the object hit has the tag enemy, as to avoid damaging a door
        // (This tag has to be assigned manually to the enemy game objects)
        if (collider.CompareTag("Enemy"))
        {
            collider.gameObject.GetComponentInParent<EnemyChasing>().TakeDamage(damage);
            //Debug.Log("Enemy hit!");
        }
        /**
        // If an enemy is in the trigger collider area, check if it has a health component
        enemyHealth = collider.GetComponent<currentHealth>();

        if(enemyHealth > 0)
        {
            // Make the enemy take damage
            enemyHealth.TakeDamage(damage);
        }
        **/
    }
}
