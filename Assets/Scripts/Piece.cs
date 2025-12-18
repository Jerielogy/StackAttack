using UnityEngine;
using Mirror;

public class Piece : NetworkBehaviour
{
    // Make sure these match the variable names you used before!
    float lastFall = 0;

    // We need a reference to the specific board this piece is on
    BoardManager board;

    void Start()
    {
        // 1. FIND THE CORRECT BOARD AUTOMATICALLY
        if (transform.position.x < 30)
        {
            GameObject env = GameObject.Find("Environment_P1");
            if (env != null) board = env.GetComponent<BoardManager>();
        }
        else
        {
            GameObject env = GameObject.Find("Environment_P2");
            if (env != null) board = env.GetComponent<BoardManager>();
        }

        // --- NEW: GAME OVER CHECK ---
        // We check immediately if the spawn position is valid.
        // If IsValidGridPos returns FALSE right now, it means we spawned INSIDE another block.
        if (!IsValidGridPos())
        {
            Debug.Log("GAME OVER!");

            // 1. Stop this script so the player can't move the overlapping block
            enabled = false;

            // 2. Tell the PlayerStats to trigger the Game Over screen
            if (NetworkClient.localPlayer != null)
            {
                PlayerStats stats = NetworkClient.localPlayer.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    // This function stops the game and shows "Game Over" text
                    stats.TargetGameOver();
                }
            }
        }
    }

    void Update()
    {
        // STOP THE OTHER PLAYER FROM MOVING MY BLOCK
        if (!isOwned) return;

        // --- MOVEMENT LOGIC ---

        // Move Left
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            transform.position += new Vector3(-1, 0, 0);
            if (IsValidGridPos())
                UpdateGrid();
            else
                transform.position += new Vector3(1, 0, 0);
        }

        // Move Right
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            transform.position += new Vector3(1, 0, 0);
            if (IsValidGridPos())
                UpdateGrid();
            else
                transform.position += new Vector3(-1, 0, 0);
        }

        // Rotate
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            transform.Rotate(0, 0, -90);
            if (IsValidGridPos())
                UpdateGrid();
            else
                transform.Rotate(0, 0, 90);
        }

        // HARD DROP (Spacebar)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            while (IsValidGridPos())
            {
                transform.position += new Vector3(0, -1, 0);
            }

            // Step back up one unit
            transform.position += new Vector3(0, 1, 0);

            UpdateGrid();

            if (board != null) board.DeleteFullRows();

            enabled = false;

            // Spawn next block
            if (NetworkClient.localPlayer != null)
            {
                NetworkClient.localPlayer.GetComponent<PlayerController>().CmdSpawnBlock();
            }

            return;
        }

        // Move Down / Fall
        if (Input.GetKeyDown(KeyCode.DownArrow) || Time.time - lastFall >= 1)
        {
            transform.position += new Vector3(0, -1, 0);

            if (IsValidGridPos())
            {
                UpdateGrid();
            }
            else
            {
                // LANDED LOGIC
                transform.position += new Vector3(0, 1, 0);

                if (board != null)
                {
                    board.DeleteFullRows();
                }

                enabled = false;

                // Spawn the next block
                if (NetworkClient.localPlayer != null)
                {
                    NetworkClient.localPlayer.GetComponent<PlayerController>().CmdSpawnBlock();
                }
            }
            lastFall = Time.time;
        }
    }

    // --- HELPER FUNCTIONS ---

    bool IsValidGridPos()
    {
        if (board == null) return false;

        foreach (Transform child in transform)
        {
            Vector2 v = board.RoundVec2(child.position);

            if (!board.InsideBorder(v))
                return false;

            // Check if another block is already there
            Vector2Int idx = board.GetGridIndex(v);
            if (board.grid[idx.x, idx.y] != null &&
                board.grid[idx.x, idx.y].parent != transform)
                return false;
        }
        return true;
    }

    public void UpdateGrid()
    {
        if (board == null) return;

        // Clear old positions
        for (int y = 0; y < board.height; y++)
        {
            for (int x = 0; x < board.width; x++)
            {
                if (board.grid[x, y] != null && board.grid[x, y].parent == transform)
                    board.grid[x, y] = null;
            }
        }

        // Set new positions
        foreach (Transform child in transform)
        {
            Vector2 v = board.RoundVec2(child.position);
            Vector2Int idx = board.GetGridIndex(v);
            board.grid[idx.x, idx.y] = child;
        }
    }
}
