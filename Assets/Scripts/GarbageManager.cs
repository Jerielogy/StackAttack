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
            StartCoroutine(AddTemporaryGarbageLine(targetBoard));
        }
    }

    IEnumerator AddTemporaryGarbageLine(BoardManager board)
    {
        // Shift existing blocks up
        Piece[] allPieces = FindObjectsOfType<Piece>(true);
        foreach (Piece piece in allPieces)
        {
            if (Vector3.Distance(piece.transform.position, board.transform.position) > 20) continue;
            piece.transform.position += Vector3.up;
            piece.UpdateGrid();
        }

        // Spawn Garbage Row
        Transform[] rowItems = new Transform[board.width];
        for (int x = 0; x < board.width; x++)
        {
            Vector3 pos = board.transform.position + new Vector3(x, 0, 0);
            GameObject block = Instantiate(garbageBlockPrefab, pos, Quaternion.identity);
            rowItems[x] = block.transform;
            board.grid[x, 0] = rowItems[x];
        }

        yield return new WaitForSeconds(garbageDuration);

        // Remove Garbage
        for (int x = 0; x < board.width; x++)
        {
            if (rowItems[x] != null)
            {
                Destroy(rowItems[x].gameObject);
                board.grid[x, 0] = null;
            }
        }

        // Shift everything back down
        for (int y = 1; y < board.height; y++)
        {
            board.DecreaseRow(y);
        }
    }
}