using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

/*
Dragon Boss:
- boss fight divided in 3 phases (the change of phase is induced by the current health of the dragon)
- attacks:
    - Phase 1: Roar that knocks the player back, and random flame sprouts on the ground that 
    damage the player if they stay in the area for too long
    - Phase 2: Spawns a boulder that the player can hide behind, and shoots projectiles in an arc that the player has to 
    dodge while hiding behind the boulder. The boulder changes position after each barrage.
    - Phase 3: Wicked spiral flame attack, todo

    todo: sprites, animations, boss health, victory screen
*/

[RequireComponent(typeof(Collider2D))]
public class EnemyDragon : MonoBehaviour, IDamageable
{
    [Header("Visuals")]
    [FormerlySerializedAs("roaring")]
    [SerializeField] private Sprite roarSprite;
    [Tooltip("How long the roar sprite stays visible.")]
    [SerializeField] private float roarSpriteDurationSeconds = 0.35f;

    private SpriteRenderer spriteRenderer;
    private Sprite defaultSprite;

    public enum DragonPhase
    {
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3
    }

    [Serializable]
    public class DragonPhaseChangedEvent : UnityEvent<DragonPhase> { }

    [Header("Boss Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("UI")]
    [SerializeField] private Slider healthBar;


    /*
    Flames spawned later in the battle and relative variables
    
    public GameObject flamePrefab;

    // values for different patterns
    public Transform centerPoint;
    public float initialRadius = 0.5f;
    public float radiusStep = 0.1f;
    public float angleStep = 10f;
    public int flameCount = 70;
    public float spiralDelay = 0.01f;

    // to handle delays of flame choreos
    private float flameTimer = 0f;
    public float flameCooldown = 4f;
    private float spiralTimer = 0f;
    public float spiralCooldown = 10f;
    */

    [Header("Phases")]
    [Tooltip("Switch to Phase 2 when health <= maxHealth * this value")]
    [Range(0f, 1f)]
    [SerializeField] private float phase2HealthPercent = 0.66f;
    [Tooltip("Switch to Phase 3 when health <= maxHealth * this value")]
    [Range(0f, 1f)]
    [SerializeField] private float phase3HealthPercent = 0.33f;
    [SerializeField] private DragonPhaseChangedEvent onPhaseChanged;

    [Header("Immovable")]
    [SerializeField] private bool makeImmovable = true;

    [Header("Phase 1: Roar")]
    [SerializeField] private float roarIntervalSeconds = 5f;
    [Tooltip("Approx. distance the roar should push the player")]
    [SerializeField] private float roarKnockbackDistance = 4f;
    [SerializeField] private float roarKnockbackDurationSeconds = 0.25f;

    [Header("Phase 1: Ground Flame Sprouts")]
    [Tooltip("Prefab to spawn for the flame sprout hazard. If omitted, 'flamePrefab' is used.")]
    [FormerlySerializedAs("fallingRockPrefab")]
    [SerializeField] private GameObject flameSproutPrefab;

    [Tooltip("World-space bounds of the arena. Flames spawn randomly inside this rectangle.")]
    [SerializeField] private Vector2 flameAreaMin = new Vector2(-8f, -5f);
    [SerializeField] private Vector2 flameAreaMax = new Vector2(8f, 5f);
    [Tooltip("Optional transform used as center of the Phase 1 flame spawn area.")]
    [SerializeField] private Transform flameSpawnAreaCenter;
    [Tooltip("Optional rectangle size for Phase 1 flames when using Flame Spawn Area Center.")]
    [SerializeField] private Vector2 flameSpawnAreaSize = new Vector2(16f, 10f);
    [Tooltip("Extra inward padding from flame spawn area edges.")]
    [SerializeField] private float flameSpawnEdgePadding = 0f;

    [FormerlySerializedAs("rockSpawnIntervalSeconds")]
    [SerializeField] private float flameWaveIntervalSeconds = 1.25f;

    [FormerlySerializedAs("rockWarningDelaySeconds")]
    [SerializeField] private float flameWarningDelaySeconds = 2f;

    [Tooltip("How many flames to spawn per wave. One is always targeted at the player; the rest are random.")]
    [SerializeField] private int flameCountPerWave = 5;

    [FormerlySerializedAs("rockDamageRadius")]
    [SerializeField] private float flameDamageRadius = 0.6f;

    [FormerlySerializedAs("rockDamageAmount")]
    [SerializeField] private float flameDamageAmount = 1f;

    [SerializeField] private float flameLifetimeSeconds = 2.5f;

    [SerializeField] private LayerMask playerLayerMask;

    [FormerlySerializedAs("maxActiveRocks")]
    [SerializeField] private int maxActiveFlames = 12;

    [Header("Phase 2: Boulder")]
    [Tooltip("Prefab with a solid Collider2D. The player hides behind this.")]
    [SerializeField] private GameObject boulderPrefab;
    [Tooltip("Optional transform used as center of the boulder spawn area.")]
    [SerializeField] private Transform boulderSpawnAreaCenter;
    [Tooltip("Fallback center if Boulder Spawn Area Center is not assigned.")]
    [SerializeField] private Vector2 boulderSpawnPosition = Vector2.zero;
    [Tooltip("Size of the random boulder spawn rectangle (no collider needed).")]
    [SerializeField] private Vector2 boulderSpawnAreaSize = new Vector2(10f, 6f);
    [Tooltip("Extra inward padding from spawn area edges.")]
    [SerializeField] private float boulderSpawnEdgePadding = 0.25f;
    [SerializeField] private float boulderWarningDelaySeconds = 2f;

    [Header("Phase 2: Projectile Barrage")]
    [Tooltip("Prefab with a Rigidbody2D + CircleCollider2D. If omitted, a simple circle is created.")]
    [SerializeField] private GameObject projectilePrefab;
    [Tooltip("How long each barrage lasts.")]
    [SerializeField] private float barrageDurationSeconds = 5f;
    [Tooltip("Cooldown between barrages.")]
    [SerializeField] private float barrageCooldownSeconds = 10f;
    [SerializeField] private float projectilesPerSecond = 15f;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileDamage = 1f;
    [SerializeField] private float projectileLifetime = 6f;
    [Tooltip("Optional exact origin transform (for example, dragon mouth). If null, local offset is used.")]
    [SerializeField] private Transform projectileSpawnPoint;
    [Tooltip("Fallback local-space spawn offset when Projectile Spawn Point is not assigned.")]
    [SerializeField] private Vector2 projectileSpawnLocalOffset = new Vector2(0.6f, 0f);
    [Tooltip("Extra distance from spawn origin along projectile travel direction.")]
    [SerializeField] private float projectileSpawnOffset = 0.6f;
    [Tooltip("Arc start angle in degrees. Unity convention: 0=right 90=up 180=left 270=down.")]
    [SerializeField] private float barrageMinAngle = 180f;
    [Tooltip("Arc end angle in degrees.")]
    [SerializeField] private float barrageMaxAngle = 360f;

    [Header("Debug")]
    [SerializeField] private bool logPhase1Actions = false;
    [Tooltip("Override the starting phase when pressing Play. Useful for testing.")]
    [SerializeField] private DragonPhase debugStartingPhase = DragonPhase.Phase1;

    private DragonPhase currentPhase = DragonPhase.Phase1;
    private Rigidbody2D body;

    private PlayerController playerController;
    private Transform playerTransform;

    private float nextRoarTime;
    private float nextFlameWaveTime;
    private readonly List<FlameSproutHazard> activeFlames = new List<FlameSproutHazard>();

    // Phase 2 state
    private bool phase2Initialized = false;
    private bool barrageRunning = false;
    private float nextBarrageTime = 0f;
    private GameObject spawnedBoulder;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public DragonPhase CurrentPhase => currentPhase;

    public event Action<DragonPhase> OnPhaseChanged;

    private void Awake()
    {
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            defaultSprite = spriteRenderer.sprite;
        }

        if (maxHealth <= 0f)
        {
            maxHealth = 100f;
        }

        currentHealth = maxHealth;
        if (healthBar != null)
        {
            healthBar.value = 1f;
        }

        currentPhase = debugStartingPhase;

        // Force health into the correct range for the chosen starting phase
        if (debugStartingPhase == DragonPhase.Phase3)
        {
            currentHealth = maxHealth * Mathf.Clamp01(phase3HealthPercent) * 0.5f;
        }
        else if (debugStartingPhase == DragonPhase.Phase2)
        {
            currentHealth = maxHealth * Mathf.Lerp(
                Mathf.Clamp01(phase3HealthPercent),
                Mathf.Clamp01(phase2HealthPercent),
                0.5f);
        }

        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }

        body = GetComponent<Rigidbody2D>();
        if (makeImmovable && body != null)
        {
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
        }

        CachePlayerRefs();
        nextRoarTime = Time.time + Mathf.Max(0.01f, roarIntervalSeconds);
        nextFlameWaveTime = Time.time + Mathf.Max(0.01f, flameWaveIntervalSeconds);
    }

