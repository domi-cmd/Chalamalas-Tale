using System.Collections;
using UnityEngine;

/// Knight
/// intermediate enemy. Charges at the player when in range, but stops to attack with a sweeping melee attack when close enough.
/// Can also block incoming attacks for a short duration, but has a cooldown for this.
/// todo: add animator for attack
/// todo: draw sprite

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyKnight : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 5f;
    private float currentHealth;

    // to have a signal of damage taken, the sprite changes color
    [Header("Damage Flash")]
    [SerializeField] private Color damageColor = Color.red;
    [SerializeField] private float flashDuration = 0.1f;

    public Color originalColor;
    private Coroutine flashCoroutine;

    [Header("Targeting")]
    [SerializeField] private string playerObjectName = "Player";
    [SerializeField] private float aggroRangeRadius = 6f;
    [SerializeField] private float attackRangeRadius = 1.8f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 1.5f;
    [SerializeField] private bool stopWhileAttacking = true;

    [Header("Block")]
    [SerializeField] private float blockDurationSeconds = 2f;
    [Tooltip("Minimum time between the start of one block and the next.")]
    [SerializeField] private float blockCooldownSeconds = 4f;
    [SerializeField] private Sprite blockSprite;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Sweep Attack")]
    [SerializeField] private float attackCooldownSeconds = 1.75f;
    [SerializeField] private float attackWindupSeconds = 0.15f;
    [SerializeField] private float sweepRange = 2f;
    [Range(0f, 180f)]
    [SerializeField] private float sweepAngleDegrees = 180f;
    [SerializeField] private float sweepDamage = 1f;
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private Transform attackOrigin;
    [Tooltip("Optional sprite shown during the attack windup/swing.")]
    [SerializeField] private Sprite attackSprite;
    [Tooltip("Optional color tint applied during the attack windup/swing.")]
    [SerializeField] private Color attackColor = new Color(1f, 0.85f, 0.85f, 1f);

    [Header("Sword Swing Visual")]
    [Tooltip("Sword pivot child (usually at the hand/hilt). Rotated during attack.")]
    [SerializeField] private Transform swordPivot;
    [SerializeField] private float swordSwingStartAngle = -70f;
    [SerializeField] private float swordSwingEndAngle = 70f;
    [SerializeField] private float swordSwingDurationSeconds = 0.18f;
    [SerializeField] private AnimationCurve swordSwingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Tooltip("Mirrors the swing when facing left so the sweep direction stays natural.")]
    [SerializeField] private bool mirrorSwingByFacing = true;

    [Header("Facing")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [Tooltip("If true, flip left/right based on facing direction X.")]
    [SerializeField] private bool faceUsingFlipX = true;
    [SerializeField] private bool spriteFacesRightByDefault = true;

    private Rigidbody2D body;
    private GameObject player;
    private Transform playerTransform;
    private Sprite defaultSprite;
    private Color defaultColor = Color.white;

    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int IsBlockingHash = Animator.StringToHash("IsBlocking");

    private Vector2 facingDirection = Vector2.down;
    private float nextAttackTime;
    private float nextAllowedBlockTime;
    private bool isBlocking;
    private bool isAttacking;
    private Quaternion swordDefaultLocalRotation = Quaternion.identity;
    private bool isSwordSwinging;
    private float currentSwordSwingAngle;

    private void Awake()
    {
        currentHealth = maxHealth;
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        if (spriteRenderer != null)
        {
            defaultSprite = spriteRenderer.sprite;
            defaultColor = spriteRenderer.color;
        }

        if (attackOrigin == null)
        {
            attackOrigin = transform;
        }

        if (swordPivot != null)
        {
            swordDefaultLocalRotation = swordPivot.localRotation;
        }
    }

    private void Start()
    {
        player = GameObject.Find(playerObjectName);
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void Update()
    {
        if (playerTransform == null)
        {
            if (player == null)
            {
                player = GameObject.Find(playerObjectName);
                if (player == null)
                {
                    body.linearVelocity = Vector2.zero;
                    return;
                }
            }

            playerTransform = player.transform;
        }

        if (isBlocking)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 toPlayer = (Vector2)playerTransform.position - (Vector2)transform.position;
        float distanceToPlayer = toPlayer.magnitude;

        if (distanceToPlayer > 0.001f)
        {
            facingDirection = toPlayer.normalized;
            UpdateFacingVisuals(facingDirection);
        }

        if (distanceToPlayer > aggroRangeRadius)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (!isAttacking && distanceToPlayer <= attackRangeRadius && Time.time >= nextAttackTime)
        {
            StartCoroutine(SweepAttackCoroutine());
            return;
        }

        if (isAttacking && stopWhileAttacking)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        if (distanceToPlayer > attackRangeRadius)
        {
            body.linearVelocity = facingDirection * moveSpeed;
        }
        else
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void LateUpdate()
    {
        if (!isSwordSwinging)
        {
            return;
        }

        ApplyCurrentSwordRotation();
    }

    public void TakeDamage(float damageAmount)
    {
        if (damageAmount <= 0f || currentHealth <= 0f)
        {
            return;
        }
        // damage signal
        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(DamageFlash());


        if (isBlocking)
        {
            return;
        }

        // First eligible incoming hit triggers a block instead of dealing damage.
        if (Time.time >= nextAllowedBlockTime)
        {
            StartCoroutine(BlockCoroutine());
            return;
        }

        currentHealth -= damageAmount;
        if (currentHealth <= 0f)
        {
            GetComponent<DropTable>()?.SpawnDrops();
            Destroy(gameObject);
        }
    }
    private IEnumerator DamageFlash()
    {
        spriteRenderer.color = damageColor;

        yield return new WaitForSeconds(flashDuration);

        spriteRenderer.color = originalColor;
    }


    private IEnumerator BlockCoroutine()
    {
        isBlocking = true;
        nextAllowedBlockTime = Time.time + Mathf.Max(blockDurationSeconds, blockCooldownSeconds);
        body.linearVelocity = Vector2.zero;
        ResetSwordPose();

        if (animator != null)
        {
            animator.SetBool(IsBlockingHash, true);
        }

        if (spriteRenderer != null && blockSprite != null)
        {
            spriteRenderer.sprite = blockSprite;
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.color = defaultColor;
        }

        yield return new WaitForSeconds(Mathf.Max(0f, blockDurationSeconds));

        isBlocking = false;
        if (animator != null)
        {
            animator.SetBool(IsBlockingHash, false);
        }
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = defaultSprite;
        }
    }

    private IEnumerator SweepAttackCoroutine()
    {
        isAttacking = true;
        nextAttackTime = Time.time + Mathf.Max(0.01f, attackCooldownSeconds);
        body.linearVelocity = Vector2.zero;

        if (animator != null)
        {
            animator.SetTrigger(AttackTriggerHash);
        }

        ApplyAttackVisuals();

        if (attackWindupSeconds > 0f)
        {
            yield return new WaitForSeconds(attackWindupSeconds);
        }

        if (swordPivot != null && swordSwingDurationSeconds > 0.01f)
        {
            yield return StartCoroutine(PlaySwordSwingAndApplyHit());
        }
        else
        {
            PerformSweepAttack();
        }

        RestorePostAttackVisuals();
        isAttacking = false;
    }

    private IEnumerator PlaySwordSwingAndApplyHit()
    {
        float duration = Mathf.Max(0.01f, swordSwingDurationSeconds);
        float elapsed = 0f;
        bool hitApplied = false;
        isSwordSwinging = true;

        float mirrorSign = 1f;
        if (mirrorSwingByFacing && Mathf.Abs(facingDirection.x) > 0.01f)
        {
            mirrorSign = facingDirection.x < 0f ? -1f : 1f;
        }

        float startAngle = swordSwingStartAngle * mirrorSign;
        float endAngle = swordSwingEndAngle * mirrorSign;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curved = swordSwingCurve != null ? swordSwingCurve.Evaluate(t) : t;
            currentSwordSwingAngle = Mathf.Lerp(startAngle, endAngle, curved);
            ApplyCurrentSwordRotation();

            if (!hitApplied && t >= 0.5f)
            {
                PerformSweepAttack();
                hitApplied = true;
            }

            yield return null;
        }

        if (!hitApplied)
        {
            PerformSweepAttack();
        }

        isSwordSwinging = false;
    }

    private void ApplyAttackVisuals()
    {
        if (animator != null || spriteRenderer == null || isBlocking)
        {
            return;
        }

        if (attackSprite != null)
        {
            spriteRenderer.sprite = attackSprite;
        }

        spriteRenderer.color = attackColor;
    }

    private void RestorePostAttackVisuals()
    {
        if (animator != null || spriteRenderer == null || isBlocking)
        {
            ResetSwordPose();
            return;
        }

        spriteRenderer.sprite = defaultSprite;
        spriteRenderer.color = defaultColor;
        ResetSwordPose();
    }

    private void ResetSwordPose()
    {
        isSwordSwinging = false;
        currentSwordSwingAngle = 0f;

        if (swordPivot != null)
        {
            swordPivot.localRotation = swordDefaultLocalRotation;
        }
    }

    private void ApplyCurrentSwordRotation()
    {
        if (swordPivot == null)
        {
            return;
        }

        swordPivot.localRotation = swordDefaultLocalRotation * Quaternion.Euler(0f, 0f, currentSwordSwingAngle);
    }

    private void PerformSweepAttack()
    {
        Vector2 origin = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        float maxDistance = Mathf.Max(0.01f, sweepRange);
        float halfAngle = Mathf.Clamp(sweepAngleDegrees, 0f, 180f) * 0.5f;
        Vector2 forward = facingDirection.sqrMagnitude > 0.0001f ? facingDirection.normalized : Vector2.down;

        Collider2D[] hits = playerLayerMask.value != 0
            ? Physics2D.OverlapCircleAll(origin, maxDistance, playerLayerMask)
            : Physics2D.OverlapCircleAll(origin, maxDistance);

        for (int i = 0; i < hits.Length; i++)
        {
            if (hits[i] == null)
            {
                continue;
            }

            Vector2 toTarget = (Vector2)hits[i].bounds.center - origin;
            if (toTarget.sqrMagnitude > maxDistance * maxDistance)
            {
                continue;
            }

            if (toTarget.sqrMagnitude <= 0.0001f || Vector2.Angle(forward, toTarget) <= halfAngle)
            {
                PlayerHealth playerHealth = hits[i].GetComponentInParent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(sweepDamage);
                    break;
                }
            }
        }
    }

    private void UpdateFacingVisuals(Vector2 direction)
    {
        if (!faceUsingFlipX || spriteRenderer == null)
        {
            return;
        }

        if (Mathf.Abs(direction.x) <= 0.01f)
        {
            return;
        }

        if (direction.x > 0f)
        {
            spriteRenderer.flipX = !spriteFacesRightByDefault;
        }
        else
        {
            spriteRenderer.flipX = spriteFacesRightByDefault;
        }
    }

    private void OnEnable()
    {
        AudioManager am = FindAnyObjectByType<AudioManager>();
        if (am != null)
        {
            am.RegisterEnemy();
        }
    }

    private void OnDisable()
    {
        AudioManager am = FindAnyObjectByType<AudioManager>();
        if (am != null)
        {
            am.UnregisterEnemy();
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Vector2 origin = attackOrigin != null ? (Vector2)attackOrigin.position : (Vector2)transform.position;
        Vector2 forward = facingDirection.sqrMagnitude > 0.0001f ? facingDirection.normalized : Vector2.down;
        float range = Mathf.Max(0.01f, sweepRange);
        float halfAngle = Mathf.Clamp(sweepAngleDegrees, 0f, 180f) * 0.5f;

        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.4f);
        Gizmos.DrawWireSphere(origin, range);

        Quaternion leftRot = Quaternion.Euler(0f, 0f, halfAngle);
        Quaternion rightRot = Quaternion.Euler(0f, 0f, -halfAngle);
        Vector3 leftDir = leftRot * (Vector3)forward;
        Vector3 rightDir = rightRot * (Vector3)forward;

        Gizmos.DrawLine(origin, origin + (Vector2)leftDir * range);
        Gizmos.DrawLine(origin, origin + (Vector2)rightDir * range);
        Gizmos.DrawLine(origin, origin + forward * range);
    }
#endif
}
