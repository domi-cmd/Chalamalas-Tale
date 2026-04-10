using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    public Transform spawnTop;
    public Transform spawnRight;
    public Transform spawnBottom;
    public Transform spawnLeft;

    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");

        int entry = PlayerSpawnData.entrySide;

        // Spawn at opposite side
        switch (entry)
        {
            case 0: // came from top → spawn bottom
                player.transform.position = spawnBottom.position;
                break;
            case 1: // came from right → spawn left
                player.transform.position = spawnLeft.position;
                break;
            case 2: // came from bottom → spawn top
                player.transform.position = spawnTop.position;
                break;
            case 3: // came from left → spawn right
                player.transform.position = spawnRight.position;
                break;
        }
    }
}