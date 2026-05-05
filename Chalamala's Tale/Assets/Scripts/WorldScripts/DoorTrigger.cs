using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    // Set in Inspector: 0=top, 1=right, 2=bottom, 3=left
    public int side;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        // Check if any enemies exist in the scene
        GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
        Debug.Log("Enemies found: " + enemies.Length);
        if (enemies.Length > 0)
        {
            // There are still enemies, do nothing
            return;
        }
        else
        {
            
        GridManager.Instance.MoveToRoom(side);}
    }
}