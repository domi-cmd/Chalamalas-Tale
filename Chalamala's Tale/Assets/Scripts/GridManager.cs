using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public int currentRow = 3;
    public int currentCol = 3;

    private const int ROWS = 4;
    private const int COLS = 4;

    // [row, col, side]  side: 0=top, 1=right, 2=bottom, 3=left
    // true = OPEN (doorway exists)
    private bool[,,] walls = new bool[ROWS, COLS, 4];

    // Direction: (dr, dc, mySide, neighborSide)
    private static readonly (int dr, int dc, int my, int nb)[] DIRECTIONS = new[]
    {
        (-1,  0,  0,  2),  // top:    my=top(0),    neighbor=bottom(2)
        ( 1,  0,  2,  0),  // bottom: my=bottom(2), neighbor=top(0)
        ( 0, -1,  3,  1),  // left:   my=left(3),   neighbor=right(1)
        ( 0,  1,  1,  3),  // right:  my=right(1),  neighbor=left(3)
    };

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        GenerateGrid();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateInstance()
    {
        if (Instance != null) return;
        var go = new GameObject("GridManager");
        go.AddComponent<GridManager>();
    }

    public bool IsOpen(int row, int col, int side) => walls[row, col, side];

    public void MoveToRoom(int side)
    {
        if (side == 0) currentRow--;
        if (side == 1) currentCol++;
        if (side == 2) currentRow++;
        if (side == 3) currentCol--;
        SceneManager.LoadScene("Room");
    }

    public void GenerateGrid()
    {
        // Reset all walls to closed
        for (int r = 0; r < ROWS; r++)
            for (int c = 0; c < COLS; c++)
                for (int s = 0; s < 4; s++)
                    walls[r, c, s] = false;

        bool[,] visited = new bool[ROWS, COLS];
        var stack = new Stack<(int r, int c)>();
        stack.Push((0, 0));
        visited[0, 0] = true;

        while (stack.Count > 0)
        {
            var (r, c) = stack.Peek();

            // Check for neighbors that already have their wall open toward us
            var forcedDirs = new List<int>();
            for (int i = 0; i < DIRECTIONS.Length; i++)
            {
                var (dr, dc, my, nb) = DIRECTIONS[i];
                int nr = r + dr, nc = c + dc;
                if (InBounds(nr, nc) && !visited[nr, nc] && walls[nr, nc, nb])
                    forcedDirs.Add(i);
            }

            int chosen;
            if (forcedDirs.Count > 0)
            {
                chosen = forcedDirs[Random.Range(0, forcedDirs.Count)];
                BreakWall(r, c, chosen);
                var (dr, dc, _, _) = DIRECTIONS[chosen];
                int nr = r + dr, nc = c + dc;
                visited[nr, nc] = true;
                stack.Push((nr, nc));
            }
            else
            {
                // Find unvisited neighbors
                var available = new List<int>();
                for (int i = 0; i < DIRECTIONS.Length; i++)
                {
                    var (dr, dc, _, _) = DIRECTIONS[i];
                    int nr = r + dr, nc = c + dc;
                    if (InBounds(nr, nc) && !visited[nr, nc])
                        available.Add(i);
                }

                if (available.Count == 0) { stack.Pop(); continue; }

                chosen = available[Random.Range(0, available.Count)];
                BreakWall(r, c, chosen);
                var (ddr, ddc, _, _) = DIRECTIONS[chosen];
                int nnr = r + ddr, nnc = c + ddc;
                visited[nnr, nnc] = true;
                stack.Push((nnr, nnc));
            }

            // 50% chance to break a second wall — but not for boss room [0,0]
            if (r == 0 && c == 0) continue;

            var remaining = new List<int>();
            for (int i = 0; i < DIRECTIONS.Length; i++)
            {
                if (i == chosen) continue;
                var (dr, dc, _, _) = DIRECTIONS[i];
                int nr = r + dr, nc = c + dc;
                if (InBounds(nr, nc) && !visited[nr, nc])
                    remaining.Add(i);
            }

            if (remaining.Count > 0 && Random.value < 0.5f)
            {
                int extra = remaining[Random.Range(0, remaining.Count)];
                BreakWall(r, c, extra);
                var (dr, dc, _, _) = DIRECTIONS[extra];
                int nr = r + dr, nc = c + dc;
                visited[nr, nc] = true;
                stack.Push((nr, nc));
            }
        }
    }

    private void BreakWall(int r, int c, int dirIndex)
    {
        var (dr, dc, my, nb) = DIRECTIONS[dirIndex];
        walls[r, c, my] = true;
        walls[r + dr, c + dc, nb] = true;
    }

    private bool InBounds(int r, int c) => r >= 0 && r < ROWS && c >= 0 && c < COLS;
}