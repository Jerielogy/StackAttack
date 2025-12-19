using UnityEngine;
using Mirror;
using TMPro;

public class PlayerStats : NetworkBehaviour
{
    private TextMeshProUGUI scoreText;
    private TextMeshProUGUI levelText;
    private GameObject gameOverPanel;

    [SyncVar(hook = nameof(OnScoreChanged))]
    public int score;

    [SyncVar(hook = nameof(OnLevelChanged))]
    public int level = 0;

    [SyncVar]
    public int linesClearedTotal = 0;

    [SyncVar]
    public int playerIndex;

    // ---------------- SERVER ----------------

    public override void OnStartServer()
    {
        playerIndex = connectionToClient.connectionId;
    }

    // ---------------- CLIENT ----------------

    public override void OnStartClient()
    {
        // Identify the correct canvas based on the player index
        string canvasName = (playerIndex == 0) ? "Canvas_P1" : "Canvas_P2";
        GameObject canvas = GameObject.Find(canvasName);
        if (canvas == null) return;

        scoreText = canvas.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
        levelText = canvas.transform.Find("LevelText")?.GetComponent<TextMeshProUGUI>();

        // Find the GameOver panel inside the canvas
        foreach (Transform t in canvas.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.ToLower().Contains("gameover"))
            {
                gameOverPanel = t.gameObject;
                gameOverPanel.SetActive(false);
            }
        }

        UpdateUI();
    }

    // ---------------- UNIFIED PROCESSOR ----------------

    [Command]
    public void CmdProcessLineClears(int linesCleared)
    {
        // 1. Calculate Score (Nintendo System)
        int basePoints = linesCleared switch
        {
            1 => 40,
            2 => 100,
            3 => 300,
            4 => 1200,
            _ => 0
        };

        score += basePoints * (level + 1);

        // 2. Process Leveling
        linesClearedTotal += linesCleared;
        while (linesClearedTotal >= (level + 1) * 10)
        {
            level++;
            Debug.Log($"Player {playerIndex} Leveled Up! Current Level: {level}");
        }

        // 3. Process Garbage Attack
        if (linesCleared > 1)
        {
            GarbageManager gm = FindObjectOfType<GarbageManager>();
            if (gm != null)
            {
                gm.SendGarbage(linesCleared, playerIndex);
            }
        }
    }

    // ---------------- UI UPDATES ----------------

    void UpdateUI()
    {
        if (scoreText != null)
            scoreText.text = "SCORE: " + score;

        if (levelText != null)
            levelText.text = "LEVEL: " + level;
    }

    void OnScoreChanged(int oldVal, int newVal) => UpdateUI();
    void OnLevelChanged(int oldVal, int newVal) => UpdateUI();

    // ---------------- GAME OVER ----------------

    [Command]
    public void CmdTriggerGameOver()
    {
        TargetShowGameOver(connectionToClient);
        if (GameStateManager.Instance != null)
            GameStateManager.Instance.RegisterGameOver();
    }

    [TargetRpc]
    void TargetShowGameOver(NetworkConnection target)
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);

        PlayerController pc = GetComponent<PlayerController>();
        if (pc != null)
            pc.enabled = false;
    }
}