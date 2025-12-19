using UnityEngine;
using Mirror;

public class GarbageManager : NetworkBehaviour
{
    public BoardManager boardP1;
    public BoardManager boardP2;
    public GameObject garbageBlockPrefab;

    [Server]
    public void SendGarbage(int lines, int senderPlayerIndex)
    {
        if (lines <= 1) return;

        // Nintendo/Classic scaling
        int garbageLines = lines switch { 2 => 1, 3 => 2, 4 => 4, _ => lines };
        int targetIndex = (senderPlayerIndex == 0) ? 1 : 0;

        RpcReceiveGarbage(garbageLines, targetIndex);
    }

    [ClientRpc]
    void RpcReceiveGarbage(int garbageLines, int targetPlayerIndex)
    {
        BoardManager targetBoard = (targetPlayerIndex == 0) ? boardP1 : boardP2;
        if (targetBoard == null) return;

        for (int i = 0; i < garbageLines; i++)
        {
            AddPermanentGarbageLine(targetBoard);
        }
    }

    void AddPermanentGarbageLine(BoardManager board)
    {
        // 1. Shift existing blocks up
        Piece[] allPieces = FindObjectsOfType<Piece>(true);
        foreach (Piece piece in allPieces)
        {
            if (Vector3.Distance(piece.transform.position, board.transform.position) > 20) continue;

            piece.transform.position += Vector3.up;
            // No need to call UpdateGrid here, DecreaseRow/IncreaseRow logic handles it better
        }

        // 2. Logic Shift: We must shift the INTERNAL grid array up too!
        // If we don't do this, the garbage row overwrites what was already there.
        for (int y = board.height - 1; y > 0; y--)
        {
            for (int x = 0; x < board.width; x++)
            {
                board.grid[x, y] = board.grid[x, y - 1];
            }
        }

        // 3. Pick a random hole
        int holeX = Random.Range(0, board.width);

        // 4. Spawn the row at y = 0
        for (int x = 0; x < board.width; x++)
        {
            if (x == holeX)
            {
                board.grid[x, 0] = null; // Ensure the hole is empty in logic
                continue;
            }

            Vector3 pos = board.transform.position + new Vector3(x, 0, 0);
            GameObject block = Instantiate(garbageBlockPrefab, pos, Quaternion.identity);

            // CRITICAL FIX: Assign the transform to the grid so pieces collide with it
            board.grid[x, 0] = block.transform;
        }
    }
}