using UnityEngine;

public class SimpleRoomSetup : MonoBehaviour
{
    // connect manually the doors gameobjects in Unity 
    public GameObject doorTop;
    public GameObject doorBottom;
    void Start()
    {
        Debug.Log("PlayerHealth: " + PlayerHealth.Instance);
        Debug.Log("GridManager: " + BasicGridManager.Instance);
        var gm = BasicGridManager.Instance;

        int r = gm.currentRow;
        int c = gm.currentCol;

        Debug.Log($"Loading room [{r},{c}]");

        var player = PlayerHealth.Instance.transform;

        // Spawn player based on entry side
        if (gm.enteredFromSide != -1)
        {
            switch(gm.enteredFromSide)
            {
                case 0: player.position = new Vector3(0.02f, -2.8f, 0); break;
                case 1: player.position = new Vector3(-7.21f, 0.51f, 0); break;
                case 2: player.position = new Vector3(0.17f, 3.47f, 0); break;
                case 3: player.position = new Vector3(6.76f, 0.46f, 0); break;
            }
        }

        // Enable/disable doors that exists in the scene
        if (doorTop != null)
            doorTop.SetActive(gm.IsOpen(r, c, 0));

        if (doorBottom != null)
            doorBottom.SetActive(gm.IsOpen(r, c, 2));
            }
}