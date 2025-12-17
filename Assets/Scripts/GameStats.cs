using UnityEngine;
using TMPro;

public class GameStats : MonoBehaviour
{
    public static GameStats instance;

    [Header("UI References")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI levelText;
    public GameObject gameOverText; // NEW: Drag your Game Over Text here

    [Header("Game Data")]
    public int score = 0;
    public int level = 1;
    public int linesClearedTotal = 0;

    public float fallSpeed = 1.0f;

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        UpdateUI();
    }

    public void AddScore(int linesCleared)
    {
        linesClearedTotal += linesCleared;

        int baseScore = 0;
        switch (linesCleared)
        {
            case 1: baseScore = 40; break;
            case 2: baseScore = 100; break;
            case 3: baseScore = 300; break;
            case 4: baseScore = 1200; break;
        }

        score += baseScore * level;

        if (linesClearedTotal >= level * 10)
        {
            level++;
            fallSpeed *= 0.9f;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        scoreText.text = "SCORE: " + score;
        levelText.text = "LEVEL: " + level;
    }

    // NEW FUNCTION: Handles Game Over
    public void TriggerGameOver()
    {
        // 1. Show the Game Over text
        if (gameOverText != null)
        {
            gameOverText.SetActive(true);
        }

        // 2. Optional: Stop the Spawner from making more pieces
        Spawner spawner = FindObjectOfType<Spawner>();
        if (spawner != null)
        {
            spawner.enabled = false;
        }

    }
}