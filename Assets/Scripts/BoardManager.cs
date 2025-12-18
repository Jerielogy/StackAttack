using UnityEngine;
using Mirror;
using System.Collections;

public class BoardManager : NetworkBehaviour
{
    public int width = 14;
    public int height = 22;
    public Transform[,] grid;

    void Awake()
    {
        grid = new Transform[width, height];
    }

    public Vector2 RoundVec2(Vector2 v)
    {
        return new Vector2(Mathf.Round(v.x), Mathf.Round(v.y));
    }

    public Vector2Int GetGridIndex(Vector2 pos)
    {
        int x = Mathf.RoundToInt(pos.x - transform.position.x);
        int y = Mathf.RoundToInt(pos.y - transform.position.y);
        return new Vector2Int(x, y);
    }

    public bool InsideBorder(Vector2 pos)
    {
        Vector2Int idx = GetGridIndex(pos);
        return (idx.x >= 0 && idx.x < width && idx.y >= 0);
    }

    // -------------------
    // DELETE FULL ROWS
    // -------------------
    public void DeleteFullRows()
    {
        int linesClearedThisTurn = 0;

        for (int y = 0; y < height; ++y)
        {
            if (IsRowFull(y))
            {
                DeleteRow(y);
                DecreaseRowsAbove(y + 1);
                y--;
                linesClearedThisTurn++;
            }
        }

        if (linesClearedThisTurn > 0)
        {
            if (NetworkClient.localPlayer != null)
            {
                PlayerStats stats = NetworkClient.localPlayer.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    int points = 0;
                    switch (linesClearedThisTurn)
                    {
                        case 1: points = 40; break;
                        case 2: points = 100; break;
                        case 3: points = 300; break;
                        case 4: points = 1200; break;
                        default: points = linesClearedThisTurn * 100; break;
                    }
                    stats.CmdAddScore(points);
                }
            }

            // --- SEND GARBAGE TO OPPONENT IF 4-LINE CLEAR ---
            if (linesClearedThisTurn == 4 && isServer)
            {
                // Find all boards and send garbage to the other player
                BoardManager[] boards = FindObjectsOfType<BoardManager>();
                foreach (BoardManager b in boards)
                {
                    if (b != this) // Opponent board
                        b.RpcSpawnGarbageLine();
                }
            }
        }
    }

    // -------------------
    // SPAWN GARBAGE LINE (NETWORKED)
    // -------------------
    [ClientRpc]
    void RpcSpawnGarbageLine()
    {
        StartCoroutine(SpawnGarbageCoroutine());
    }

    IEnumerator SpawnGarbageCoroutine()
    {
        // 1. Shift all rows up by 1
        for (int y = height - 2; y >= 0; y--)
        {
            for (int x = 0; x < width; x++)
            {
                if (grid[x, y] != null)
                {
                    grid[x, y + 1] = grid[x, y];
                    grid[x, y] = null;
                    grid[x, y + 1].position += Vector3.up;
                }
            }
        }

        // 2. Spawn a full row at bottom
        for (int x = 0; x < width; x++)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.transform.position = new Vector3(transform.position.x + x, transform.position.y, 0);
            block.transform.SetParent(transform);

            // Only spawn networked objects on the server
            if (isServer) NetworkServer.Spawn(block);

            grid[x, 0] = block.transform;

            // Destroy after 5 seconds
            StartCoroutine(DestroyBlockAfterSeconds(block, 5f));
        }

        yield return null;
    }

    IEnumerator DestroyBlockAfterSeconds(GameObject block, float seconds)
    {
        yield return new WaitForSeconds(seconds);

        Vector2Int idx = GetGridIndex(block.transform.position);
        if (idx.x >= 0 && idx.x < width && idx.y >= 0 && idx.y < height)
            grid[idx.x, idx.y] = null;

        if (isServer) NetworkServer.Destroy(block);
        else Destroy(block);
    }

    // -------------------
    // ROW HELPERS
    // -------------------
    public void DeleteRow(int y)
    {
        for (int x = 0; x < width; ++x)
        {
            if (grid[x, y] != null)
            {
                Destroy(grid[x, y].gameObject);
                grid[x, y] = null;
            }
        }
    }

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

    public void DecreaseRowsAbove(int y)
    {
        for (int i = y; i < height; i++)
        {
            DecreaseRow(i);
        }
    }

    public bool IsRowFull(int y)
    {
        for (int x = 0; x < width; ++x)
        {
            if (grid[x, y] == null)
                return false;
        }
        return true;
    }
}
