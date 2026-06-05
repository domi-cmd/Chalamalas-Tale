using UnityEngine;


/// A projectile fired by the dragon during Phase 2.
/// Uses a Dynamic Rigidbody2D so it is physically blocked by the boulder.
/// Deals damage on collision with the player and destroys itself on any solid hit.

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CircleCollider2D))]
public class DragonProjectile : MonoBehaviour
{
    private float damage;
    private LayerMask playerLayerMask;
    private bool hasHit;

    private Rigidbody2D body;
    private Collider2D ownCollider;

    public void Initialize(Vector2 direction, float speed, float dmg, float lifetime, LayerMask playerMask)
    {
        damage = Mathf.Max(0f, dmg);
        playerLayerMask = playerMask;

        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        // In 2D, local +X (transform.right) is treated as the projectile tip direction.
        transform.right = dir;
        body.linearVelocity = dir * Mathf.Max(0f, speed);

        Destroy(gameObject, Mathf.Max(0.1f, lifetime));
    }

    // Call this so the projectile doesn't immediately collide with the dragon that fired it
    public void IgnoreCollider(Collider2D other)
    {
        if (ownCollider != null && other != null)
        {
            Physics2D.IgnoreCollision(ownCollider, other);
        }
    }

    private void Awake()
    {
        // Ensure required components exist regardless of prefab setup
        body = GetComponent<Rigidbody2D>();
        if (body == null) body = gameObject.AddComponent<Rigidbody2D>();

        ownCollider = GetComponent<CircleCollider2D>();
        if (ownCollider == null) ownCollider = gameObject.AddComponent<CircleCollider2D>();

        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        AudioManager.instance.PlaySFX(AudioManager.instance.fireball);

    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasHit) return;

        // Check if we hit the player
        PlayerHealth ph = collision.gameObject.GetComponentInParent<PlayerHealth>();
        if (ph != null)
        {
            hasHit = true;
            ph.TakeDamage(damage);
        }

        // Destroy on any solid collision (boulder, wall, or player)
        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        var col = GetComponent<CircleCollider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, col.radius);
        }
    }
#endif
}
