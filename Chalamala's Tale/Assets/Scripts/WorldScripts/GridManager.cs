using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    // Keep track of which rooms have been visited, and should be part of the minimap
    public HashSet<(int, int)> visitedRooms = new HashSet<(int, int)>();

    // Keep track of the types of the rooms (enemy, npc, boss, startc, etc.)
    public RoomTypes[,] roomTypes = new RoomTypes[4, 4];

    // Keeps track of the current row and column in the 4x4 room grid. 
    // At the moment, the player starts at the bottom right, meaning coordinates (3, 3).
    public int currentRow = 3;
    public int currentCol = 3;

    // Keeps track of the side from which the player exits the room, as to place him on the opposite side of the next room
    // (If u go left, u should come out at the right side of the next room, ect.)
    public int enteredFromSide = -1;

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

    // Creates the GridManager before any scene loads if it doesnt exist yet
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateInstance()
    {
        if (Instance != null) return;
        new GameObject("GridManager").AddComponent<GridManager>();
    }

    void Awake()
    {
        // Singleton pattern that destroys duplicates and persist across scenes
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        GenerateGrid();
    }

    // Returns whether a certain side of the room has a door
    public bool IsOpen(int row, int col, int side) => walls[row, col, side];

    // Updates current room position and loads the new room that the player moved to
    public void MoveToRoom(int side)
    {
        enteredFromSide = side;
        if (side == 0) currentRow--;
        if (side == 1) currentCol++;
        if (side == 2) currentRow++;
        if (side == 3) currentCol--;

        // Check what type of room should be loaded (start room, enemy room, npc room, boss room, etc.)
        RoomTypes roomTypeToLoad = roomTypes[currentRow, currentCol];
        
        switch (roomTypeToLoad)
        {
            case RoomTypes.Chasing_Enemy_Room:
                SceneManager.LoadScene("RoomEnemyChasing");
                break;
            
            case RoomTypes.NPC_Room:
                SceneManager.LoadScene("RoomBasicNPC");
                //SceneManager.LoadScene("Room");
                break;

            case RoomTypes.Start_Room:
                SceneManager.LoadScene("Room");
                break;

            case RoomTypes.Goat_Room:
                SceneManager.LoadScene("RoomEnemyGoat");
                break;

            case RoomTypes.Turret_Room:
                SceneManager.LoadScene("RoomEnemyTurret");
                break;

            // TODO: Missing rooms here (such as dragon room, other enemy rooms, more specific types of npc rooms, etc)
            default:
                SceneManager.LoadScene("Room");
                break;
        }
        Debug.Log("Test" + roomTypes[currentRow, currentCol]);

    }

    // Generates the map grid using a recursive dfs approach
    public void GenerateGrid()
    {
        var visited = new bool[ROWS, COLS];
        var stack = new Stack<(int r, int c)>();

        stack.Push((0, 0));
        visited[0, 0] = true;

        // Create a random number generator to randomly assign the other rooms later in the method
        System.Random roomRandomNumber = new System.Random();

        while (stack.Count > 0)
        {
            var (r, c) = stack.Peek();

             // Assign the room to either be npc or enemy
            AssignRoomType(roomRandomNumber, r, c);

            var neighbors = GetUnvisitedNeighbors(r, c, visited);

            // Backtrack if no unvisited neighbors
            if (neighbors.Count == 0) { stack.Pop(); continue; }

            // Carve a passage to a random unvisited neighbor
            int dirIndex = neighbors[Random.Range(0, neighbors.Count)];
            OpenPassage(r, c, dirIndex, visited, stack);

            // 50% chance to carve a second passage for more open layouts
            // Disabled for the boss room to keep it more isolated
            if (r == 0 && c == 0) continue;
            neighbors = GetUnvisitedNeighbors(r, c, visited);
            if (neighbors.Count > 0 && Random.value < 0.5f)
                OpenPassage(r, c, neighbors[Random.Range(0, neighbors.Count)], visited, stack);
        }

        // Set the room types of the start and final boss room, as they are fixed in place
        roomTypes[0, 0] = RoomTypes.Dragon_Room;
        roomTypes[3, 3] = RoomTypes.Start_Room;
    }

    // Returns a list of direction indices leading to unvisited in-bounds neighbors
    List<int> GetUnvisitedNeighbors(int r, int c, bool[,] visited)
    {
        var result = new List<int>();
        for (int i = 0; i < DIRECTIONS.Length; i++)
        {
            var (dr, dc, _, _) = DIRECTIONS[i];
            int nr = r + dr, nc = c + dc;
            if (InBounds(nr, nc) && !visited[nr, nc])
                result.Add(i);
        }
        return result;
    }

    private void AssignRoomType(System.Random roomRandomNumber, int r, int c)
    {
        // Decide on the type of the room 
        int roomType = roomRandomNumber.Next(2, 6);

        // Assign the type
        roomTypes[r, c] = (RoomTypes)roomType;
    }

    // Helper method that opens the wall between current cell and its neighbor in designated direction
    void OpenPassage(int r, int c, int dirIndex, bool[,] visited, Stack<(int, int)> stack)
    {
        var (dr, dc, my, nb) = DIRECTIONS[dirIndex];
        walls[r, c, my] = true;
        walls[r + dr, c + dc, nb] = true;
        visited[r + dr, c + dc] = true;
        stack.Push((r + dr, c + dc));
    }

    private bool InBounds(int r, int c) => r >= 0 && r < ROWS && c >= 0 && c < COLS;
}

public enum RoomTypes
{
    Start_Room,
    Dragon_Room,
    NPC_Room,
    Chasing_Enemy_Room,
    Goat_Room,
    Turret_Room
}