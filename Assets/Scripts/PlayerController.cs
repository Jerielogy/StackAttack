using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    public Camera opponentCamera;

    // Track my specific spawner, board, and preview location
    private Spawner mySpawner;
    private BoardManager myBoard;
    private Transform nextPieceLocation;
    private GameObject currentPreviewObject; // The visual ghost in the box

    // SYNCVAR: The Server calculates this, and the Client sees it automatically
    // When it changes, "UpdateNextPieceUI" runs on the Client
    [SyncVar(hook = nameof(UpdateNextPieceUI))]
    public int nextPieceIndex = -1;

    public override void OnStartLocalPlayer()
    {
        // --- 1. SETUP CAMERAS & REFS ---
        if (opponentCamera == null)
        {
            GameObject camObj = GameObject.Find("OpponentCamera");
            if (camObj != null) opponentCamera = camObj.GetComponent<Camera>();
        }

        GameObject p1Obj = GameObject.Find("Spawner_P1");
        GameObject p2Obj = GameObject.Find("Spawner_P2");

        if (isServer)
        {
            // HOST (Player 1)
            if (p1Obj != null) mySpawner = p1Obj.GetComponent<Spawner>();

            // Find Board & Next Piece Border
            GameObject env1 = GameObject.Find("Environment_P1");
            if (env1 != null)
            {
                myBoard = env1.GetComponent<BoardManager>();
                // Find the Border inside the environment or by name
                Transform border = env1.transform.Find("NextPieceBorder_P1");
                if (border == null) border = GameObject.Find("NextPieceBorder_P1")?.transform;
                nextPieceLocation = border;
            }

            Camera.main.transform.position = new Vector3(6.5f, 12, -10);
            if (opponentCamera != null) opponentCamera.transform.position = new Vector3(69f, 12, -10);
        }
        else
        {
            // CLIENT (Player 2)
            if (p2Obj != null) mySpawner = p2Obj.GetComponent<Spawner>();

            GameObject env2 = GameObject.Find("Environment_P2");
            if (env2 != null)
            {
                myBoard = env2.GetComponent<BoardManager>();
                Transform border = env2.transform.Find("NextPieceBorder_P2");
                if (border == null) border = GameObject.Find("NextPieceBorder_P2")?.transform;
                nextPieceLocation = border;
            }

            Camera.main.transform.position = new Vector3(69f, 12, -10);
            Camera.main.orthographicSize = 14f;
            if (opponentCamera != null) opponentCamera.transform.position = new Vector3(6.5f, 12, -10);
        }

        // --- 2. START THE GAME ---
        CmdSpawnBlock();
    }

    // --- HOOK: Updates the UI when the server picks a new number ---
    void UpdateNextPieceUI(int oldIndex, int newIndex)
    {
        // 1. Clear the old preview
        if (currentPreviewObject != null) Destroy(currentPreviewObject);

        // 2. Safety Check
        if (mySpawner == null || nextPieceLocation == null) return;
        if (newIndex < 0 || newIndex >= mySpawner.tetrominoes.Length) return;

        // 3. Create the visual ghost
        GameObject prefab = mySpawner.tetrominoes[newIndex];
        currentPreviewObject = Instantiate(prefab, nextPieceLocation.position, Quaternion.identity);

        // 4. Make it a child of the Border so it moves with it
        currentPreviewObject.transform.SetParent(nextPieceLocation);
        currentPreviewObject.transform.localPosition = Vector3.zero; // Center it

        // 5. IMPORTANT: Disable the script so it doesn't fall!
        Piece pieceScript = currentPreviewObject.GetComponent<Piece>();
        if (pieceScript != null) pieceScript.enabled = false;

        // Disable Network components on the ghost
        if (currentPreviewObject.GetComponent<NetworkTransformReliable>() != null)
            currentPreviewObject.GetComponent<NetworkTransformReliable>().enabled = false;

        // Or just destroy it generically if you aren't sure which one you use
        NetworkBehaviour[] netComps = currentPreviewObject.GetComponents<NetworkBehaviour>();
        foreach (var comp in netComps) comp.enabled = false;
    }

    // --- 3. SPAWNING LOGIC ---
    [Command]
    public void CmdSpawnBlock()
    {
        // Identify Spawner
        Spawner targetSpawner = (connectionToClient.connectionId == 0)
            ? GameObject.Find("Spawner_P1").GetComponent<Spawner>()
            : GameObject.Find("Spawner_P2").GetComponent<Spawner>();

        // Initialization: If we haven't picked a "Next" piece yet, pick one now
        if (nextPieceIndex == -1)
        {
            nextPieceIndex = Random.Range(0, targetSpawner.tetrominoes.Length);
        }

        // 1. Spawn the CURRENT "Next" piece
        GameObject block = Instantiate(targetSpawner.tetrominoes[nextPieceIndex], targetSpawner.transform.position, Quaternion.identity);
        NetworkServer.Spawn(block, connectionToClient);

        // 2. Pick the NEW "Next" piece for the future
        nextPieceIndex = Random.Range(0, targetSpawner.tetrominoes.Length);
    }
}