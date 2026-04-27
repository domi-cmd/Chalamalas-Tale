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

    private PlayerState playerState;
    private float slideSpeed;
    private Vector2 slideDirection;
    

    private enum PlayerState
    {
        CantMove,
        Normal,
        DodgeRollSliding,
    }

    public enum PlayerFacingDirection
    {
        Up,
        Right,
        Down,
        Left
    }

    public PlayerFacingDirection CurrentFacing { get; private set; } = PlayerFacingDirection.Down;
    

    void Start()
    {
        body = GetComponentInChildren<Rigidbody2D>();
        spriteImage = GetComponentInChildren<SpriteRenderer>();
        spriteImage.sprite = spriteIdle;

        // Relevant for dodgeroll logic, state is "Normal" by default
        playerState = PlayerState.Normal;
    }

    void Update()
    {
        switch (playerState)
        {
            case(PlayerState.CantMove):
            // when the player has to be locked (during dialoge, etc.)
                movement = Vector2.zero;
                return;

            case(PlayerState.Normal):
                HandleBasicMovement();
                HandleDodgeRoll();  
                break;
            
            case(PlayerState.DodgeRollSliding):
                HandleDodgeRollSliding();
                break;
        }
    }

    private void HandleBasicMovement()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        body.linearVelocity = movement * speed;

        // Change sprite based on direction
        if (movement.x > 0)
        {
            spriteImage.sprite = spriteRight;
            CurrentFacing = PlayerFacingDirection.Right;
        }
        else if (movement.x < 0)
        {
            spriteImage.sprite = spriteLeft;
            CurrentFacing = PlayerFacingDirection.Left;
        }
        else if (movement.y > 0)
        {
            spriteImage.sprite = spriteBack;
            CurrentFacing = PlayerFacingDirection.Up;
        }
        else if (movement.y < 0)
        {
            spriteImage.sprite = spriteIdle;
            CurrentFacing = PlayerFacingDirection.Down;
        }
        else
        {
            return;
        }
    }

    private void HandleDodgeRoll()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            Debug.Log("Doing a dodge roll!");
            playerState = PlayerState.DodgeRollSliding;
            slideDirection = movement.normalized;
            slideSpeed = 15f;
        }
    }

    private void HandleDodgeRollSliding()
    {
        body.linearVelocity = slideDirection * slideSpeed;
        slideSpeed -= slideSpeed * 5f * Time.deltaTime;

        if(slideSpeed < 5f)
        {
            playerState = PlayerState.Normal;
            body.linearVelocity = Vector2.zero;
        } 
    }

    public void FreezePlayerMovement()
    {
        playerState = PlayerState.CantMove;
    }

    public void UnfreezePlayerMovement()
    {
        playerState = PlayerState.Normal;
    }
}