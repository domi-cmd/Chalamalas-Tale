using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/*
class for managing the simple tutorial grid:
3 rooms on one line, the player encounters them in this order (starting from the one placed at the bottom to the top one):
- dialogue room
- easy fight
- dragon on steroid fight (the player dies)
*/

public class BasicGridManager : MonoBehaviour
{
    public static BasicGridManager Instance;
    // Keep track of which rooms have been visited, and should be part of the minimap
    public HashSet<(int, int)> visitedRooms = new HashSet<(int, int)>();

    // Keep track of the types of the rooms (here just three types, hardcoded in the door system)
    public TutoRoomTypes[,] roomTypes = new TutoRoomTypes[3, 1];

    // Keeps track of the current row and column in the 3x1 room grid. 
    // (0,0): dragon
    // (1,0): easy fight
    // (2,0): spawn introduction
    public int currentRow = 2;
    public int currentCol = 0;

    // Keeps track of the side from which the player exits the room, as to place him on the opposite side of the next room
    // (If u go left, u should come out at the right side of the next room, ect.)
    public int enteredFromSide = -1;

    private const int ROWS = 3;
    private const int COLS = 1;

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
        new GameObject("GridManager").AddComponent<BasicGridManager>();
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
        TutoRoomTypes roomTypeToLoad = roomTypes[currentRow, currentCol];
        
        switch (roomTypeToLoad)
        {
            case TutoRoomTypes.Dragon_Room:
                SceneManager.LoadScene("dragon_killing_you");
                break;
            
            case TutoRoomTypes.Simple_Enemy_Room:
                SceneManager.LoadScene("easy_fight");
                break;

            case TutoRoomTypes.Start_Room:
                SceneManager.LoadScene("Tutorial_first_scene");
                break;

        }
        Debug.Log("Test" + roomTypes[currentRow, currentCol]);

    }

    // Generates the map grid using an hardcoded structure
    public void GenerateGrid()
    {
        // Clear everything
        visitedRooms.Clear();

        // Define room types manually
        roomTypes[0, 0] = TutoRoomTypes.Dragon_Room;       // top
        roomTypes[1, 0] = TutoRoomTypes.Simple_Enemy_Room; // middle
        roomTypes[2, 0] = TutoRoomTypes.Start_Room;        // bottom

        // Open passages (vertical only)

        // Between (2,0) <-> (1,0)
        walls[2, 0, 0] = true; // top of start
        walls[1, 0, 2] = true; // bottom of middle

        // Between (1,0) <-> (0,0)
        walls[1, 0, 0] = true; // top of middle
        walls[0, 0, 2] = true; // bottom of boss
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
        // Decide on the type of the room (at the moment there are only two types, 0 for npc, 1 for enemy)
        int roomType = roomRandomNumber.Next(2, 4);

        // Assign the type
        roomTypes[r, c] = (TutoRoomTypes)roomType;
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

public enum TutoRoomTypes
{
    Start_Room,
    Dragon_Room,
    Simple_Enemy_Room
}