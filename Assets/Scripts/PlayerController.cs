using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    public Camera opponentCamera;
    private Spawner mySpawner;
    private Transform nextPieceLocation;
    private GameObject currentPreviewObject;

    [SyncVar(hook = nameof(UpdateNextPieceUI))]
    public int nextPieceIndex = -1;

    public override void OnStartLocalPlayer()
    {
        // Setup Cameras
        if (opponentCamera == null)
        {
            GameObject camObj = GameObject.Find("OpponentCamera");
            if (camObj != null) opponentCamera = camObj.GetComponent<Camera>();
        }

        if (isServer) // Player 1
        {
            mySpawner = GameObject.Find("Spawner_P1")?.GetComponent<Spawner>();
            GameObject env1 = GameObject.Find("Environment_P1");
            if (env1 != null) nextPieceLocation = env1.transform.Find("NextPieceBorder_P1");
            Camera.main.transform.position = new Vector3(6.5f, 12, -10);
            if (opponentCamera != null) opponentCamera.transform.position = new Vector3(69f, 12, -10);
        }
        else // Player 2
        {
            mySpawner = GameObject.Find("Spawner_P2")?.GetComponent<Spawner>();
            GameObject env2 = GameObject.Find("Environment_P2");
            if (env2 != null) nextPieceLocation = env2.transform.Find("NextPieceBorder_P2");
            Camera.main.transform.position = new Vector3(69f, 12, -10);
            Camera.main.orthographicSize = 16f;
            if (opponentCamera != null) opponentCamera.transform.position = new Vector3(6.5f, 12, -10);
        }

        CmdSpawnBlock();
    }

    void UpdateNextPieceUI(int oldIndex, int newIndex)
    {
        if (currentPreviewObject != null) Destroy(currentPreviewObject);
        if (mySpawner == null || nextPieceLocation == null || newIndex < 0) return;

        currentPreviewObject = Instantiate(mySpawner.tetrominoes[newIndex], nextPieceLocation.position, Quaternion.identity);

        // 1. Parent it to the UI Border
        currentPreviewObject.transform.SetParent(nextPieceLocation);

        // 2. Center it locally
        currentPreviewObject.transform.localPosition = Vector3.zero;

        // 3. Shrink it down (Adjust 0.3f if it is still too big/small)
        currentPreviewObject.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

        // 4. Disable logic so it doesn't fall or cause Game Over
        Piece p = currentPreviewObject.GetComponent<Piece>();
        if (p != null) p.enabled = false;

        // Disable Mirror components on the visual ghost
        foreach (var comp in currentPreviewObject.GetComponents<NetworkBehaviour>()) comp.enabled = false;
    }

    [Command]
    public void CmdSpawnBlock()
    {
        Spawner targetSpawner = (connectionToClient.connectionId == 0)
            ? GameObject.Find("Spawner_P1").GetComponent<Spawner>()
            : GameObject.Find("Spawner_P2").GetComponent<Spawner>();

        if (nextPieceIndex == -1) nextPieceIndex = Random.Range(0, targetSpawner.tetrominoes.Length);

        GameObject block = Instantiate(targetSpawner.tetrominoes[nextPieceIndex], targetSpawner.transform.position, Quaternion.identity);
        NetworkServer.Spawn(block, connectionToClient);

        nextPieceIndex = Random.Range(0, targetSpawner.tetrominoes.Length);
    }
}