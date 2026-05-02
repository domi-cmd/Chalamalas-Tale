using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using System.Collections;  // for different coroutines
using UnityEngine.UI; // for healthbar

/*
class for the dragon boss ennemy:
- set up: put maximal health
- take damage: only when there are no flames around the dragon
- boss fight divided in 3 phases (the change of phase is induced by the current health of the dragon)
- attack: ATM the dragon doesn't attack directly but using two prefabs:
    - flames (different patterns generated throught the battle)
    - falling rocks
*/

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

    /*
    healt:
    maximal health of 100 hp set at the beginning,
    shown in the UI slider health bar
    */
    [Header("Boss Health")]
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float currentHealth;

    [Header("UI")]
    [SerializeField] private Slider healthBar;

    /*
    flames spawned later in the battle and relative variables
    */
    public GameObject flamePrefab; 
     // values for different patterns
    public Transform centerPoint;
    public float initialRadius = 0.5f;
    public float radiusStep = 0.1f;
    public float angleStep = 10f;
    public int flameCount = 70;
    public float spiralDelay = 0.01f;

    // to have the flames appear each 7 seconds
    private float flameTimer = 0f;
    public float flameCooldown = 4f;


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
        healthBar.value = 1f;   // as the UI values are [0,1], we work with fractions of health to be shown
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
        /*
        timer handling flames coreographies
        */

        flameTimer += Time.deltaTime;

        if (flameTimer >= flameCooldown)
        {
            SpawnCloseFlames();
            flameTimer = 0f;
        }

        // handler of phase 1
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


    /*
    flames patterns
    */
    public void SpawnCloseFlames()
    {
        StartCoroutine(SpawnCloseFlamesCoroutine());
    }

    // for flames contourning the boss
    private IEnumerator SpawnCloseFlamesCoroutine(){
        Vector3 center = centerPoint.position ;
        // Dragon size to define the borders of the rectangle
        SpriteRenderer sr = centerPoint.GetComponent<SpriteRenderer>();

        float halfWidth = sr.bounds.extents.x;
        float halfHeight = sr.bounds.extents.y;


        // Small margin so flames are "around" the dragon, not inside it
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
                /* uncomment for empty rectangle
                bool isBorder =
                    ix == 0 || ix == stepsX ||
                    iy == 0 || iy == stepsY;

                if (!isBorder)
                    continue;
                */

                float x = minX + ix * flameSize;
                float y = minY + iy * flameSize;

                Vector3 spawnPos = new Vector3(x, y, 0f);

                GameObject flame = Instantiate(flamePrefab, spawnPos, Quaternion.identity);
                Destroy(flame, 4f);
            }
        }
        yield return null;
    }

    // to spawn a spiral of flames from the dragon center
    IEnumerator SpawnSpiral()
    {
        float currentRadius = initialRadius;
        float currentAngle = 0f;

        for (int i = 0; i < flameCount; i++)
        {
            Debug.Log($"flame:{i}");
            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 pos = centerPoint.position + new Vector3(
                Mathf.Cos(rad) * currentRadius,
                Mathf.Sin(rad) * currentRadius,
                0f
            );

            // Instantiate a true prefab clone
            Instantiate(flamePrefab, pos, Quaternion.identity);

            currentAngle -= angleStep;
            currentRadius += radiusStep;
            //radiusStep = radiusStep - 0.01;

            yield return new WaitForSecondsRealtime(spiralDelay);
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
        Debug.Log("current healt" + currentHealth + "damage taken:" + damageAmount);
        UpdatePhaseFromHealth();

        // Update health bar
        healthBar.value = currentHealth / maxHealth;

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
        healthBar.gameObject.SetActive(false);
    }


    //sound change
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