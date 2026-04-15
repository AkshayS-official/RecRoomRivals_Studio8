using UnityEngine;
using UnityEngine.AI;
using TMPro;
using System.Collections;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    public enum GameState { FreeRoam, Prompted, Countdown, Playing, GameOver }
    public GameState currentState = GameState.FreeRoam;

    [Header("Spawn Points (shared, each has PlayerSlot + BotSlot children)")]
    public Transform[] sharedSpawnPoints;
    public Transform playerFreeRoamStart;

    [Header("References")]
    public Transform playerTransform;
    public ShooterBotAgent botAgent;
    public NavMeshAgent botNavAgent;       // for smooth bot movement
    public Transform coachTransform;       // the coach NPC at center court
    public GameObject ballStandPrefab;
    public GameObject basketballPrefab;

    [Header("UI")]
    public GameObject challengePromptUI;
    public GameObject countdownUI;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI botScoreText;
    public GameObject gameOverUI;
    public TextMeshProUGUI gameOverText;

    [Header("Settings")]
    public float gameDuration = 120f;
    public float coachPromptDistance = 3f;
    public float repositionDelay = 1.5f;   // delay after throw before moving

    [HideInInspector] public int playerScore = 0;
    [HideInInspector] public int botScore = 0;

    float timeLeft;
    int currentSpawnIndex = 0;
    GameObject currentStand;
    bool repositioning = false;

    void Awake() => Instance = this;

    void Start()
    {
        if (playerFreeRoamStart != null && playerTransform != null)
            playerTransform.position = playerFreeRoamStart.position;
        SetUIState(GameState.FreeRoam);
    }

    void Update()
    {
        switch (currentState)
        {
            case GameState.FreeRoam:
            case GameState.Prompted:
                CheckCoachProximity();
                if (currentState == GameState.Prompted && Input.GetKeyDown(KeyCode.E))
                    StartCoroutine(StartChallenge());
                break;

            case GameState.Playing:
                UpdateTimer();
                break;
        }
    }

    void CheckCoachProximity()
    {
        if (coachTransform == null || playerTransform == null) return;
        float dist = Vector3.Distance(playerTransform.position, coachTransform.position);
        bool near = dist < coachPromptDistance;

        if (near && currentState == GameState.FreeRoam)
        {
            currentState = GameState.Prompted;
            SetUIState(GameState.Prompted);
        }
        else if (!near && currentState == GameState.Prompted)
        {
            currentState = GameState.FreeRoam;
            SetUIState(GameState.FreeRoam);
        }
    }

    IEnumerator StartChallenge()
    {
        currentState = GameState.Countdown;
        SetUIState(GameState.Countdown);

        // Pick first spawn point
        currentSpawnIndex = 0;
        yield return StartCoroutine(MoveToSpawnPoint(currentSpawnIndex));

        // Countdown
        for (int i = 3; i > 0; i--)
        {
            if (countdownText) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }
        if (countdownText) countdownText.text = "GO!";
        yield return new WaitForSeconds(0.5f);

        // Start game
        playerScore = 0;
        botScore = 0;
        timeLeft = gameDuration;
        UpdateScoreUI();
        currentState = GameState.Playing;
        SetUIState(GameState.Playing);

        SpawnBallStand(currentSpawnIndex);
        if (botAgent != null) botAgent.BeginRound();
    }

    // Called after BOTH player and bot finish their throws at current position
    public void RequestReposition()
    {
        if (repositioning || currentState != GameState.Playing) return;
        StartCoroutine(RepositionRoutine());
    }

    IEnumerator RepositionRoutine()
    {
        repositioning = true;
        yield return new WaitForSeconds(repositionDelay);

        // Destroy old stand
        if (currentStand != null) Destroy(currentStand);

        // Move to next spawn point
        currentSpawnIndex = Random.Range(0, sharedSpawnPoints.Length);
        yield return StartCoroutine(MoveToSpawnPoint(currentSpawnIndex));

        // Spawn new ball stand
        SpawnBallStand(currentSpawnIndex);

        // Tell bot to start next round
        if (botAgent != null) botAgent.BeginRound();

        repositioning = false;
    }

    IEnumerator MoveToSpawnPoint(int index)
    {
        Transform spawn = sharedSpawnPoints[index];
        Transform playerSlot = spawn.Find("PlayerSlot");
        Transform botSlot = spawn.Find("BotSlot");

        Vector3 playerTarget = playerSlot != null ? playerSlot.position : spawn.position + Vector3.left;
        Vector3 botTarget = botSlot != null ? botSlot.position : spawn.position + Vector3.right;

        // Move bot via NavMesh (smooth walk)
        if (botNavAgent != null && botNavAgent.isOnNavMesh)
        {
            botNavAgent.SetDestination(botTarget);
        }

        // Teleport player (XR rig can't navmesh-walk easily)
        if (playerTransform != null)
            playerTransform.position = playerTarget;

        // Wait for bot to arrive
        if (botNavAgent != null)
        {
            float timeout = 5f;
            while (botNavAgent.remainingDistance > 0.5f && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            // fallback: just move instantly
            if (botAgent != null) botAgent.transform.position = botTarget;
        }
    }

    public void StartChallengeFromButton()
    {
        if (currentState != GameState.Prompted) return;
        Debug.Log("Starting challenge from button press!");
        StartCoroutine(StartChallenge());
    }

    void SpawnBallStand(int spawnIndex)
    {
        if (currentStand != null) Destroy(currentStand);
        if (ballStandPrefab == null) return;

        Transform spawn = sharedSpawnPoints[spawnIndex];
        Transform playerSlot = spawn.Find("PlayerSlot");
        Vector3 standPos = playerSlot != null
            ? playerSlot.position + playerSlot.right * 1.5f
            : spawn.position + Vector3.left * 2f;
        standPos.y = 0f;

        currentStand = Instantiate(ballStandPrefab, standPos, Quaternion.identity);

        for (int i = 0; i < currentStand.transform.childCount && i < 4; i++)
        {
            Transform pt = currentStand.transform.GetChild(i);
            if (basketballPrefab != null)
            {
                GameObject ball = Instantiate(basketballPrefab, pt.position, pt.rotation);
                ball.transform.localScale = Vector3.one; // FORCE scale 1,1,1
                Rigidbody rb = ball.GetComponent<Rigidbody>();
                if (rb != null) rb.isKinematic = true;
            }
        }
    }

    void UpdateTimer()
    {
        timeLeft -= Time.deltaTime;
        int mins = Mathf.FloorToInt(timeLeft / 60);
        int secs = Mathf.FloorToInt(timeLeft % 60);
        if (timerText) timerText.text = $"{mins:00}:{secs:00}";
        if (timeLeft <= 0f) EndGame();
    }

    public void AddScore(bool isPlayer)
    {
        if (currentState != GameState.Playing) return;
        if (isPlayer) playerScore++;
        else botScore++;
        UpdateScoreUI();
    }

    public bool IsGameActive() => currentState == GameState.Playing;

    void UpdateScoreUI()
    {
        if (playerScoreText) playerScoreText.text = $"Player: {playerScore}";
        if (botScoreText) botScoreText.text = $"Bot: {botScore}";
    }

    void EndGame()
    {
        currentState = GameState.GameOver;
        SetUIState(GameState.GameOver);
        string result = playerScore > botScore ? "🏆 You Win!" :
                        botScore > playerScore ? "🤖 Bot Wins!" : "It's a Tie!";
        if (gameOverText) gameOverText.text = $"{result}\nYou: {playerScore}  Bot: {botScore}";
        if (currentStand != null) Destroy(currentStand);
    }

    void SetUIState(GameState state)
    {
        if (challengePromptUI) challengePromptUI.SetActive(state == GameState.Prompted);
        if (countdownUI) countdownUI.SetActive(state == GameState.Countdown);
        if (gameOverUI) gameOverUI.SetActive(state == GameState.GameOver);
        bool playing = state == GameState.Playing;
        if (timerText) timerText.gameObject.SetActive(playing);
        if (playerScoreText) playerScoreText.gameObject.SetActive(playing);
        if (botScoreText) botScoreText.gameObject.SetActive(playing);
    }

    public void RestartGame()
    {
        StopAllCoroutines();
        playerScore = 0;
        botScore = 0;
        repositioning = false;
        currentState = GameState.FreeRoam;
        SetUIState(GameState.FreeRoam);
        if (playerFreeRoamStart != null)
            playerTransform.position = playerFreeRoamStart.position;
        if (currentStand != null) Destroy(currentStand);
    }
}