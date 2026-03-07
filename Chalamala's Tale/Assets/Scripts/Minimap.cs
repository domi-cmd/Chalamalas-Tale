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

    void Start() => Draw();

    public void Draw()
    {
        // Clear old minimap
        foreach (Transform child in transform)
            Destroy(child.gameObject);

        int rows = 4, cols = 4;
        var gm = GridManager.Instance;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float x = c * (cellSize + wallThickness);
                float y = -r * (cellSize + wallThickness);

                // Room square
                var room = CreateRect("Room", transform);
                room.anchoredPosition = new Vector2(x, y);
                room.sizeDelta = new Vector2(cellSize, cellSize);
                room.GetComponent<Image>().color =
                    (r == gm.currentRow && c == gm.currentCol) ? currentRoomColor : roomColor;

                // Walls — draw a wall line on each CLOSED side
                // This version has no walls around the outside of the map
                /**
                if (!gm.IsOpen(r, c, 0) && r > 0) // top
                    DrawWall(x, y + cellSize / 2f + wallThickness / 2f, true);
                if (!gm.IsOpen(r, c, 2) && r < rows - 1) // bottom
                    DrawWall(x, y - cellSize / 2f - wallThickness / 2f, true);
                if (!gm.IsOpen(r, c, 3) && c > 0) // left
                    DrawWall(x - cellSize / 2f - wallThickness / 2f, y, false);
                if (!gm.IsOpen(r, c, 1) && c < cols - 1) // right
                    DrawWall(x + cellSize / 2f + wallThickness / 2f, y, false);
                **/

                // This version of the minimap has walls around the outside of the map
                if (!gm.IsOpen(r, c, 0)) // top
                    DrawWall(x, y + cellSize / 2f + wallThickness / 2f, true);
                if (!gm.IsOpen(r, c, 2)) // bottom
                    DrawWall(x, y - cellSize / 2f - wallThickness / 2f, true);
                if (!gm.IsOpen(r, c, 3)) // left
                    DrawWall(x - cellSize / 2f - wallThickness / 2f, y, false);
                if (!gm.IsOpen(r, c, 1)) // right
                    DrawWall(x + cellSize / 2f + wallThickness / 2f, y, false);
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

    RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        return rt;
    }
}