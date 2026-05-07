using UnityEngine;

public class PlayerMeleeSwipeAttackArea : MonoBehaviour
{
    // Damage dealt by the attack
    private int damage = 3;

    private void OnEnable()
    {
        AimAtNearestEnemy();
        Debug.Log($"Parent position: {transform.parent.position}, AttackArea position: {transform.position}");
    }

    private void AimAtNearestEnemy()
    {
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        if (enemies.Length == 0) {
            transform.localPosition = new Vector3(0f, -5f, 0f);
            return;
        }


        GameObject nearest = null;
        float shortestDistance = Mathf.Infinity;

        foreach (GameObject enemy in enemies)
        {
            float distance = Vector3.Distance(transform.parent.position, enemy.transform.position);
            if (distance < shortestDistance)
            {
                shortestDistance = distance;
                nearest = enemy;
            }
        }

        if (nearest != null)
        {
            Vector2 direction = (nearest.transform.position - transform.parent.position).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            // Apply rotation first
            transform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Then move it down in LOCAL space (relative to player)
            transform.localPosition = new Vector3(0f, -5f, 0f);
        }
    }

    private void OnTriggerEnter2D(Collider2D collider)
    {
        // Check first whether the object hit has the tag enemy, as to avoid damaging a door
        // (This tag has to be assigned manually to the enemy game objects)
        if (collider.CompareTag("Enemy"))
        {
            collider.gameObject.GetComponentInParent<IDamageable>()?.TakeDamage(damage);
            Debug.Log("Enemy hit!");
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
