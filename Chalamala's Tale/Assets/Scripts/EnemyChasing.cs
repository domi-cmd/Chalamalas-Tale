using UnityEngine;

public class EnemyChasing : MonoBehaviour
{
    //
    GameObject player;

    // Radius for the area in which the enemy is aggro'd by the player
    private float aggroRange = 5f;
    // Flag for whether player is aggro'd or in idle mode
    private bool isAggro = false;

    // Fields for enemy speed, movement direction and collision detection
    private float speed = 3f;
    private Vector2 movement;
    private Rigidbody2D body;

    // Unused field, for adding enemy sprite later on :)
    public SpriteRenderer spriteImage;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Get the player object
        player = GameObject.Find("PlayerHitbox");

        body = GetComponent<Rigidbody2D>();
        // No sprite yet :)
        // spriteImage = GetComponent<SpriteRenderer>();
        
    }

    // Update is called once per frame
    void Update()
    {
        // Calculate whether the player is within the enemy's aggro range
        //Debug.Log("Player Position: X = " + player.transform.position.x + " --- Y = " + player.transform.position.y + " --- Z = " + 
        //player.transform.position.z);
        Vector3 playerCurrentPosition = player.transform.position;
        Debug.Log(playerCurrentPosition);

        // If player is in aggro range, charge at the player

        // If player is not in aggro range, have idle walking script

    }
}
