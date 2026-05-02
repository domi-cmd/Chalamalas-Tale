using UnityEngine;

public class FlameDamage : MonoBehaviour
{
    private float damageCooldown = 0.5f; // time between hits
    private float timer = 0f;

    void OnTriggerStay2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();

        if (player == null) return;

        timer += Time.deltaTime;

        if (timer >= damageCooldown)
        {
            player.TakeDamage(1f); 
            timer = 0f;
        }
    }
}