using UnityEngine;
using UnityEngine.UI;

public class Minimap : MonoBehaviour
{
    public int cellSize = 12;
    public int wallThickness = 2;
    public Color roomColor = new Color(0.8f, 0.8f, 0.8f);
    public Color currentRoomColor = Color.yellow;
    public Color wallColor = Color.black;
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.1f);
    // For storing the minimap rectangles
    private RectTransform[,] roomRects;
    // Icon fields for both player and boss
    public Sprite playerIcon;
    public Sprite bossIcon;


    void Start() => Draw();

    public void Draw()
    {
        // Clear old minimap
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        int rows = 4, cols = 4;
        var gm = GridManager.Instance;

        // Initialize the room rectangle that stores the minimap squares
        roomRects = new RectTransform[rows, cols];

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float x = c * (cellSize);
                float y = -r * (cellSize);

                // Room square
                var room = CreateRect("Room", transform);
                // Add the room to the stored minimap squares
                roomRects[r, c] = room;
                room.anchoredPosition = new Vector2(x, y);
                room.sizeDelta = new Vector2(cellSize, cellSize);
                room.GetComponent<Image>().color =
                    (r == gm.currentRow && c == gm.currentCol) ? currentRoomColor : roomColor;

                // Add player icon to minimap current square
                if(r == gm.currentRow && c == gm.currentCol){
                    AddIcon(roomRects[r, c], playerIcon);
                }
                
                // Set the boss room to be a red square
                if (r == 0 && c == 0 && (gm.currentRow != 0 | gm.currentCol != 0)) {
                    room.GetComponent<Image>().color = Color.red;
                    AddIcon(roomRects[0, 0], bossIcon);
                }

                // Draw the four sides of the walls as doors per default. If there is a wall, the door will
                // simply be overwritten :)
                //DrawDoor(x, y + cellSize / 2f, true);
                //DrawDoor(x, y - cellSize / 2f, true);
                //DrawDoor(x + cellSize / 2f, y, true);
                //DrawDoor(x - cellSize / 2f, y, true);

                // This version of the minimap has walls around the outside of the map
                if (!gm.IsOpen(r, c, 0)) // top
                    DrawWall(x, y + cellSize / 2f, true);
                else {
                    DrawDoor(x, y + cellSize / 2f, true);
                }
                if (!gm.IsOpen(r, c, 2)) // bottom
                    DrawWall(x, y - cellSize / 2f, true);
                else {
                    DrawDoor(x, y - cellSize / 2f, true);
                }
                if (!gm.IsOpen(r, c, 3)) // left
                    DrawWall(x - cellSize / 2f, y, false);
                else {
                    DrawDoor(x - cellSize / 2f, y, false);
                }
                if (!gm.IsOpen(r, c, 1)) // right
                    DrawWall(x + cellSize / 2f, y, false);
                else {
                    DrawDoor(x + cellSize / 2f, y, false);
                }
            }
        }
    }

    void DrawWall(float x, float y, bool horizontal)
    {
        var wall = CreateRect("Wall", transform);
        wall.anchoredPosition = new Vector2(x, y);
        wall.sizeDelta = horizontal
            ? new Vector2(cellSize, wallThickness)
            : new Vector2(wallThickness, cellSize);
        wall.GetComponent<Image>().color = wallColor;
    }

    void DrawDoor(float x, float y, bool horizontal)
    {
        // First calculate how large each side of the wall next to the door is
        var wallSegmentLength = cellSize / 4f;
        float offset = cellSize * 3f / 8f;

        if(horizontal){
            // Create the left third/wall of the doorway
            var leftWall = CreateRect("DoorLeftWall", transform);
            leftWall.anchoredPosition = new Vector2(x - offset, y);
            leftWall.sizeDelta = new Vector2(wallSegmentLength, wallThickness);
            leftWall.GetComponent<Image>().color = wallColor;

            // Create the right third/wall of the doorway
            var rightWall = CreateRect("DoorRightWall", transform);
            rightWall.anchoredPosition = new Vector2(x + offset, y);
            rightWall.sizeDelta = new Vector2(wallSegmentLength, wallThickness);
            rightWall.GetComponent<Image>().color = wallColor;
        } else {
            // Create the top third/wall of the doorway
            var topWall = CreateRect("DoorTopWall", transform);
            topWall.anchoredPosition = new Vector2(x, y + offset);
            topWall.sizeDelta = new Vector2(wallThickness, wallSegmentLength);
            topWall.GetComponent<Image>().color = wallColor;

            // Create the bottom third/wall of the doorway
            var bottomWall = CreateRect("DoorBottomWall", transform);
            bottomWall.anchoredPosition = new Vector2(x, y - offset);
            bottomWall.sizeDelta = new Vector2(wallThickness, wallSegmentLength);
            bottomWall.GetComponent<Image>().color = wallColor;
        }
      
    }

    RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }


    // Helper method for adding sprites to minimap rooms
    void AddIcon(RectTransform room, Sprite icon)
    {
        var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGo.transform.SetParent(room, false);
        
        var rt = iconGo.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(cellSize - 2, cellSize - 2);
        
        iconGo.GetComponent<Image>().sprite = icon;
    }
}