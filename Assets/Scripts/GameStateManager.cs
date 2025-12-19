using UnityEngine;
using Mirror;

public class GameStateManager : NetworkBehaviour
{
    public static GameStateManager Instance;

    [SyncVar]
    private int playersGameOver = 0;

    [SyncVar]
    private int totalPlayers = 0;

    void Awake()
    {
        Instance = this;
    }

    public override void OnStartServer()
    {
        playersGameOver = 0;
        totalPlayers = NetworkServer.connections.Count;
    }

    [Server]
    public void RegisterGameOver()
    {
        playersGameOver++;

        if (playersGameOver >= totalPlayers)
        {
            RpcEnableRestartButtons();
        }
    }

    [ClientRpc]
    void RpcEnableRestartButtons()
    {
        GameOverButtons[] buttons = FindObjectsOfType<GameOverButtons>(true);
        foreach (var b in buttons)
            b.EnableRestartButton();
    }
}
