using UnityEngine;

public class RoomSetup : MonoBehaviour
{
    void Start()
    {
        int r = GridManager.Instance.currentRow;
        int c = GridManager.Instance.currentCol;
        Debug.Log($"Loading room [{r},{c}]");

        // Reposition player based on which side they entered from (Access the hitbox object, not the empty player parent)
        var player = PlayerHealth.Instance.transform;
        switch(GridManager.Instance.enteredFromSide)
        {
            case 0: player.transform.position = new Vector3(0.02f, -2.8f, 0); break; // entered from top, spawn at bottom
            case 1: player.transform.position = new Vector3(-7.21f, 0.51f, 0); break; // entered from right, spawn at left
            case 2: player.transform.position = new Vector3(0.17f, 3.47f, 0); break; // entered from bottom, spawn at top
            case 3: player.transform.position = new Vector3(6.76f, 0.46f, 0); break; // entered from left, spawn at right
        }
        Debug.Log($"Player spawned at: {player.transform.position}");

        GameObject.Find("DoorTop").SetActive(   GridManager.Instance.IsOpen(r, c, 0));
        GameObject.Find("DoorRight").SetActive( GridManager.Instance.IsOpen(r, c, 1));
        GameObject.Find("DoorBottom").SetActive(GridManager.Instance.IsOpen(r, c, 2));
        GameObject.Find("DoorLeft").SetActive(  GridManager.Instance.IsOpen(r, c, 3));

        FindFirstObjectByType<Minimap>()?.Draw(); // redraw minimap with current room highlighted
    }
}