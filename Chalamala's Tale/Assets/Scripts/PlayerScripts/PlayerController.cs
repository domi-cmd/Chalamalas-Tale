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
    public Canvas info;

    void Start()
    {
        body = GetComponentInChildren<Rigidbody2D>();
        spriteImage = GetComponentInChildren<SpriteRenderer>();
    }

    void Update()
    {
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
        else
        {
            spriteImage.sprite = spriteIdle;
        }
    }

    void FixedUpdate()
    {
        body.linearVelocity = movement * speed;
    }


}