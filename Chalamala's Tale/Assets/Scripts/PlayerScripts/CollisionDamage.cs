using UnityEngine;
using UnityEngine.UI;

public class CollisionDamage : MonoBehaviour
{
   private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponentInParent<PlayerHealth>().TakeDamage(1);
        }
    }
   
}