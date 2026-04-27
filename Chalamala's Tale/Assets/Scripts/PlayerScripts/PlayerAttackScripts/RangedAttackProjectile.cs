using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerArrowProjectile : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damageAmount = 3; // Changed to int to match your EnemyChasing
    [SerializeField] private string enemyTag = "Enemy";

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

        // Ensure it's a trigger
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
        if (other == null) return;

        // 1. Damage the ENEMY, then destroy the projectile.
        if (other.CompareTag(enemyTag))
        {
            // Look for your enemy script
            var enemy = other.GetComponentInParent<EnemyChasing>();
            if (enemy != null)
            {
                enemy.TakeDamage(damageAmount);
            }

            Destroy(gameObject);
            return;
        }

        // 2. Blocked by walls using LayerMask (prevents the "Tag not defined" error)
        if (IsInLayerMask(other.gameObject, wallLayerMask) && (Time.time - spawnTime) >= wallArmDelaySeconds)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (other == null) return;

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