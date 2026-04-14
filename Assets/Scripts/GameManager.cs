using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game Settings")]
    public float gameDuration = 120f;

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI botScoreText;
    public GameObject gameOverPanel;
    public TextMeshProUGUI gameOverText;

    [HideInInspector] public int playerScore = 0;
    [HideInInspector] public int botScore = 0;

    float timeLeft;
    bool gameActive = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        timeLeft = gameDuration;
        gameActive = true;
        if (gameOverPanel) gameOverPanel.SetActive(false);
        UpdateScoreUI();
    }

    void Update()
    {
        if (!gameActive) return;

        timeLeft -= Time.deltaTime;
        if (timerText != null)
        {
            int mins = Mathf.FloorToInt(timeLeft / 60);
            int secs = Mathf.FloorToInt(timeLeft % 60);
            timerText.text = $"{mins:00}:{secs:00}";
        }

        if (timeLeft <= 0f) EndGame();
    }

    public void AddScore(bool isPlayer)
    {
        if (!gameActive) return;
        if (isPlayer) playerScore++;
        else botScore++;
        UpdateScoreUI();
    }

    public bool IsGameActive() => gameActive;

    void UpdateScoreUI()
    {
        if (playerScoreText) playerScoreText.text = $"Player: {playerScore}";
        if (botScoreText) botScoreText.text = $"Bot: {botScore}";
    }

    void EndGame()
    {
        gameActive = false;
        string result = playerScore > botScore ? "🏆 Player Wins!" :
                        botScore > playerScore ? "🤖 Bot Wins!" : "It's a Tie!";
        if (timerText) timerText.text = "00:00";
        if (gameOverPanel) gameOverPanel.SetActive(true);
        if (gameOverText) gameOverText.text = result;
        Debug.Log("Game Over: " + result);
    }
}