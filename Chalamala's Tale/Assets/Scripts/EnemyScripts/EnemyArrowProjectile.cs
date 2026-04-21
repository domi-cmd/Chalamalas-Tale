using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class EnemyArrowProjectile : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private float damageAmount = 1f;
    [SerializeField] private string playerTag = "Player";

    [Header("Collision Filters")]
    [Tooltip("Layers the arrow should be blocked by (typically your level walls).")]
    [SerializeField] private LayerMask wallLayerMask;

    [Tooltip("Prevents the arrow from despawning immediately if it spawns overlapping a wall collider.")]
    [SerializeField] private float wallArmDelaySeconds = 0.05f;

    private Collider2D projectileCollider;
    private float spawnTime;

    private void Awake()
    {
        projectileCollider = GetComponent<Collider2D>();

        // Make this a trigger so it doesn't physically collide with anything.
        // We'll still detect overlaps and selectively react to walls/player.
        projectileCollider.isTrigger = true;

        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        }
    }

    private void OnEnable()
    {
        spawnTime = Time.time;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        // Damage the player, then destroy the projectile.
        if (other.CompareTag(playerTag))
        {
            var playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damageAmount);
            }

            Destroy(gameObject);
            return;
        }

        // Only "collide" with (i.e., be blocked by) walls.
        if (IsInLayerMask(other.gameObject, wallLayerMask) && (Time.time - spawnTime) >= wallArmDelaySeconds)
        {
            Destroy(gameObject);
        }

        // Anything else: ignore (pass through).
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        if (IsInLayerMask(other.gameObject, wallLayerMask) && (Time.time - spawnTime) >= wallArmDelaySeconds)
        {
            Destroy(gameObject);
        }
    }

    private static bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }
}