    private void Update()
    {
        /*
        // timer handling flames coreographies
        flameTimer += Time.deltaTime;
        spiralTimer += Time.deltaTime;

        if (flameTimer >= flameCooldown)
        {
            SpawnCloseFlames();
            flameTimer = 0f;
        }

        if (spiralTimer >= spiralCooldown)
        {
            StartCoroutine(SpawnSpiral());
            spiralTimer = 0f;
        }
        */

        if (currentPhase == DragonPhase.Phase1)
        {
            HandlePhase1();
        }
        else if (currentPhase == DragonPhase.Phase2)
        {
            HandlePhase2();
        }
    }

    private void FixedUpdate()
    {
        if (!makeImmovable || body == null)
        {
            return;
        }

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private void HandlePhase1()
    {
        if (Time.time >= nextRoarTime)
        {
            DoRoarKnockback();
            nextRoarTime = Time.time + Mathf.Max(0.01f, roarIntervalSeconds);
            AudioManager.instance.PlaySFX(AudioManager.instance.dash);

        }

        if (Time.time >= nextFlameWaveTime)
        {
            SpawnFlameSproutWave();
            nextFlameWaveTime = Time.time + Mathf.Max(0.01f, flameWaveIntervalSeconds);

        }
    }

    private void HandlePhase2()
    {
        if (!phase2Initialized)
        {
            phase2Initialized = true;
            StartCoroutine(SpawnBoulderWithWarning());
            // Give the boulder time to appear before the first barrage
            nextBarrageTime = Time.time + boulderWarningDelaySeconds + 1f;
        }

        if (!barrageRunning && Time.time >= nextBarrageTime)
        {
            StartCoroutine(BarrageCoroutine());
        }
    }

    private IEnumerator SpawnBoulderWithWarning()
    {
        if (boulderPrefab == null)
        {
            Debug.LogWarning($"{name}: No boulderPrefab assigned for Phase 2.", this);
            yield break;
        }

        if (spawnedBoulder != null)
        {
            Destroy(spawnedBoulder);
            spawnedBoulder = null;
        }

        Vector3 spawnPos = GetRandomBoulderSpawnPosition();
        spawnedBoulder = Instantiate(boulderPrefab, spawnPos, Quaternion.identity);

        var boulder = spawnedBoulder.GetComponent<PhaseBoulder>();
        if (boulder == null)
        {
            boulder = spawnedBoulder.AddComponent<PhaseBoulder>();
        }

        boulder.Initialize(boulderWarningDelaySeconds);
        yield break;
    }

    private IEnumerator BarrageCoroutine()
    {
        barrageRunning = true;

        float interval = 1f / Mathf.Max(0.1f, projectilesPerSecond);
        float endTime = Time.time + Mathf.Max(0.01f, barrageDurationSeconds);
        float nextShotTime = Time.time;

        while (Time.time < endTime)
        {
            // Catch up if this frame is late, so we preserve configured shots/sec.
            while (Time.time >= nextShotTime)
            {
                float angle = UnityEngine.Random.Range(barrageMinAngle, barrageMaxAngle);
                float rad = angle * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
                FireProjectile(dir);
                nextShotTime += interval;
            }

            yield return null;
        }

        if (spawnedBoulder != null)
        {
            Destroy(spawnedBoulder);
            spawnedBoulder = null;
        }

        // Spawn the next cover boulder at a new random location while cooldown runs.
        StartCoroutine(SpawnBoulderWithWarning());

        barrageRunning = false;
        nextBarrageTime = Time.time + barrageCooldownSeconds;
    }

    private Vector3 GetRandomBoulderSpawnPosition()
    {
        Vector2 center;
        Vector2 size;
        GetBoulderSpawnAreaRect(out center, out size);

        float halfX = size.x * 0.5f;
        float halfY = size.y * 0.5f;

        Vector2 boulderHalfExtents = GetBoulderHalfExtentsApprox();
        float pad = Mathf.Max(0f, boulderSpawnEdgePadding);

        float minX = center.x - halfX + boulderHalfExtents.x + pad;
        float maxX = center.x + halfX - boulderHalfExtents.x - pad;
        float minY = center.y - halfY + boulderHalfExtents.y + pad;
        float maxY = center.y + halfY - boulderHalfExtents.y - pad;

        // If area is too small after clamping, fall back to center on that axis.
        float xPos = minX <= maxX ? UnityEngine.Random.Range(minX, maxX) : center.x;
        float yPos = minY <= maxY ? UnityEngine.Random.Range(minY, maxY) : center.y;
        return new Vector3(xPos, yPos, 0f);
    }

    private void GetBoulderSpawnAreaRect(out Vector2 center, out Vector2 size)
    {
        center = boulderSpawnPosition;
        size = new Vector2(
            Mathf.Max(0.1f, boulderSpawnAreaSize.x),
            Mathf.Max(0.1f, boulderSpawnAreaSize.y));

        if (boulderSpawnAreaCenter == null)
        {
            return;
        }

        center = boulderSpawnAreaCenter.position;

        Renderer areaRenderer = boulderSpawnAreaCenter.GetComponent<Renderer>();
        if (areaRenderer == null)
        {
            areaRenderer = boulderSpawnAreaCenter.GetComponentInChildren<Renderer>();
        }

        if (areaRenderer != null)
        {
            Bounds b = areaRenderer.bounds;
            center = b.center;
            size = new Vector2(Mathf.Max(0.1f, b.size.x), Mathf.Max(0.1f, b.size.y));
        }
    }

    private Vector2 GetBoulderHalfExtentsApprox()
    {
        if (boulderPrefab == null)
        {
            return Vector2.zero;
        }

        SpriteRenderer prefabSr = boulderPrefab.GetComponentInChildren<SpriteRenderer>(true);
        if (prefabSr != null && prefabSr.sprite != null)
        {
            Vector2 spriteSize = prefabSr.sprite.bounds.size;
            Vector3 scale = prefabSr.transform.lossyScale;
            float width = Mathf.Abs(spriteSize.x * scale.x);
            float height = Mathf.Abs(spriteSize.y * scale.y);
            return new Vector2(width * 0.5f, height * 0.5f);
        }

        return Vector2.zero;
    }

    private void FireProjectile(Vector2 direction)
    {
        GameObject prefab = projectilePrefab;
        GameObject proj;
        Vector2 dir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        Vector3 spawnOrigin = projectileSpawnPoint != null
            ? projectileSpawnPoint.position
            : transform.TransformPoint(projectileSpawnLocalOffset);
        Vector3 spawnPos = spawnOrigin + (Vector3)(dir * Mathf.Max(0f, projectileSpawnOffset));

        if (prefab != null)
        {
            proj = Instantiate(prefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // Runtime fallback: plain circle
            proj = new GameObject("DragonProjectile");
            proj.transform.position = spawnPos;
            var rb = proj.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            var col = proj.AddComponent<CircleCollider2D>();
            col.radius = 0.2f;
        }

        var dp = proj.GetComponent<DragonProjectile>();
        if (dp == null)
        {
            dp = proj.AddComponent<DragonProjectile>();
        }

        // Ignore collision between this projectile and the dragon itself
        var dragonColliders = GetComponentsInChildren<Collider2D>();
        for (int i = 0; i < dragonColliders.Length; i++)
        {
            dp.IgnoreCollider(dragonColliders[i]);
        }

        dp.Initialize(direction, projectileSpeed, projectileDamage, projectileLifetime, playerLayerMask);

    }

    private void DoRoarKnockback()
    {
        CachePlayerRefs();

        if (playerController == null || playerTransform == null)
        {
            return;
        }

        if (spriteRenderer != null && roarSprite != null)
        {
            spriteRenderer.sprite = roarSprite;
            StartCoroutine(RestoreSpriteAfterRoar());
        }

        Vector2 away = ((Vector2)playerTransform.position - (Vector2)transform.position);
        if (away.sqrMagnitude < 0.0001f)
        {
            away = Vector2.right;
        }
        away.Normalize();

        float duration = Mathf.Max(0.01f, roarKnockbackDurationSeconds);
        float speed = Mathf.Max(0f, roarKnockbackDistance) / duration;
        Vector2 velocity = away * speed;

        playerController.ApplyKnockback(velocity, duration);

        if (logPhase1Actions)
        {
            Debug.Log($"{name}: Roar knockback applied. vel={velocity}, duration={duration}", this);
        }
    }

    private IEnumerator RestoreSpriteAfterRoar()
    {
        float visualDuration = Mathf.Max(0.01f, roarSpriteDurationSeconds);
        yield return new WaitForSeconds(visualDuration);

        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = defaultSprite;
        }
    }

    private void SpawnFlameSproutWave()
    {
        activeFlames.RemoveAll(f => f == null);
        if (maxActiveFlames > 0 && activeFlames.Count >= maxActiveFlames)
        {
            return;
        }

        CachePlayerRefs();
        if (playerTransform == null)
        {
            return;
        }

        int count = Mathf.Max(1, flameCountPerWave);

        Vector2 spawnCenter;
        Vector2 spawnSize;
        GetFlameSpawnAreaRect(out spawnCenter, out spawnSize);

        float pad = Mathf.Max(0f, flameSpawnEdgePadding);
        float halfX = Mathf.Max(0f, spawnSize.x * 0.5f - pad);
        float halfY = Mathf.Max(0f, spawnSize.y * 0.5f - pad);

        float minX = spawnCenter.x - halfX;
        float maxX = spawnCenter.x + halfX;
        float minY = spawnCenter.y - halfY;
        float maxY = spawnCenter.y + halfY;

        // One flame always targets the player's current position.
        SpawnOneFlame(new Vector3(
            Mathf.Clamp(playerTransform.position.x, minX, maxX),
            Mathf.Clamp(playerTransform.position.y, minY, maxY),
            0f));

        // Remaining flames spawn at random positions inside the arena.
        for (int i = 1; i < count; i++)
        {
            float x = minX <= maxX ? UnityEngine.Random.Range(minX, maxX) : spawnCenter.x;
            float y = minY <= maxY ? UnityEngine.Random.Range(minY, maxY) : spawnCenter.y;
            SpawnOneFlame(new Vector3(x, y, 0f));
        }
    }

    private void GetFlameSpawnAreaRect(out Vector2 center, out Vector2 size)
    {
        center = (flameAreaMin + flameAreaMax) * 0.5f;
        size = new Vector2(
            Mathf.Max(0.1f, Mathf.Abs(flameAreaMax.x - flameAreaMin.x)),
            Mathf.Max(0.1f, Mathf.Abs(flameAreaMax.y - flameAreaMin.y)));

        if (flameSpawnAreaCenter == null)
        {
            return;
        }

        center = flameSpawnAreaCenter.position;
        size = new Vector2(
            Mathf.Max(0.1f, flameSpawnAreaSize.x),
            Mathf.Max(0.1f, flameSpawnAreaSize.y));

        Renderer areaRenderer = flameSpawnAreaCenter.GetComponent<Renderer>();
        if (areaRenderer == null)
        {
            areaRenderer = flameSpawnAreaCenter.GetComponentInChildren<Renderer>();
        }

        if (areaRenderer != null)
        {
            Bounds b = areaRenderer.bounds;
            center = b.center;
            size = new Vector2(Mathf.Max(0.1f, b.size.x), Mathf.Max(0.1f, b.size.y));
        }
    }

    private void SpawnOneFlame(Vector3 spawnPos)
    {
        if (maxActiveFlames > 0 && activeFlames.Count >= maxActiveFlames)
        {
            return;
        }

        GameObject prefab = flameSproutPrefab ;
        if (prefab == null)
        {
            Debug.LogWarning($"{name}: No flame sprout prefab assigned (and flamePrefab is null).", this);
            return;
        }

        GameObject flameObject = Instantiate(prefab, spawnPos, Quaternion.identity);
        AudioManager.instance.PlaySFX(AudioManager.instance.fireball);

        if (flameObject == null)
        {
            return;
        }

        if (!flameObject.activeSelf)
        {
            flameObject.SetActive(true);
        }

        var hazard = flameObject.GetComponent<FlameSproutHazard>();
        if (hazard == null)
        {
            hazard = flameObject.AddComponent<FlameSproutHazard>();
        }

        hazard.Initialize(flameWarningDelaySeconds, flameDamageRadius, flameDamageAmount, flameLifetimeSeconds, playerLayerMask);
        activeFlames.Add(hazard);

        if (logPhase1Actions)
        {
            Debug.Log($"{name}: Spawned FlameSprout at {spawnPos}", this);
        }
    }

    private void ApplyLayerToHierarchy(GameObject root, int layer)
    {
        if (root == null)
        {
            return;
        }

        root.layer = layer;
        Transform t = root.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            Transform child = t.GetChild(i);
            if (child != null)
            {
                ApplyLayerToHierarchy(child.gameObject, layer);
            }
        }
    }


