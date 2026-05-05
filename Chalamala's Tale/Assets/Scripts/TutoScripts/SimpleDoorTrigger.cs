using UnityEngine;
using UnityEngine.SceneManagement;

public class SimpleDoorTrigger : MonoBehaviour
{
    // 0=top, 1=right, 2=bottom, 3=left
    public int side;


    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

                // Save which door we used
        PlayerSpawnData.entrySide = side;


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
            

        // to correctly update the map, we change room accordingly to its logic
        BasicGridManager.Instance.MoveToRoom(side);
        }



    }

}
    public static class PlayerSpawnData
{
    public static int entrySide = -1;
}