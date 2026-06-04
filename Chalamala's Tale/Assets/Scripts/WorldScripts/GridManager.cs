using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GridManager : MonoBehaviour
{
    private static GridManager _instance;

    //singleton 
    public static GridManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject obj = new GameObject("GridManager");
                _instance = obj.AddComponent<GridManager>();
            }

            return _instance;
        }
    }
    // Keep track of which rooms have been visited, and should be part of the minimap
    public HashSet<(int, int)> visitedRooms = new HashSet<(int, int)>();

    // Keep track of what rooms have been cleared, and should not have spawning enemies
    // This has to be tracked separately from visited rooms, since you can visit a room without clearing it by dying.
    // Initialized to false by default
    public bool[,] clearedRooms = new bool[4, 4];

    // Keep track of the types of the rooms (enemy, npc, boss, start, etc.)
    public RoomTypes[,] roomTypes = new RoomTypes[4, 4];

    // Keeps track of the current row and column in the 4x4 room grid. 
    // At the moment, the player starts at the bottom right, meaning coordinates (3, 3).
    public int currentRow = 3;
    public int currentCol = 3;

    // Keeps track of the side from which the player exits the room, as to place him on the opposite side of the next room
    // (If u go left, u should come out at the right side of the next room, etc.)
    public int enteredFromSide = -1;

    private const int ROWS = 4;
    private const int COLS = 4;

    private Dictionary<RoomTypes, int> roomTypeCount = new Dictionary<RoomTypes, int>();
    private const int MAX_PER_TYPE = 3; // to avoid having too many repetitions of the same room

    private bool CanUseType(RoomTypes type)
    {
        if (!roomTypeCount.ContainsKey(type))
            roomTypeCount[type] = 0;

        return roomTypeCount[type] < MAX_PER_TYPE;
    }

    private void RegisterType(RoomTypes type)
    {
        if (!roomTypeCount.ContainsKey(type))
            roomTypeCount[type] = 0;

        roomTypeCount[type]++;
    }

    private bool[,] reserved = new bool[4, 4]; // not to override fixed or important rooms

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

    /*
    dangerous right now as we don't use this gridmanager in the tutorial section,
    the initialization happens before "Room" gets loaded
    // Creates the GridManager before any scene loads if it doesn't exist yet
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void CreateInstance()
    {
        if (Instance != null) return;
        new GameObject("GridManager").AddComponent<GridManager>();
    }
    */

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        GenerateGrid();
    }

    // Returns whether a certain side of the room has a door
    public bool IsOpen(int row, int col, int side) => walls[row, col, side];

    // Updates current room position and loads the new room that the player moved to
    public void MoveToRoom(int side)
    {
        // Mark the previous room as cleared
        clearedRooms[currentRow, currentCol] = true;

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
                SceneManager.LoadScene("RoomDragonWarning");
                break;

            case RoomTypes.Start_Room:
                SceneManager.LoadScene("Room");
                break;

            case RoomTypes.Goat_Room:
                SceneManager.LoadScene("RoomEnemyGoat");
                break;

            case RoomTypes.Cheese_Room:
                SceneManager.LoadScene("RoomCheese");
                break;

            case RoomTypes.Turret_Room:
                SceneManager.LoadScene("RoomEnemyTurret");
                break;

            case RoomTypes.Dragon_Room:
                SceneManager.LoadScene("Bossroom");
                break;

            case RoomTypes.Ranged_Attack_Upgrade_Room:
                SceneManager.LoadScene("RoomUpgradeRangedAttack");
                break;

            case RoomTypes.Arrows_Enemy_Room:
                SceneManager.LoadScene("RoomEnemyArrows");
                break;

            case RoomTypes.Knight_Enemy_Room:
                SceneManager.LoadScene("RoomEnemyKnight");
                break;

            default:
                SceneManager.LoadScene("Room");
                break;
        }

        Debug.Log("Moved to room type: " + roomTypes[currentRow, currentCol]);
    }

    // Generates the map grid using a recursive dfs approach
    public void GenerateGrid()
    {
        roomTypeCount.Clear();
        reserved = new bool[4, 4];
        var visited = new bool[ROWS, COLS];
        var stack = new Stack<(int r, int c)>();

        stack.Push((0, 0));
        visited[0, 0] = true;

        // Set the room types of the start and final boss room, as they are fixed in place
        roomTypes[0, 0] = RoomTypes.Dragon_Room;
        roomTypes[3, 3] = RoomTypes.Start_Room;

        // Also hardcode the upgrade ranged attack room
        roomTypes[0, 3] = RoomTypes.Ranged_Attack_Upgrade_Room;

        roomTypes[2,2] = RoomTypes.NPC_Room;

        reserved[0, 0] = true;
        reserved[3, 3] = true;
        reserved[0, 3] = true;
        reserved[2,2] = true;

        // Place goat and cheese rooms in guaranteed positions
        PlaceGuaranteedRooms();

        // Create a random number generator to randomly assign the other rooms
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

            // 20% chance to carve a second passage for more open layouts
            // Disabled for the boss room to keep it more isolated
            if (r == 0 && c == 0) continue;
            neighbors = GetUnvisitedNeighbors(r, c, visited);
            if (neighbors.Count > 0 && Random.value < 0.2f)
                OpenPassage(r, c, neighbors[Random.Range(0, neighbors.Count)], visited, stack);
        }
        /*
        safety check, prints the content of the generated cells
        */
        for (int r = 0; r < ROWS; r++)
        {
            for (int c = 0; c < COLS; c++)
            {
                Debug.Log($"[{r},{c}] = {roomTypes[r,c]}");
            }
        }
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
        // Skip reserved cells (boss room, start room, special rooms)
        if (reserved[r, c])
            return;

        // BUG FIX: The original code had two `safety++` increments per iteration,
        // meaning the loop counter doubled each check. It also never broke out
        // on a successful find — the `break` was only on the safety limit.
        // Fixed: single counter, break immediately when a valid type is found.

        RoomTypes type = RoomTypes.NPC_Room; // fallback default
        bool found = false;

        for (int safety = 0; safety < 100; safety++)
        {
            int roomType = roomRandomNumber.Next(6, 10);
            type = (RoomTypes)roomType;

            if (CanUseType(type))
            {
                found = true;
                break;
            }
        }

        if (!found)
        {
            // If we exhausted attempts, try to find any available type
            foreach (RoomTypes t in System.Enum.GetValues(typeof(RoomTypes)))
            {
                if (t == RoomTypes.Start_Room || t == RoomTypes.Dragon_Room ||
                    t == RoomTypes.Ranged_Attack_Upgrade_Room || t == RoomTypes.Goat_Room ||
                    t == RoomTypes.Cheese_Room|| t == RoomTypes.NPC_Room )
                    continue;

                if (CanUseType(t))
                {
                    type = t;
                    found = true;
                    break;
                }
            }
        }

        if (!found)
            Debug.Log($"Could not assign a room type to [{r},{c}] — all types at max count.");

        roomTypes[r, c] = type;
        RegisterType(type);
    }

    // Placement of goat and cheese rooms guaranteed once each
    private void PlaceGuaranteedRooms()
    {
        PlaceSpecial(RoomTypes.Goat_Room);
        PlaceSpecial(RoomTypes.Cheese_Room);
    }

    private void PlaceSpecial(RoomTypes type)
    {
        List<(int row, int col)> valid = new List<(int, int)>();

        for (int row = 1; row < ROWS; row++) // avoids row 0
        {
            for (int col = 1; col < COLS; col++)  // avoids column 0
            {
                if (reserved[row, col]) continue;
                valid.Add((row, col));
            }
        }

        if (valid.Count == 0)
        {
            Debug.LogError("No valid cell for: " + type);
            return;
        }

        var (r, c) = valid[Random.Range(0, valid.Count)];

        roomTypes[r, c] = type;
        reserved[r, c] = true;

        RegisterType(type);
    }


    // Helper method that opens the wall between current cell and its neighbor in the designated direction
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

/*
types of room present:
- first 3 placed at fixed cells
- goat and cheese at random cells in a specific area (3*3) placed once each
- NPC placed once at at least one cell of distance from start  (atm just at a fixed cell)
- other (enemies) randomly placed in the remaining cells 
*/

public enum RoomTypes
{
    Start_Room,   //0
    Dragon_Room,  //1
    Ranged_Attack_Upgrade_Room,  //2

    Goat_Room,  //3
    Cheese_Room,  //4

    NPC_Room,  //5

    Chasing_Enemy_Room,  //6
    Turret_Room,  //7
    Arrows_Enemy_Room,  //8
    Knight_Enemy_Room   //9
}