    private void CachePlayerRefs()
    {
        if (playerTransform == null && PlayerHealth.Instance != null)
        {
            playerTransform = PlayerHealth.Instance.transform;
        }

        if (playerController == null)
        {
            playerController = FindAnyObjectByType<PlayerController>();
        }
    }

    /*
    flames patterns
    
    public void SpawnCloseFlames()
    {
        StartCoroutine(SpawnCloseFlamesCoroutine());
    }

  
    private IEnumerator SpawnCloseFlamesCoroutine()
    {
        Vector3 center = centerPoint.position;
        SpriteRenderer sr = centerPoint.GetComponent<SpriteRenderer>();

        float halfWidth = sr.bounds.extents.x;
        float halfHeight = sr.bounds.extents.y;

        float margin = 1f;
        float flameSize = 1f;

        float minX = center.x - halfWidth - margin;
        float maxX = center.x + halfWidth + margin;
        float minY = center.y - halfHeight - margin;
        float maxY = center.y + halfHeight + margin;

        int stepsX = Mathf.CeilToInt((maxX - minX) / flameSize);
        int stepsY = Mathf.CeilToInt((maxY - minY) / flameSize);

        for (int ix = 0; ix <= stepsX; ix++)
        {
            for (int iy = 0; iy <= stepsY; iy++)
            {
                float x = minX + ix * flameSize;
                float y = minY + iy * flameSize;

                Vector3 spawnPos = new Vector3(x, y, 0f);

                GameObject flame = Instantiate(flamePrefab, spawnPos, Quaternion.identity);
                Destroy(flame, 4f);
            }
        }

        yield return null;
    }

    private IEnumerator SpawnSpiral()
    {
        float currentRadius = initialRadius;
        float currentAngle = 0f;

        for (int i = 0; i < flameCount; i++)
        {
            float rad = currentAngle * Mathf.Deg2Rad;

            Vector3 pos = centerPoint.position + new Vector3(
                Mathf.Cos(rad) * currentRadius,
                Mathf.Sin(rad) * currentRadius,
                0f
            );

            GameObject flame = Instantiate(flamePrefab, pos, Quaternion.identity);
            Destroy(flame, 1f);

            currentAngle -= angleStep;
            currentRadius += radiusStep;

            yield return new WaitForSecondsRealtime(spiralDelay);
        }
    }
    */

