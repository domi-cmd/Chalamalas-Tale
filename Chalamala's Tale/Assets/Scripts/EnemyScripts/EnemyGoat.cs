using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class EnemyGoat : MonoBehaviour
{
    private enum Phase
    {
        Wander,
        Charge,
        Ram,
        Confused
    }

    [Header("Health")]
    [SerializeField] private float maxHealth = 3f;
    private float currentHealth;

    [Header("Targeting")]
    [SerializeField] private string playerObjectName = "Player";
    private GameObject player;

    [Header("Timings")]
    [SerializeField] private float wanderDurationSeconds = 3f;
    [SerializeField] private float chargeDurationSeconds = 2f;
    [SerializeField] private float confusedDurationSeconds = 4f;

    [Header("Movement")]
    [SerializeField] private float wanderSpeed = 1.25f;
    [SerializeField] private float ramSpeed = 5.5f;
    [Tooltip("How often the goat picks a new random wander direction.")]
    [SerializeField] private float wanderDirectionChangeIntervalSeconds = 0.6f;

    [Header("Wall Detection")]
    [Tooltip("Which layers count as walls for ending the ram.")]
    [SerializeField] private LayerMask wallLayerMask;

    [Header("Player Contact Damage")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float contactDamageAmount = 1f;
    [Tooltip("Prevents multiple hits if the player has multiple colliders or re-enters quickly.")]
    [SerializeField] private float contactDamageCooldownSeconds = 0.25f;
    private float nextContactDamageTime;

    [Header("Impact Projectiles")]
    [SerializeField] private GameObject impactProjectilePrefab;
    [SerializeField] private int impactProjectileCount = 6;
    [SerializeField] private float impactProjectileSpeed = 7f;
    [SerializeField] private float impactProjectileLifetimeSeconds = 3.5f;
    [SerializeField] private float impactProjectileSpawnOffset = 0.15f;

    [Header("Phase Visuals")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private bool setColorByPhase = true;
    [SerializeField] private Color wanderColor = Color.white;
    [SerializeField] private Color chargeColor = new Color(1f, 0.85f, 0.35f, 1f);
    [SerializeField] private Color ramColor = new Color(1f, 0.35f, 0.35f, 1f);
    [SerializeField] private Color confusedColor = new Color(0.6f, 0.6f, 1f, 1f);

    [SerializeField] private bool swapSpriteByPhase = false;
    [SerializeField] private Sprite wanderSprite;
    [SerializeField] private Sprite chargeSprite;
    [SerializeField] private Sprite ramSprite;
    [SerializeField] private Sprite confusedSprite;

    [Header("Facing")]
    [Tooltip("If true, the goat will flip left/right using SpriteRenderer.flipX and never rotate.")]
    [SerializeField] private bool faceUsingFlipX = true;
    [Tooltip("Set based on your art: true if the sprite looks RIGHT by default (flipX=false).")]
    [SerializeField] private bool spriteFacesRightByDefault = true;

    private Rigidbody2D body;
    private Collider2D myCollider;

    private Phase currentPhase = Phase.Wander;
    private float phaseTimer;

    private Vector2 wanderDirection;
    private float nextWanderDirectionPickTime;

    private Vector2 lockedRamDirection;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        myCollider = GetComponent<Collider2D>();

        body = GetComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.freezeRotation = true;
        body.rotation = 0f;
    }

    private void Start()
    {
        player = GameObject.Find(playerObjectName);
        EnterPhase(Phase.Wander);

#if UNITY_EDITOR
        if (impactProjectilePrefab != null && impactProjectilePrefab.scene.IsValid())
        {
            impactProjectilePrefab.SetActive(false);
            Debug.LogWarning(
                $"{name}: 'Impact Projectile Prefab' is a scene object. Make it a prefab asset so you don't need one in the scene.",
                this);
        }
#endif
    }

    private void Update()
    {
        if (player == null)
        {
            // If the player isn't present, just keep wandering.
            if (currentPhase != Phase.Wander)
            {
                EnterPhase(Phase.Wander);
            }

            UpdateWander();
            return;
        }

        switch (currentPhase)
        {
            case Phase.Wander:
                UpdateWander();
                break;

            case Phase.Charge:
                UpdateCharge();
                break;

            case Phase.Ram:
                UpdateRam();
                break;

            case Phase.Confused:
                UpdateConfused();
                break;
        }
    }
    void OnEnable()
    {
        Debug.Log("GOAT ENABLED");
        AudioManager am = FindAnyObjectByType<AudioManager>();
        if (am != null)
            am.RegisterEnemy();
    }

    void OnDisable()
    {
        AudioManager am = FindAnyObjectByType<AudioManager>();
        if (am != null)
            am.UnregisterEnemy();
    }
    private void EnterPhase(Phase newPhase)
    {
        currentPhase = newPhase;
        phaseTimer = 0f;
        ApplyPhaseVisuals();

        // During the ram attack, the goat should pass through the player instead of being blocked.
        // We do this by making its collider a trigger only for the Ram phase.
        if (myCollider != null)
        {
            myCollider.isTrigger = currentPhase == Phase.Ram;
        }

        switch (currentPhase)
        {
            case Phase.Wander:
                PickNewWanderDirection();
                nextWanderDirectionPickTime = Time.time + wanderDirectionChangeIntervalSeconds;
                body.linearVelocity = wanderDirection * wanderSpeed;
                break;

            case Phase.Charge:
                body.linearVelocity = Vector2.zero;
                break;

            case Phase.Ram:
                lockedRamDirection = GetDirectionToPlayer();
                if (lockedRamDirection.sqrMagnitude < 0.0001f)
                {
                    lockedRamDirection = Vector2.right;
                }

                body.linearVelocity = lockedRamDirection * ramSpeed;
                break;

            case Phase.Confused:
                body.linearVelocity = Vector2.zero;
                break;
        }
    }

    private void UpdateWander()
    {
        phaseTimer += Time.deltaTime;

        if (Time.time >= nextWanderDirectionPickTime)
        {
            PickNewWanderDirection();
            nextWanderDirectionPickTime = Time.time + wanderDirectionChangeIntervalSeconds;
        }

        body.linearVelocity = wanderDirection * wanderSpeed;
        UpdateFacing(wanderDirection);

        if (phaseTimer >= wanderDurationSeconds)
        {
            EnterPhase(Phase.Charge);
        }
    }

    private void UpdateCharge()
    {
        phaseTimer += Time.deltaTime;

        // Aim at the player while charging up (left/right only).
        Vector2 aimDir = GetDirectionToPlayer();
        UpdateFacing(aimDir);

        if (phaseTimer >= chargeDurationSeconds)
        {
            EnterPhase(Phase.Ram);
        }
    }

    private void UpdateRam()
    {
        // Keep ramming in the locked direction until we hit a wall.
        body.linearVelocity = lockedRamDirection * ramSpeed;
        UpdateFacing(lockedRamDirection);
    }

    private void UpdateConfused()
    {
        phaseTimer += Time.deltaTime;
        body.linearVelocity = Vector2.zero;

        if (phaseTimer >= confusedDurationSeconds)
        {
            EnterPhase(Phase.Wander);
        }
    }

    private Vector2 GetDirectionToPlayer()
    {
        if (player == null)
        {
            return Vector2.zero;
        }

        return ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
    }

    private void PickNewWanderDirection()
    {
        wanderDirection = Random.insideUnitCircle;
        if (wanderDirection.sqrMagnitude < 0.0001f)
        {
            wanderDirection = Vector2.right;
        }

        wanderDirection.Normalize();
    }

    private void UpdateFacing(Vector2 direction)
    {
        if (!faceUsingFlipX || spriteRenderer == null)
        {
            return;
        }

        if (direction.x > 0.01f)
        {
            spriteRenderer.flipX = !spriteFacesRightByDefault;
        }
        else if (direction.x < -0.01f)
        {
            spriteRenderer.flipX = spriteFacesRightByDefault;
        }
    }

    private void ApplyPhaseVisuals()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        if (setColorByPhase)
        {
            switch (currentPhase)
            {
                case Phase.Wander:
                    spriteRenderer.color = wanderColor;
                    break;
                case Phase.Charge:
                    spriteRenderer.color = chargeColor;
                    break;
                case Phase.Ram:
                    spriteRenderer.color = ramColor;
                    break;
                case Phase.Confused:
                    spriteRenderer.color = confusedColor;
                    break;
            }
        }

        if (swapSpriteByPhase)
        {
            Sprite nextSprite = null;
            switch (currentPhase)
            {
                case Phase.Wander:
                    nextSprite = wanderSprite;
                    break;
                case Phase.Charge:
                    nextSprite = chargeSprite;
                    break;
                case Phase.Ram:
                    nextSprite = ramSprite;
                    break;
                case Phase.Confused:
                    nextSprite = confusedSprite;
                    break;
            }

            if (nextSprite != null)
            {
                spriteRenderer.sprite = nextSprite;
            }
        }
    }

    public void TakeDamage(float damageAmount)
    {
        // The goat is only vulnerable while confused.
        if (currentPhase != Phase.Confused)
        {
            return;
        }

        if (damageAmount <= 0f)
        {
            return;
        }

        currentHealth -= damageAmount;
        if (currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.gameObject == null)
        {
            return;
        }

        if (!IsInLayerMask(collision.gameObject, wallLayerMask))
        {
            return;
        }

        // We only care about wall hits while ramming.
        if (currentPhase != Phase.Ram)
        {
            return;
        }

        Vector2 impactPoint = transform.position;
        Vector2 impactNormal = Vector2.zero;

        if (collision.contactCount > 0)
        {
            var contact = collision.GetContact(0);
            impactPoint = contact.point;

            // Compute a normal that points away from the wall (toward the goat).
            impactNormal = ((Vector2)transform.position - contact.point).normalized;
            if (impactNormal.sqrMagnitude < 0.0001f)
            {
                impactNormal = contact.normal;
            }
        }
        else
        {
            impactNormal = -lockedRamDirection;
        }

        OnWallHit(impactPoint, impactNormal, collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null)
        {
            return;
        }

        // While ramming, the goat should pass through the player but still deal damage.
        if (currentPhase == Phase.Ram && other.CompareTag(playerTag))
        {
            if (Time.time >= nextContactDamageTime)
            {
                var playerHealth = other.GetComponentInParent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(contactDamageAmount);
                }

                nextContactDamageTime = Time.time + contactDamageCooldownSeconds;
            }

            return;
        }

        if (!IsInLayerMask(other.gameObject, wallLayerMask))
        {
            return;
        }

        if (currentPhase != Phase.Ram)
        {
            return;
        }

        Vector2 impactPoint = other.ClosestPoint(transform.position);
        Vector2 impactNormal = ((Vector2)transform.position - impactPoint).normalized;
        if (impactNormal.sqrMagnitude < 0.0001f)
        {
            impactNormal = -lockedRamDirection;
        }

        OnWallHit(impactPoint, impactNormal, other);
    }

    private void OnWallHit(Vector2 impactPoint, Vector2 impactNormal, Collider2D wallCollider)
    {
        if (impactNormal.sqrMagnitude < 0.0001f)
        {
            impactNormal = -lockedRamDirection;
        }

        impactNormal.Normalize();

        SpawnImpactProjectiles(impactPoint, impactNormal, wallCollider);
        EnterPhase(Phase.Confused);
    }

    private void SpawnImpactProjectiles(Vector2 impactPoint, Vector2 awayFromWallNormal, Collider2D wallColliderToIgnore)
    {
        if (impactProjectilePrefab == null)
        {
            Debug.LogWarning($"{name}: No impact projectile prefab assigned (Impact Projectiles > Impact Projectile Prefab).", this);
            return;
        }

        if (impactProjectileCount <= 0)
        {
            return;
        }

        Vector2 spawnPos = impactPoint + (awayFromWallNormal * impactProjectileSpawnOffset);
        var myColliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < impactProjectileCount; i++)
        {
            Vector2 dir = RandomDirectionAwayFromNormal(awayFromWallNormal);
            Quaternion rot = Quaternion.FromToRotation(Vector3.right, dir);

            GameObject projectile = Instantiate(impactProjectilePrefab, spawnPos, rot);
            if (!projectile.activeSelf)
            {
                projectile.SetActive(true);
            }

            var projectileCollider = projectile.GetComponent<Collider2D>();
            if (projectileCollider != null)
            {
                if (wallColliderToIgnore != null)
                {
                    Physics2D.IgnoreCollision(projectileCollider, wallColliderToIgnore);
                }

                for (int c = 0; c < myColliders.Length; c++)
                {
                    if (myColliders[c] != null)
                    {
                        Physics2D.IgnoreCollision(projectileCollider, myColliders[c]);
                    }
                }
            }

            var projectileBody = projectile.GetComponent<Rigidbody2D>();
            if (projectileBody != null)
            {
                projectileBody.linearVelocity = dir * impactProjectileSpeed;
            }

            if (impactProjectileLifetimeSeconds > 0f)
            {
                Destroy(projectile, impactProjectileLifetimeSeconds);
            }
        }
    }

    private static Vector2 RandomDirectionAwayFromNormal(Vector2 normal)
    {
        normal = normal.sqrMagnitude < 0.0001f ? Vector2.right : normal.normalized;

        // Pick a random direction in the hemisphere defined by the wall normal.
        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector2 candidate = Random.insideUnitCircle;
            if (candidate.sqrMagnitude < 0.0001f)
            {
                continue;
            }

            candidate.Normalize();
            if (Vector2.Dot(candidate, normal) > 0f)
            {
                return candidate;
            }
        }

        return normal;
    }

    private static bool IsInLayerMask(GameObject obj, LayerMask mask)
    {
        return (mask.value & (1 << obj.layer)) != 0;
    }
}
