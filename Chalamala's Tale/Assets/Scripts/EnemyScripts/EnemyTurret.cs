using UnityEngine;

public class EnemyTurret : MonoBehaviour, IDamageable
{
    private enum ShotPattern
    {
        Cardinal,
        Diagonal
    }

    [Header("Health")]
    [SerializeField] private float maxHealth = 3f;
    private float currentHealth;

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float fireIntervalSeconds = 1.25f;
    [SerializeField] private float initialDelaySeconds = 0f;
    [SerializeField] private float projectileLifetimeSeconds = 4f;
    [SerializeField] private float projectileSpawnOffset = 0.25f;
    [Tooltip("Start with cardinal (up/down/left/right). If false, starts with diagonals.")]
    [SerializeField] private bool startWithCardinal = true;

    private float nextFireTime;
    private ShotPattern currentPattern;

    private void Awake()
    {
        currentHealth = maxHealth;

        if (firePoint == null)
        {
            firePoint = transform;
        }

        currentPattern = startWithCardinal ? ShotPattern.Cardinal : ShotPattern.Diagonal;
    }

    private void Start()
    {
#if UNITY_EDITOR
        if (projectilePrefab != null && projectilePrefab.scene.IsValid())
        {
            projectilePrefab.SetActive(false);
            Debug.LogWarning(
                $"{name}: 'Projectile Prefab' is a scene object. Make it a prefab asset so you don't need one in the scene.",
                this);
        }
#endif

        nextFireTime = Time.time + Mathf.Max(0f, initialDelaySeconds);
    }

    private void Update()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireIntervalSeconds;
        FireBurst(currentPattern);
        currentPattern = currentPattern == ShotPattern.Cardinal ? ShotPattern.Diagonal : ShotPattern.Cardinal;
    }
    void OnEnable()
    {
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
    private void FireBurst(ShotPattern pattern)
    {
        Vector2[] directions = pattern == ShotPattern.Cardinal
            ? new[] { Vector2.right, Vector2.left, Vector2.up, Vector2.down }
            : new[]
            {
                new Vector2(1f, 1f).normalized,
                new Vector2(-1f, 1f).normalized,
                new Vector2(1f, -1f).normalized,
                new Vector2(-1f, -1f).normalized
            };

        Vector2 origin = firePoint.position;
        var myColliders = GetComponentsInChildren<Collider2D>();

        for (int i = 0; i < directions.Length; i++)
        {
            Vector2 dir = directions[i];
            Vector2 spawnPosition = origin + (dir * projectileSpawnOffset);
            Quaternion rotation = Quaternion.FromToRotation(Vector3.right, dir);

            GameObject projectile = Instantiate(projectilePrefab, spawnPosition, rotation);

            var projectileCollider = projectile.GetComponent<Collider2D>();
            if (projectileCollider != null)
            {
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
                projectileBody.linearVelocity = dir * projectileSpeed;
            }

            if (projectileLifetimeSeconds > 0f)
            {
                Destroy(projectile, projectileLifetimeSeconds);
            }
        }
    }

    public void TakeDamage(float damageAmount)
    {
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
}
