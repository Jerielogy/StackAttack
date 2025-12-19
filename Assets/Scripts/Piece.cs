using UnityEngine;
using Mirror;

public class Piece : NetworkBehaviour
{
    float lastFall = 0;
    BoardManager board;

    void Start()
    {
        // 1. Detect Board
        if (transform.position.x < 30)
            board = GameObject.Find("Environment_P1")?.GetComponent<BoardManager>();
        else
            board = GameObject.Find("Environment_P2")?.GetComponent<BoardManager>();

        // 2. SAFETY CHECK: If this is a preview piece
        if (transform.parent != null && transform.parent.name.Contains("Border"))
        {
            // Fix: Force small scale for preview
            transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            this.enabled = false;
            return;
        }

        // 3. Game Over Check
        if (!IsValidGridPos())
        {
            this.enabled = false;
            if (isOwned && NetworkClient.localPlayer != null)
            {
                NetworkClient.localPlayer.GetComponent<PlayerStats>().CmdTriggerGameOver();
            }
        }
    }

    void Update()
    {
        if (!isOwned) return;

        float speedMultiplier = 1.0f;
        PlayerStats stats = NetworkClient.localPlayer.GetComponent<PlayerStats>();
        if (stats != null)
        {
            speedMultiplier = Mathf.Max(0.1f, 1.0f - (stats.level * 0.1f));
        }

        // HARD DROP FIX
        if (Input.GetKeyDown(KeyCode.Space))
        {
            while (true)
            {
                transform.position += Vector3.down;
                if (!IsValidGridPos())
                {
                    transform.position += Vector3.up; // Move back to last valid spot
                    break; // Hit the floor or garbage
                }
            }
            UpdateGrid();
            LockPiece();
            return;
        }

        // Fall logic
        if (Input.GetKeyDown(KeyCode.DownArrow) || Time.time - lastFall >= speedMultiplier)
        {
            transform.position += Vector3.down;
            if (IsValidGridPos())
            {
                UpdateGrid();
            }
            else
            {
                transform.position += Vector3.up;
                LockPiece();
            }
            lastFall = Time.time;
        }

        if (Input.GetKeyDown(KeyCode.LeftArrow)) { if (Move(Vector3.left)) AudioManager.Instance.PlayMove(); }
        if (Input.GetKeyDown(KeyCode.RightArrow)) { if (Move(Vector3.right)) AudioManager.Instance.PlayMove(); }
        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            transform.Rotate(0, 0, -90);
            if (!IsValidGridPos())
            {
                transform.Rotate(0, 0, 90); // Failed to rotate
            }
            else
            {
                UpdateGrid();
                AudioManager.Instance.PlayMove(); // Only play if rotation worked!
            }
        }
    }

    bool Move(Vector3 dir) // Change 'void' to 'bool'
    {
        transform.position += dir;
        if (IsValidGridPos())
        {
            UpdateGrid();
            return true; // The move worked!
        }
        else
        {
            transform.position -= dir;
            return false; // The move failed (hit a wall/block)
        }
    }

    void LockPiece()
    {
        if (board != null) board.DeleteFullRows();
        this.enabled = false;
        if (NetworkClient.localPlayer != null)
            NetworkClient.localPlayer.GetComponent<PlayerController>().CmdSpawnBlock();
    }

    bool IsValidGridPos()
    {
        if (board == null) return false;
        foreach (Transform child in transform)
        {
            Vector2 v = board.RoundVec2(child.position);
            if (!board.InsideBorder(v)) return false;
            Vector2Int idx = board.GetGridIndex(v);
            if (board.grid[idx.x, idx.y] != null && board.grid[idx.x, idx.y].parent != transform)
                return false;
        }
        return true;
    }

    public void UpdateGrid()
    {
        if (board == null) return;
        for (int y = 0; y < board.height; y++)
        {
            for (int x = 0; x < board.width; x++)
            {
                if (board.grid[x, y] != null && board.grid[x, y].parent == transform)
                    board.grid[x, y] = null;
            }
        }
        foreach (Transform child in transform)
        {
            Vector2 v = board.RoundVec2(child.position);
            Vector2Int idx = board.GetGridIndex(v);
            board.grid[idx.x, idx.y] = child;
        }
    }
}