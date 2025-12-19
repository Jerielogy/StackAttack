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
        // 1. Shift ALL visual blocks currently in the grid UP
        // We loop through the board and move anything that isn't null
        for (int y = board.height - 1; y >= 0; y--)
        {
            for (int x = 0; x < board.width; x++)
            {
                if (board.grid[x, y] != null)
                {
                    // Physically move the block up in the world
                    board.grid[x, y].position += Vector3.up;
                }
            }
        }

        // 2. Shift the LOGICAL grid references UP by 1
        // We start from the top so we don't overwrite data as we go
        for (int y = board.height - 1; y > 0; y--)
        {
            for (int x = 0; x < board.width; x++)
            {
                board.grid[x, y] = board.grid[x, y - 1];
            }
        }

        // 3. Clear the bottom row logically to prepare for new garbage
        for (int x = 0; x < board.width; x++)
        {
            board.grid[x, 0] = null;
        }

        // 4. Spawn the new row with a hole
        int holeX = Random.Range(0, board.width);
        for (int x = 0; x < board.width; x++)
        {
            if (x == holeX) continue;

            Vector3 pos = board.transform.position + new Vector3(x, 0, 0);
            GameObject block = Instantiate(garbageBlockPrefab, pos, Quaternion.identity);

            // Register in the grid
            board.grid[x, 0] = block.transform;
        }
    }
}