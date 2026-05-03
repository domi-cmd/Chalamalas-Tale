using UnityEngine;
using System.Collections;

public class FlameDamage : MonoBehaviour
{
    [SerializeField] private float damagePerTick = 0.5f;
    [SerializeField] private float damageCooldown = 2f;

    private Coroutine damageRoutine;

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null) return;

        damageRoutine = StartCoroutine(DealDamage(player));
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        PlayerHealth player = other.GetComponentInParent<PlayerHealth>();
        if (player == null) return;

        if (damageRoutine != null)
        {
            StopCoroutine(damageRoutine);
            damageRoutine = null;
        }
    }

    private IEnumerator DealDamage(PlayerHealth player)
    {
        while (true)
        {
            player.TakeDamage(damagePerTick); 
            yield return new WaitForSeconds(damageCooldown);
        }
    }
}