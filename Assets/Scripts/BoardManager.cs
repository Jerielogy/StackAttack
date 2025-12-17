using UnityEngine;
using Mirror;
using System.Collections.Generic; // Needed for Lists

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

    // --- 1. THE DELETE LOGIC ---
    public void DeleteRow(int y)
    {
        // A. Clear the Logic Grid (Server Side)
        for (int x = 0; x < width; ++x)
        {
            if (grid[x, y] != null)
            {
                Transform blockTransform = grid[x, y];
                grid[x, y] = null;

                // Network Destroy logic
                if (blockTransform.parent != null)
                {
                    NetworkIdentity parentId = blockTransform.parent.GetComponent<NetworkIdentity>();
                    int siblingIndex = blockTransform.GetSiblingIndex();

                    if (NetworkClient.localPlayer != null)
                    {
                        PlayerStats stats = NetworkClient.localPlayer.GetComponent<PlayerStats>();
                        if (stats != null && parentId != null)
                        {
                            stats.CmdDestroyChild(parentId, siblingIndex);
                        }
                    }
                }
                else
                {
                    Destroy(blockTransform.gameObject);
                }
            }
        }

        // B. Force Visual Update on ALL Clients (Fixes the "Floating Blocks" issue)
        RpcVisualShift(y);
    }

    // --- 2. THE NEW VISUAL FIX ---
    [ClientRpc]
    void RpcVisualShift(int rowY)
    {
        // Don't run this on the Server (Host) because the normal logic already handled it there.
        // We only want to force this on the Client (Opponent) to fix their view.
        if (isServer) return;

        // 1. Find ALL Pieces in the scene (even disabled ones)
        Piece[] allPieces = FindObjectsOfType<Piece>(true);

        foreach (Piece p in allPieces)
        {
            // 2. Is this piece on THIS board? (Check X distance)
            // (If I am Board P1, I only care about pieces near me)
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist > 20) continue; // Skip pieces that belong to the other board

            // 3. Check every block inside this piece
            foreach (Transform block in p.transform)
            {
                // Round the Y position to be safe
                int blockY = Mathf.RoundToInt(block.position.y);

                if (blockY > rowY)
                {
                    // 4. MOVE IT DOWN!
                    block.position += Vector3.down;
                }
            }
        }
    }

    // --- STANDARD HELPER FUNCTIONS ---
    public void DecreaseRow(int y)
    {
        for (int x = 0; x < width; ++x)
        {
            if (grid[x, y] != null)
            {
                grid[x, y - 1] = grid[x, y];
                grid[x, y] = null;
                grid[x, y - 1].position += new Vector3(0, -1, 0);
            }
        }
    }

    public void DecreaseRowsAbove(int y)
    {
        for (int i = y; i < height; ++i)
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
        }
    }
}