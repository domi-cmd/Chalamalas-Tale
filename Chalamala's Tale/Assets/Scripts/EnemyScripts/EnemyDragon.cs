using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class EnemyDragon : MonoBehaviour, IDamageable
{
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

    [Header("Phase 1: Falling Rocks")]
    [Tooltip("Optional prefab to spawn. If omitted, a simple invisible 2D physics rock is created at runtime.")]
    [SerializeField] private GameObject fallingRockPrefab;
    [Tooltip("Optional: define the arena bounds for rock spawn positions. Rocks spawn at the TOP (ceiling) of this box.")]
    [SerializeField] private BoxCollider2D rockSpawnArea;
    [Tooltip("Used if Rock Spawn Area is not set.")]
    [SerializeField] private Vector2 fallbackSpawnAreaSize = new Vector2(16f, 10f);
    [SerializeField] private float rockSpawnIntervalSeconds = 1.25f;
    [SerializeField] private float rockWarningDelaySeconds = 2f;
    [SerializeField] private float rockFallSpeed = 10f;
    [SerializeField] private float rockDamageRadius = 1.5f;
    [SerializeField] private float rockDamageAmount = 1f;
    [SerializeField] private LayerMask playerLayerMask;
    [SerializeField] private int maxActiveRocks = 12;

    [Header("Debug")]
    [SerializeField] private bool logPhase1Actions = false;

    private DragonPhase currentPhase = DragonPhase.Phase1;
    private Rigidbody2D body;

    private PlayerController playerController;
    private Transform playerTransform;

    private float nextRoarTime;
    private float nextRockSpawnTime;
    private readonly List<FallingRock> activeRocks = new List<FallingRock>();

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public DragonPhase CurrentPhase => currentPhase;

    public event Action<DragonPhase> OnPhaseChanged;

    private void Awake()
    {
        if (maxHealth <= 0f)
        {
            maxHealth = 100f;
        }

        currentHealth = maxHealth;
        currentPhase = DragonPhase.Phase1;

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
        nextRockSpawnTime = Time.time + Mathf.Max(0.01f, rockSpawnIntervalSeconds);
    }

    private void Update()
    {
        if (currentPhase == DragonPhase.Phase1)
        {
            HandlePhase1();
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
        }

        if (Time.time >= nextRockSpawnTime)
        {
            SpawnFallingRock();
            nextRockSpawnTime = Time.time + Mathf.Max(0.01f, rockSpawnIntervalSeconds);
        }
    }

    private void DoRoarKnockback()
    {
        CachePlayerRefs();

        if (playerController == null || playerTransform == null)
        {
            return;
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

    private void SpawnFallingRock()
    {
        activeRocks.RemoveAll(r => r == null);
        if (maxActiveRocks > 0 && activeRocks.Count >= maxActiveRocks)
        {
            return;
        }

        Bounds bounds = GetRockSpawnBounds();
        float x = UnityEngine.Random.Range(bounds.min.x, bounds.max.x);
        float y = bounds.max.y;
        Vector3 spawnPos = new Vector3(x, y, 0f);

        GameObject rockObject;
        if (fallingRockPrefab != null)
        {
            rockObject = Instantiate(fallingRockPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            rockObject = CreateRuntimeRock(spawnPos);
        }

        if (rockObject == null)
        {
            return;
        }

        // In case the prefab asset was saved inactive.
        if (!rockObject.activeSelf)
        {
            rockObject.SetActive(true);
        }

        ApplyLayerToHierarchy(rockObject, gameObject.layer);

        var rock = rockObject.GetComponent<FallingRock>();
        if (rock == null)
        {
            rock = rockObject.AddComponent<FallingRock>();
        }

        rock.Initialize(rockWarningDelaySeconds, rockFallSpeed, rockDamageRadius, rockDamageAmount, playerLayerMask);
        activeRocks.Add(rock);

        if (logPhase1Actions)
        {
            Debug.Log($"{name}: Spawned FallingRock at {spawnPos}", this);
        }

        IgnoreRockCollisionWithSelf(rockObject);
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

    private Bounds GetRockSpawnBounds()
    {
        if (rockSpawnArea != null)
        {
            return rockSpawnArea.bounds;
        }

        Vector3 center = transform.position;
        Vector3 size = new Vector3(Mathf.Max(0.1f, fallbackSpawnAreaSize.x), Mathf.Max(0.1f, fallbackSpawnAreaSize.y), 0f);
        return new Bounds(center, size);
    }

    private GameObject CreateRuntimeRock(Vector3 spawnPosition)
    {
        Debug.LogWarning($"{name}: Falling Rock Prefab is not set. Creating a simple runtime rock (no visuals).", this);

        GameObject rock = new GameObject("FallingRock");
        rock.transform.position = spawnPosition;
        rock.transform.rotation = Quaternion.identity;

        var rb = rock.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var circle = rock.AddComponent<CircleCollider2D>();
        circle.radius = 0.4f;

        return rock;
    }

    private void IgnoreRockCollisionWithSelf(GameObject rockObject)
    {
        if (rockObject == null)
        {
            return;
        }

        var rockColliders = rockObject.GetComponentsInChildren<Collider2D>();
        var myColliders = GetComponentsInChildren<Collider2D>();

        for (int r = 0; r < rockColliders.Length; r++)
        {
            if (rockColliders[r] == null) continue;
            for (int m = 0; m < myColliders.Length; m++)
            {
                if (myColliders[m] == null) continue;
                Physics2D.IgnoreCollision(rockColliders[r], myColliders[m]);
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
        UpdatePhaseFromHealth();

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
        Destroy(gameObject);
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
