using UnityEngine;
using Mirror;
using TMPro; // Keep using TMPro

public class PlayerStats : NetworkBehaviour
{
    private TextMeshProUGUI scoreText;
    private GameObject gameOverObject;

    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score;

    [SyncVar]
    public int playerIndex;

    public override void OnStartServer()
    {
        playerIndex = connectionToClient.connectionId;
    }

    public override void OnStartClient()
    {
        string canvasName = (playerIndex == 0) ? "Canvas_P1" : "Canvas_P2";
        GameObject canvas = GameObject.Find(canvasName);

        if (canvas != null)
        {
            Transform scoreTrans = canvas.transform.Find("ScoreText");
            if (scoreTrans != null)
            {
                scoreText = scoreTrans.GetComponent<TextMeshProUGUI>();
                if (scoreText != null) scoreText.text = score.ToString();
            }

            Transform overTrans = canvas.transform.Find("GameOverText");
            if (overTrans != null)
            {
                gameOverObject = overTrans.gameObject;
                gameOverObject.SetActive(false);
            }
        }
    }

    public void OnScoreChanged(int oldScore, int newScore)
    {
        if (scoreText != null) scoreText.text = newScore.ToString();
    }

    [Command]
    public void CmdAddScore(int amount)
    {
        score += amount;
    }

    // --- NEW: DESTROY CHILD BLOCK LOGIC ---

    [Command]
    public void CmdDestroyChild(NetworkIdentity parentId, int childIndex)
    {
        // Tell all clients to run this function
        RpcDestroyChild(parentId, childIndex);
    }

    [ClientRpc]
    void RpcDestroyChild(NetworkIdentity parentId, int childIndex)
    {
        if (parentId != null)
        {
            // Find the child at that specific index
            if (childIndex >= 0 && childIndex < parentId.transform.childCount)
            {
                Transform child = parentId.transform.GetChild(childIndex);
                Destroy(child.gameObject);
            }
        }
    }

    [TargetRpc]
    public void TargetGameOver()
    {
        if (gameOverObject != null) gameOverObject.SetActive(true);
        if (scoreText != null) scoreText.color = Color.red;
    }
}