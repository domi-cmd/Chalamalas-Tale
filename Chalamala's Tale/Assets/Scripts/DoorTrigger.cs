using UnityEngine;

public class DoorTrigger : MonoBehaviour
{
    // Set in Inspector: 0=top, 1=right, 2=bottom, 3=left
    public int side;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        GridManager.Instance.MoveToRoom(side);
    }
}