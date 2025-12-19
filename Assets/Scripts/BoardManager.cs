using UnityEngine;
using Mirror;

public class BoardManager : NetworkBehaviour
{
    public int width = 14;
    public int height = 22;
    public Transform[,] grid;
    public int playerIndex;

    void Awake() => grid = new Transform[width, height];
    public Vector2 RoundVec2(Vector2 v) => new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
    public Vector2Int GetGridIndex(Vector2 pos) => new Vector2Int(Mathf.RoundToInt(pos.x - transform.position.x), Mathf.RoundToInt(pos.y - transform.position.y));
    public bool InsideBorder(Vector2 pos)
    {
        Vector2Int idx = GetGridIndex(pos);
        return (idx.x >= 0 && idx.x < width && idx.y >= 0);
    }

    public void DeleteFullRows()
    {
        int linesThisTurn = 0; // Track how many lines cleared in one drop
        for (int y = 0; y < height; ++y)
        {
            if (IsRowFull(y))
            {
                DeleteRow(y);
                DecreaseRowsAbove(y + 1);
                y--;
                linesThisTurn++; // Count it!
            }
        }

        if (linesThisTurn > 0 && NetworkClient.localPlayer != null)
        {
            PlayerStats stats = NetworkClient.localPlayer.GetComponent<PlayerStats>();
            if (stats != null)
            {
                // Send the raw number (1, 2, 3, or 4) to the Stats script
                stats.CmdAddScore(linesThisTurn);
            }

            // Garbage logic
            GarbageManager gm = FindObjectOfType<GarbageManager>();
            if (gm != null && isServer) gm.SendGarbage(linesThisTurn, playerIndex);
        }
    }

    public bool IsRowFull(int y) { for (int x = 0; x < width; ++x) if (grid[x, y] == null) return false; return true; }

    public void DeleteRow(int y)
    {
        for (int x = 0; x < width; ++x)
        {
            if (grid[x, y] != null) { Destroy(grid[x, y].gameObject); grid[x, y] = null; }
        }
        if (isServer) RpcVisualShift(y);
    }

    [ClientRpc]
    void RpcVisualShift(int rowY)
    {
        if (isServer) return;
        Piece[] allPieces = FindObjectsOfType<Piece>(true);
        foreach (Piece p in allPieces)
        {
            if (Vector3.Distance(transform.position, p.transform.position) > 20) continue;
            foreach (Transform block in p.transform)
            {
                if (Mathf.RoundToInt(block.position.y) > rowY) block.position += Vector3.down;
            }
        }
    }

    public void DecreaseRowsAbove(int y) { for (int i = y; i < height; ++i) DecreaseRow(i); }
    public void DecreaseRow(int y)
    {
        for (int x = 0; x < width; ++x)
        {
            if (grid[x, y] != null)
            {
                grid[x, y - 1] = grid[x, y];
                grid[x, y] = null;
                grid[x, y - 1].position += Vector3.down;
            }
        }
    }
}