    public void TakeDamage(float damageAmount)
    {
        if (damageAmount <= 0f)
        {
            return;
        }

        if (currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - damageAmount);
        Debug.Log("current healt" + currentHealth + "damage taken:" + damageAmount);
        UpdatePhaseFromHealth();

        if (healthBar != null)
        {
            healthBar.value = currentHealth / maxHealth;
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    private void UpdatePhaseFromHealth()
    {
        float phase2Threshold = maxHealth * Mathf.Clamp01(phase2HealthPercent);
        float phase3Threshold = maxHealth * Mathf.Clamp01(phase3HealthPercent);

        DragonPhase newPhase = DragonPhase.Phase1;
        if (currentHealth <= phase3Threshold)
        {
            newPhase = DragonPhase.Phase3;
        }
        else if (currentHealth <= phase2Threshold)
        {
            newPhase = DragonPhase.Phase2;
        }

        if (newPhase == currentPhase)
        {
            return;
        }

        currentPhase = newPhase;
        onPhaseChanged?.Invoke(currentPhase);
        OnPhaseChanged?.Invoke(currentPhase);
    }

    private void Die()
    {
        GetComponent<DropTable>()?.SpawnDrops();
        GameManager.Victory();
        Destroy(gameObject);
        if (healthBar != null)
        {
            healthBar.gameObject.SetActive(false);
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
}
