using UnityEngine;

public class PlayerRangedAttack : MonoBehaviour
{
    public GameObject projectilePrefab;
    public float firingCooldown = 1.25f;
    private float nextFiringTime = 0f;
    private Vector2 attackDirection;
    private float attackSpawnOffset = 1.5f;

    // We use the player sprite to determine in which direction the projectile should fly
    private PlayerController playerController;
    private int damage = 3;
    private float projectileSpeed = 8f;
    

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.K) && Time.time >= nextFiringTime)
        {
            FireAttack();
        }
    }

    private void FireAttack()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        // Decide in which direction the attack should fly
        switch (playerController.CurrentFacing)
        {
            case PlayerController.PlayerFacingDirection.Left:  
                attackDirection = Vector2.left;  
                break;

            case PlayerController.PlayerFacingDirection.Right: 
                attackDirection = Vector2.right; 
                break;

            case PlayerController.PlayerFacingDirection.Up:    
                attackDirection = Vector2.up;    
                break;

            case PlayerController.PlayerFacingDirection.Down:  
                attackDirection = Vector2.down;  
                break;
        }

        // Calculate cooldown until next attack
        nextFiringTime = Time.time + firingCooldown;

        // Spawn the projectile 1.5 units away from the player's center toward the enemy so it doesn't touch the 
        // player's collider
        Vector3 spawnPos = transform.position + (Vector3)(attackDirection * attackSpawnOffset);
        
        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        
        // Even if it spawns close, tell physics to ignore the player
        Collider2D playerCollider = GetComponent<Collider2D>();
        Collider2D projectileCollider = projectile.GetComponent<Collider2D>();
        
        if (playerCollider != null && projectileCollider != null)
        {
            Physics2D.IgnoreCollision(playerCollider, projectileCollider);
        }

        // Set Velocity
        Rigidbody2D rb = projectile.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = attackDirection * projectileSpeed;
        }
        
        Destroy(projectile, 3f); 
    }
}