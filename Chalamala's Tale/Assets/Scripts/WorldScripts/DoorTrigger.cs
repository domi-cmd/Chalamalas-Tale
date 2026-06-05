using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    // Set in Inspector: 0=top, 1=right, 2=bottom, 3=left
    public int side;

    // Sprite to show when all enemies are gone
    [Header("Door Visuals")]
    public SpriteRenderer doorSpriteRenderer;
    public Sprite unlockedSprite;

    private bool unlocked = false;

    void Update()
    {
        // Only check until unlocked
        if (unlocked) return;

        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");

        if (enemies.Length == 0)
        {
            unlocked = true;

            // Change sprite if assigned
            if (doorSpriteRenderer != null && unlockedSprite != null)
            {
                doorSpriteRenderer.sprite = unlockedSprite;
                AudioManager.instance.PlaySFX(AudioManager.instance.door);
            }
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        // Prevent entering while enemies still exist
        if (!unlocked) return;

        GridManager.Instance.MoveToRoom(side);
    }
}