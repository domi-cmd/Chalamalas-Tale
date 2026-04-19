using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;

    private Rigidbody2D body;
    private Vector2 movement;

    public SpriteRenderer spriteImage;

    public Sprite spriteRight;
    public Sprite spriteLeft;
    public Sprite spriteIdle;
    public Sprite spriteBack;

    public bool canMove = true; // to stop player actions when the scene is paused (menu, dialogues)
    

    void Start()
    {
        body = GetComponentInChildren<Rigidbody2D>();
        spriteImage = GetComponentInChildren<SpriteRenderer>();
        spriteImage.sprite = spriteIdle;
    }

    void Update()
    {

        if (!canMove) // when the player has to be locked
        {
            movement = Vector2.zero;
            return;
        }
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        // Change sprite based on direction
        if (movement.x > 0)
        {
            spriteImage.sprite = spriteRight;
        }
        else if (movement.x < 0)
        {
            spriteImage.sprite = spriteLeft;
        }
        else if (movement.y > 0)
        {
            spriteImage.sprite = spriteBack;
        }
        else if (movement.y < 0)
        {
            spriteImage.sprite = spriteIdle;
        }
        else
        {
            return;
        }
    }

    void FixedUpdate()
    {
        body.linearVelocity = movement * speed;
    }


}