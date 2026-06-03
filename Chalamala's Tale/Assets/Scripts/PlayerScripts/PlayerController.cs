using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float speed = 5f;

    private Animator animator;
    private Vector2 lastMoveDirection = Vector2.down;
    [Header("Combat")]
    [SerializeField] private int baseAttackDamage = 3;

    private Rigidbody2D body;
    private Vector2 movement;

    public SpriteRenderer spriteImage;


    // to stop player actions when the scene is paused (menu, dialogues)
    public bool canMove = true; 

    private PlayerState playerState;
    private float slideSpeed;
    private Vector2 slideDirection;

    private float knockbackEndTime;
    private Vector2 knockbackVelocity;

    // Flag to check whether the player already has the ranged attack unlocked
    private bool rangedAttackEnabled = false;
    

    private enum PlayerState
    {
        CantMove,
        Normal,
        DodgeRollSliding,
        Knockback,
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
        animator = GetComponentInChildren<Animator>();

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

            case (PlayerState.Knockback):
                HandleKnockback();
                break;
        }
    }

    private void HandleKnockback()
    {
        body.linearVelocity = knockbackVelocity;

        if (Time.time >= knockbackEndTime)
        {
            knockbackVelocity = Vector2.zero;
            playerState = PlayerState.Normal;
            body.linearVelocity = Vector2.zero;
        }
    }

    private void HandleBasicMovement(){
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        movement.Normalize();

        body.linearVelocity = movement * speed;

        if (movement != Vector2.zero)
        {
            lastMoveDirection = movement;

            // Determine facing direction
            if (Mathf.Abs(movement.x) > Mathf.Abs(movement.y))
            {
                CurrentFacing = movement.x > 0
                    ? PlayerFacingDirection.Right
                    : PlayerFacingDirection.Left;
            }
            else
            {
                CurrentFacing = movement.y > 0
                    ? PlayerFacingDirection.Up
                    : PlayerFacingDirection.Down;
            }
        }
        animator.SetFloat("MoveX", lastMoveDirection.x);
        animator.SetFloat("MoveY", lastMoveDirection.y);
        animator.SetBool("IsMoving", movement != Vector2.zero);
        //Debug.Log($"MoveX: {movement.x} MoveY: {movement.y}");
    }

    private void HandleDodgeRoll()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("Doing a dodge roll!");
            playerState = PlayerState.DodgeRollSliding;
            slideDirection = movement.normalized;
            slideSpeed = 15f;
        }
    }
    public void PlayAttackAnimation()
    {
        Vector2 facing = GetFacingVector();

        animator.SetFloat("AttackX", facing.x);
        animator.SetFloat("AttackY", facing.y);
        animator.SetTrigger("Attack");
    }
    private Vector2 GetFacingVector()
    {
        switch (CurrentFacing)
        {
            case PlayerFacingDirection.Up:
                return Vector2.up;
            case PlayerFacingDirection.Down:
                return Vector2.down;
            case PlayerFacingDirection.Left:
                return Vector2.left;
            case PlayerFacingDirection.Right:
                return Vector2.right;
        }
        return Vector2.down;
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

    public void ApplyKnockback(Vector2 velocity, float durationSeconds)
    {
        if (body == null)
        {
            body = GetComponentInChildren<Rigidbody2D>();
        }

        if (durationSeconds <= 0f)
        {
            return;
        }

        knockbackVelocity = velocity;
        knockbackEndTime = Time.time + durationSeconds;
        playerState = PlayerState.Knockback;
    }

    public void EnablePlayerRangedAttack()
    {
        rangedAttackEnabled = true;
    }

    public bool HasRangedAttack()
    {
        return rangedAttackEnabled;
    }

    public int GetAttackDamage()
    {
        return Mathf.Max(0, baseAttackDamage);
    }

    public void IncreaseAttackDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        baseAttackDamage += amount;
    }
}