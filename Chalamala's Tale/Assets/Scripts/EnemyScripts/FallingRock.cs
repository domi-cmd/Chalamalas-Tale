using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class FallingRock : MonoBehaviour
{
    [Header("Drop")]
    [SerializeField] private float warningDelaySeconds = 2f;
    [Tooltip("Downward speed once the rock starts dropping. Works even if Physics2D gravity is 0.")]
    [SerializeField] private float fallSpeed = 10f;

    [Header("Damage")]
    [SerializeField] private float damageRadius = 1.5f;
    [SerializeField] private float damageAmount = 1f;
    [SerializeField] private LayerMask playerLayerMask;

    [Header("Lifetime")]
    [SerializeField] private float destroyAfterSeconds = 6f;

    private Rigidbody2D body;
    private Collider2D col;

    private bool isDropping;
    private bool hasImpacted;

    public void Initialize(float warningDelay, float speed, float radius, float damage, LayerMask playerMask)
    {
        warningDelaySeconds = Mathf.Max(0f, warningDelay);
        fallSpeed = Mathf.Max(0f, speed);
        damageRadius = Mathf.Max(0f, radius);
        damageAmount = Mathf.Max(0f, damage);
        playerLayerMask = playerMask;

        // Re-arm drop timing with updated settings.
        CancelInvoke(nameof(BeginDrop));
        if (isActiveAndEnabled)
        {
            Invoke(nameof(BeginDrop), warningDelaySeconds);
        }
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        body.gravityScale = 0f;
        body.bodyType = RigidbodyType2D.Kinematic;
        body.freezeRotation = true;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;

        // During the warning phase, don't let this collide with / hurt anything.
        col.enabled = false;

        if (destroyAfterSeconds > 0f)
        {
            Destroy(gameObject, destroyAfterSeconds);
        }
    }

    private void OnEnable()
    {
        Invoke(nameof(BeginDrop), Mathf.Max(0f, warningDelaySeconds));
    }

    private void BeginDrop()
    {
        if (hasImpacted)
        {
            return;
        }

        isDropping = true;
        col.enabled = true;
        body.bodyType = RigidbodyType2D.Dynamic;
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void FixedUpdate()
    {
        if (!isDropping || hasImpacted)
        {
            return;
        }

        float speed = Mathf.Max(0f, fallSpeed);
        body.linearVelocity = Vector2.down * speed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!isDropping || hasImpacted)
        {
            return;
        }

        hasImpacted = true;

        // Radius damage on impact.
        if (damageAmount > 0f && damageRadius > 0f)
        {
            Collider2D[] hits = playerLayerMask.value != 0
                ? Physics2D.OverlapCircleAll(transform.position, damageRadius, playerLayerMask)
                : Physics2D.OverlapCircleAll(transform.position, damageRadius);

            for (int i = 0; i < hits.Length; i++)
            {
                if (hits[i] == null) continue;

                PlayerHealth ph = hits[i].GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    ph.TakeDamage(damageAmount);
                    break;
                }
            }
        }

        Destroy(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, damageRadius);
    }
#endif
}
