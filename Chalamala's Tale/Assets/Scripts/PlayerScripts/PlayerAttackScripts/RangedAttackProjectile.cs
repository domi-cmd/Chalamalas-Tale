using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PlayerArrowProjectile : MonoBehaviour
{
    [Header("Damage")]
    [SerializeField] private int damageAmount = 3;
    [SerializeField] private string enemyTag = "Enemy";

    [Header("Collision Filters")]
    [SerializeField] private LayerMask wallLayerMask;
    private const float wallArmDelaySeconds = 0.05f;

    private float spawnTime;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

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
        if (other.CompareTag(enemyTag))
        {
            other.GetComponentInParent<EnemyChasing>()?.TakeDamage(damageAmount);
            Destroy(gameObject);
            return;
        }

        if (((1 << other.gameObject.layer) & wallLayerMask) != 0 
            && Time.time - spawnTime >= wallArmDelaySeconds)
            Destroy(gameObject);
    }

    private static bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }
}