using UnityEngine;

public class EnemyArcher : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 3f;
    private float currentHealth;

    [Header("Targeting")]
    [SerializeField] private string playerObjectName = "Player";
    private GameObject player;

    [Header("Ranges")]
    [SerializeField] private float aggroRangeRadius = 6f;
    [SerializeField] private float shootingRangeRadius = 4.5f;
    [Tooltip("If the player is closer than this (while the archer is in shooting range), the archer backs away.")]
    [SerializeField] private float retreatTriggerRadius = 2.5f;
    [SerializeField] private bool showAggroRange = false;
    private GameObject aggroRange;

    [Header("Movement")]
    [SerializeField] private float approachSpeed = 1.1f;
    [SerializeField] private float retreatSpeed = 0.75f;

    [Header("Shooting")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float projectileSpeed = 6f;
    [SerializeField] private float fireCooldownSeconds = 1.25f;
    [SerializeField] private float projectileLifetimeSeconds = 4f;
    [SerializeField] private float projectileSpawnOffset = 0.25f;

    private Rigidbody2D body;
    private float nextFireTime;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        currentHealth = maxHealth;
        player = GameObject.Find(playerObjectName);
        body = GetComponent<Rigidbody2D>();

#if UNITY_EDITOR
        if (projectilePrefab != null && projectilePrefab.scene.IsValid())
        {
            projectilePrefab.SetActive(false);
            Debug.LogWarning(
                $"{name}: 'Projectile Prefab' is a scene object. Make it a prefab asset so you don't need an arrow in the scene.",
                this);
        }
#endif

        if (firePoint == null)
        {
            firePoint = transform;
        }

        CreateAggroRange();
    }

    // Update is called once per frame
    private void Update()
    {
        if (aggroRange != null)
        {
            var meshRenderer = aggroRange.GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                meshRenderer.enabled = showAggroRange;
            }
        }

        if (player == null)
        {
            body.linearVelocity = Vector2.zero;
            return;
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);

        if (distanceToPlayer <= aggroRangeRadius)
        {
            MoveWithArcherLogic(distanceToPlayer);

            if (distanceToPlayer <= shootingRangeRadius)
            {
                TryShootAtPlayer();
            }
        }
        else
        {
            body.linearVelocity = Vector2.zero;
        }
    }

    private void MoveWithArcherLogic(float distanceToPlayer)
    {
        // Outside shooting range: close the distance.
        if (distanceToPlayer > shootingRangeRadius)
        {
            ApproachPlayer();
            return;
        }

        // Inside shooting range: only retreat if the player gets too close.
        if (distanceToPlayer < retreatTriggerRadius)
        {
            RetreatFromPlayer();
            return;
        }

        // In shooting range and not being pushed: hold position.
        body.linearVelocity = Vector2.zero;
    }

    private void ApproachPlayer()
    {
        Vector2 toPlayerDirection = ((Vector2)player.transform.position - (Vector2)transform.position).normalized;
        body.linearVelocity = toPlayerDirection * approachSpeed;
    }

    private void RetreatFromPlayer()
    {
        Vector2 awayDirection = ((Vector2)transform.position - (Vector2)player.transform.position).normalized;
        body.linearVelocity = awayDirection * retreatSpeed;
    }

    private void TryShootAtPlayer()
    {
        if (projectilePrefab == null)
        {
            return;
        }

        if (Time.time < nextFireTime)
        {
            return;
        }

        nextFireTime = Time.time + fireCooldownSeconds;

        Vector2 fireOrigin = firePoint.position;
        Vector2 directionToPlayer = ((Vector2)player.transform.position - fireOrigin).normalized;
        Vector2 spawnPosition = fireOrigin + (directionToPlayer * projectileSpawnOffset);

        Quaternion rotation = Quaternion.FromToRotation(Vector3.right, directionToPlayer);
        GameObject projectile = Instantiate(projectilePrefab, spawnPosition, rotation);

        var projectileCollider = projectile.GetComponent<Collider2D>();
        if (projectileCollider != null)
        {
            var myColliders = GetComponentsInChildren<Collider2D>();
            for (int i = 0; i < myColliders.Length; i++)
            {
                if (myColliders[i] != null)
                {
                    Physics2D.IgnoreCollision(projectileCollider, myColliders[i]);
                }
            }
        }

        var projectileBody = projectile.GetComponent<Rigidbody2D>();
        if (projectileBody != null)
        {
            projectileBody.linearVelocity = directionToPlayer * projectileSpeed;
        }

        if (projectileLifetimeSeconds > 0f)
        {
            Destroy(projectile, projectileLifetimeSeconds);
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

    // Helper method that sets up the aggro range centered on the object (enemy) that this script is attached to.
    private void CreateAggroRange()
    {
        aggroRange = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        aggroRange.transform.SetParent(transform);
        aggroRange.transform.localPosition = Vector3.zero;
        aggroRange.transform.localScale = Vector3.one * (aggroRangeRadius * 2);

        var collider3D = aggroRange.GetComponent<Collider>();
        if (collider3D != null)
        {
            Destroy(collider3D);
        }

        // Code below just sets up the material of the aggro range object, as to make it somewhat transparent.
        // This could be done way easier in the unity inspector and editor, but that doesnt work with the dynamic approach here.
        // Grab the existing material instead of creating a new one
        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
        mat.SetOverrideTag("RenderType", "Transparent");
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.renderQueue = 3000;
        mat.color = new Color(1f, 0f, 0f, 0.3f); // red, 30% opacity
        aggroRange.GetComponent<MeshRenderer>().material = mat;
    }
}
