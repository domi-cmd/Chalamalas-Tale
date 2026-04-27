using UnityEngine;

public class PlayerRangedAttackArea : MonoBehaviour
{
    private PlayerRangedAttack settings;
    private int damage = 3;
    private float projectileSpeed = 8f;

    public void Setup(PlayerRangedAttack parentSettings)
    {
        settings = parentSettings;
    }

    private void OnEnable()
    {
        if (settings == null) return;

        Collider2D[] hitEnemies = Physics2D.OverlapCircleAll(transform.position, settings.attackRangeRadius);
        
        foreach (var enemyCollider in hitEnemies)
        {
            if (enemyCollider.CompareTag("Enemy"))
            {
                // We ONLY shoot. We don't call TakeDamage here anymore.
                Shoot(enemyCollider.transform.position);

                // This STOPs the loop so we only fire one projectile per 'K' press
                break; 
            }
        }
    }
    private void Shoot(Vector3 targetPosition)
    {
        if (settings.projectilePrefab == null) return;

        // 1. Calculate direction
        Vector2 direction = (targetPosition - transform.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        // 2. OFFSET: Spawn the projectile 1.5 units away from the player's center 
        // toward the enemy so it doesn't touch the player's collider.
        Vector3 spawnPos = transform.position + (Vector3)(direction * 1.5f);
        
        GameObject projectile = Instantiate(settings.projectilePrefab, spawnPos, Quaternion.Euler(0, 0, angle));
        
        // 3. LAYER/COLLISION IGNORE: Even if it spawns close, tell physics to ignore the player
        // This assumes this script is a child of the Player object
        Collider2D playerCollider = transform.parent.GetComponent<Collider2D>();
        Collider2D projectileCollider = projectile.GetComponent<Collider2D>();
        
        if (playerCollider != null && projectileCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, projectileCollider);
        }

        // 4. Set Velocity
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = direction * projectileSpeed;
        }
        
        Destroy(projectile, 3f); 
    }
}