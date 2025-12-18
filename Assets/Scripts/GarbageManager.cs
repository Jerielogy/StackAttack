using UnityEngine;
using Mirror;
using System.Collections;

public class GarbageManager : NetworkBehaviour
{
    public BoardManager boardP1;
    public BoardManager boardP2;
    public GameObject garbageBlockPrefab;
    public float garbageDuration = 5f;

    [Server]
    public void SendGarbage(int lines, int senderPlayerIndex)
    {
        if (lines <= 1) return;

        int garbageLines = 0;
        switch (lines)
        {
            case 2: garbageLines = 1; break;
            case 3: garbageLines = 2; break;
            case 4: garbageLines = 4; break;
            default: garbageLines = lines; break;
        }

        BoardManager targetBoard = (senderPlayerIndex == 0) ? boardP2 : boardP1;
        if (targetBoard == null) return;

        RpcReceiveGarbage(garbageLines, targetBoard.playerIndex);
    }

    [ClientRpc]
    void RpcReceiveGarbage(int garbageLines, int targetPlayerIndex)
    {
        BoardManager targetBoard = (targetPlayerIndex == 0) ? boardP1 : boardP2;
        if (targetBoard == null) return;

        for (int i = 0; i < garbageLines; i++)
        {
            StartCoroutine(AddTemporaryGarbageLine(targetBoard));
        }
    }

    IEnumerator AddTemporaryGarbageLine(BoardManager board)
    {
        int width = board.width;
        Transform[] newGarbage = new Transform[width];

        // Push existing blocks up in grid
        Piece[] allPieces = FindObjectsOfType<Piece>(true);

        foreach (Piece piece in allPieces)
        {
            // Only move pieces on this board
            if (Vector3.Distance(piece.transform.position, board.transform.position) > 20) continue;

            // Shift the whole piece up by 1
            piece.transform.position += Vector3.up;

            // Update the piece’s grid positions
            piece.UpdateGrid(); // Make sure UpdateGrid() in Piece is public
        }

        // Add solid garbage line at bottom
        for (int x = 0; x < width; x++)
        {
            Vector3 pos = board.transform.position + new Vector3(x, 0, 0);
            GameObject block = Instantiate(garbageBlockPrefab, pos, Quaternion.identity);
            newGarbage[x] = block.transform;
            board.grid[x, 0] = newGarbage[x];
        }

        // Wait garbageDuration
        yield return new WaitForSeconds(garbageDuration);

        // Remove garbage and shift everything above down
        for (int x = 0; x < width; x++)
        {
            if (newGarbage[x] != null)
            {
                Destroy(newGarbage[x].gameObject);
                board.grid[x, 0] = null;
            }
        }

        // Shift everything down after garbage disappears
        for (int y = 1; y < board.height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (board.grid[x, y] != null)
                {
                    board.grid[x, y - 1] = board.grid[x, y];
                    board.grid[x, y] = null;
                    board.grid[x, y - 1].position += Vector3.down;
                }
            }
        }
    }
}
