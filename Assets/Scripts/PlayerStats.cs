using UnityEngine;
using Mirror;
using TMPro;

public class PlayerStats : NetworkBehaviour
{
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI levelText;
    private GameObject gameOverObject;

    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score;

    [SyncVar(hook = nameof(OnLevelChanged))]
    public int level = 0; // Starts at 0 (Nintendo rules)

    [SyncVar]
    public int linesClearedTotal = 0;

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
            scoreText = canvas.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
            levelText = canvas.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();

            foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
            {
                if (t.name.ToLower().Contains("gameover"))
                {
                    gameOverObject = t.gameObject;
                    gameOverObject.SetActive(false);
                }
            }
            UpdateUI();
        }
    }

    [Command]
    public void CmdAddScore(int linesCleared)
    {
        int basePoints = linesCleared switch
        {
            1 => 40,
            2 => 100,
            3 => 300,
            4 => 1200,
            _ => 0
        };

        // Nintendo Formula: Score += Base * (Level + 1)
        score += basePoints * (level + 1);

        // Logic: Level up every 10 lines
        linesClearedTotal += linesCleared;
        if (linesClearedTotal >= (level + 1) * 10)
        {
            level++;
        }
    }

    void UpdateUI()
    {
        if (scoreText != null) scoreText.text = "SCORE: " + score;
        if (levelText != null) levelText.text = "LEVEL: " + level;
    }

    public void OnScoreChanged(int oldScore, int newScore) => UpdateUI();
    public void OnLevelChanged(int oldLevel, int newLevel) => UpdateUI();

    [Command]
    public void CmdTriggerGameOver() => TargetGameOver();

    [TargetRpc]
    public void TargetGameOver()
    {
        if (gameOverObject != null) gameOverObject.SetActive(true);
        if (scoreText != null) scoreText.color = Color.red;

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null) pc.enabled = false;
    }
}