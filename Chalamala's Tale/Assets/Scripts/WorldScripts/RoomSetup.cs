using UnityEngine;

public class RoomSetup : MonoBehaviour
{
    void Start()
    {
        int r = GridManager.Instance.currentRow;
        int c = GridManager.Instance.currentCol;
        Debug.Log($"Loading room [{r},{c}]");

        GameObject.Find("DoorTop").SetActive(   GridManager.Instance.IsOpen(r, c, 0));
        GameObject.Find("DoorRight").SetActive( GridManager.Instance.IsOpen(r, c, 1));
        GameObject.Find("DoorBottom").SetActive(GridManager.Instance.IsOpen(r, c, 2));
        GameObject.Find("DoorLeft").SetActive(  GridManager.Instance.IsOpen(r, c, 3));

        FindFirstObjectByType<Minimap>()?.Draw(); // redraw minimap with current room highlighted
    }
}