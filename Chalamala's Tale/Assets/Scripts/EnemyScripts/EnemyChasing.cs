using UnityEngine;

public class EnemyChasing : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 3f;
    private float currentHealth;

    // Reference to the player object, as to be able to calculate the distance to it and how to move towards it, etc.
    GameObject player;

    // Radius for the area in which the enemy is aggro'd by the player
    public float aggroRangeRadius = 5f;
    // Field for the (possibly) visible aggro range of the enemy
    private GameObject aggroRange;
    // Flag for debugging that shows enemy aggro range. Should be removed for release!
    public bool showAggroRange = false;
    // Fields for enemy speed, movement direction and collision detection
    public float speed = 1.5f;
    private Vector2 movement;
    private Rigidbody2D body;

    // Unused field, for adding enemy sprite later on :)
    public SpriteRenderer spriteImage;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        currentHealth = maxHealth;

        // Get the player object
        player = GameObject.Find("Player");

        body = GetComponent<Rigidbody2D>();
        // No sprite yet :)
        // spriteImage = GetComponent<SpriteRenderer>();
        
        // Instantiate the aggro range object based on the decided range radius
        CreateAggroRange();

        // Set the hp to max at spawn
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        // Show enemy aggro range if flag is turned on
        if(showAggroRange) this.aggroRange.GetComponent<MeshRenderer>().enabled = true;
        else this.aggroRange.GetComponent<MeshRenderer>().enabled = false;

        // Calculate whether the player is within the enemy's aggro range
        float distanceToPlayer = Vector3.Distance(transform.position, player.transform.position); 

        // If player is in aggro range, charge at the player
        if(distanceToPlayer <= aggroRangeRadius){
            // Calculate vector in direction to player
            movement = (player.transform.position - transform.position).normalized;
            body.linearVelocity = movement * speed;
        } else {
            body.linearVelocity = Vector2.zero;
            // If player is not in aggro range, have idle walking script (still missing)
            // TODO: Idle walking script
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
    private void CreateAggroRange(){
        this.aggroRange = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        this.aggroRange.transform.SetParent(transform);
        this.aggroRange.transform.localPosition = Vector3.zero;
        this.aggroRange.transform.localScale = Vector3.one * (aggroRangeRadius * 2);

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
        this.aggroRange.GetComponent<MeshRenderer>().material = mat;
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
}
