using UnityEngine;
using UnityEngine.AI;
using TMPro;
using System.Collections;

public class GameFlowManager : MonoBehaviour
{
    public static GameFlowManager Instance;

    public enum GameState { FreeRoam, Prompted, Countdown, Playing, GameOver }
    public GameState currentState = GameState.FreeRoam;

    // ── Spawn Points ──────────────────────────────────────────────────
    [Header("Spawn Points (each needs PlayerSlot + BotSlot children)")]
    public Transform[] sharedSpawnPoints;
    public Transform playerFreeRoamStart;
    public Transform playerFreeRoamReturn;      // where player returns after game over

    // ── Core References ───────────────────────────────────────────────
    [Header("References")]
    public Transform playerTransform;
    public ShooterBotAgent botAgent;
    public NavMeshAgent botNavAgent;
    public Transform coachTransform;
    [Tooltip("Hoop the player should face when spawning (usually the player's own hoop)")]
    public Transform playerHoopTransform;

    // ── Ball Grabber ──────────────────────────────────────────────────
    [Header("Player Ball System")]
    [Tooltip("PlayerBallGrabber on the XR Rig — enabled only while game is Playing")]
    public PlayerBallGrabber playerBallGrabber;

    // ── Bot ───────────────────────────────────────────────────────────
    [Header("Bot Ball")]
    [Tooltip("Prefab used by the bot to spawn its own balls (assign same basketball prefab)")]
    public GameObject botBasketballPrefab;

    // ── World Score ───────────────────────────────────────────────────
    [Header("World Score Display")]
    [Tooltip("Empty GO above the hoop with WorldScoreDisplay component")]
    public WorldScoreDisplay worldScoreDisplay;

    // ── HUD UI (on Camera Offset Canvas) ─────────────────────────────
    [Header("HUD UI")]
    public GameObject challengePromptUI;
    public GameObject countdownUI;
    public TextMeshProUGUI countdownText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI botScoreText;
    public GameObject gameOverUI;
    public TextMeshProUGUI gameOverText;
    public TextMeshProUGUI highScoreText;

    // ── Settings ──────────────────────────────────────────────────────
    [Header("Settings")]
    public float gameDuration = 120f;
    public float coachPromptDistance = 3f;
    public float repositionDelay = 1.5f;

    // ── Background Music ──────────────────────────────────────────────
    [Header("Background Music")]
    [Tooltip("Separate AudioSource for BGM — plays as soon as the scene loads")]
    public AudioSource bgmAudioSource;
    public AudioClip bgmClip;
    [Range(0f, 1f)] public float bgmVolume = 0.35f;

    // ── SFX Audio ─────────────────────────────────────────────────────
    [Header("SFX Audio")]
    public AudioSource audioSource;
    public AudioClip crowdAmbientClip;
    public AudioClip crowdCheerClip;
    public AudioClip crowdBooClip;
    public AudioClip buzzerClip;
    public AudioClip countdownBeepClip;

    // ── Runtime State ─────────────────────────────────────────────────
    [HideInInspector] public int playerScore = 0;
    [HideInInspector] public int botScore = 0;

    const string HighScoreKey = "HighScore";
    float timeLeft;
    int currentSpawnIndex = 0;
    bool repositioning = false;

    // ─────────────────────────────────────────────────────────────────

    void Awake() => Instance = this;

    void Start()
    {
        // Place player at start, facing their hoop
        if (playerFreeRoamStart != null && playerTransform != null)
        {
            playerTransform.position = playerFreeRoamStart.position;
            FaceHoop(playerTransform);
        }

        // Initial UI state
        SetUIState(GameState.FreeRoam);
        UpdateHighScoreUI();

        // Scoreboard and ball grabber start disabled
        if (worldScoreDisplay != null) worldScoreDisplay.SetVisible(false);
        if (playerBallGrabber != null) playerBallGrabber.enabled = false;

        // Start BGM immediately on scene load
        PlayBGM();
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

    // ── Helpers ───────────────────────────────────────────────────────

    void FaceHoop(Transform t)
    {
        if (playerHoopTransform == null) return;
        Vector3 dir = playerHoopTransform.position - t.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.01f)
            t.rotation = Quaternion.LookRotation(dir);
    }

    // ── Coach Proximity ───────────────────────────────────────────────

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

    // ── Physical Button ───────────────────────────────────────────────

    /// <summary>Called by ChallengeButton when the player pokes it.</summary>
    public void StartChallengeFromButton()
    {
        if (currentState != GameState.Prompted && currentState != GameState.FreeRoam) return;
        StartCoroutine(StartChallenge());
    }

    // ── Challenge Flow ────────────────────────────────────────────────

    IEnumerator StartChallenge()
    {
        currentState = GameState.Countdown;
        SetUIState(GameState.Countdown);

        currentSpawnIndex = 0;
        yield return StartCoroutine(MoveToSpawnPoint(currentSpawnIndex));

        for (int i = 3; i > 0; i--)
        {
            if (countdownText) countdownText.text = i.ToString();
            PlayOneShot(countdownBeepClip);
            yield return new WaitForSeconds(1f);
        }
        if (countdownText) countdownText.text = "GO!";
        PlayOneShot(countdownBeepClip);
        yield return new WaitForSeconds(0.5f);

        playerScore = 0;
        botScore = 0;
        timeLeft = gameDuration;
        UpdateScoreUI();

        currentState = GameState.Playing;
        SetUIState(GameState.Playing);

        PlayAmbientCrowd();

        // Enable world scoreboard
        if (worldScoreDisplay != null) worldScoreDisplay.ShowGamePlay(0, 0, gameDuration);

        // Enable G-key ball grabber for player
        if (playerBallGrabber != null) playerBallGrabber.enabled = true;

        if (botAgent != null) botAgent.BeginRound();
    }

    // ── Reposition ────────────────────────────────────────────────────

    public void RequestReposition()
    {
        if (repositioning || currentState != GameState.Playing) return;
        StartCoroutine(RepositionRoutine());
    }

    IEnumerator RepositionRoutine()
    {
        repositioning = true;
        yield return new WaitForSeconds(repositionDelay);

        currentSpawnIndex = Random.Range(0, sharedSpawnPoints.Length);
        yield return StartCoroutine(MoveToSpawnPoint(currentSpawnIndex));

        if (botAgent != null) botAgent.BeginRound();
        repositioning = false;
    }

    IEnumerator MoveToSpawnPoint(int index)
    {
        Transform spawn = sharedSpawnPoints[index];
        Transform playerSlot = spawn.Find("PlayerSlot");
        Transform botSlot    = spawn.Find("BotSlot");

        Vector3 playerTarget = playerSlot != null ? playerSlot.position : spawn.position + Vector3.left;
        Vector3 botTarget    = botSlot    != null ? botSlot.position    : spawn.position + Vector3.right;

        // Teleport player, then face hoop
        if (playerTransform != null)
        {
            playerTransform.position = playerTarget;
            FaceHoop(playerTransform);
        }

        // Bot: walk via NavMesh if available, otherwise teleport
        bool botCanNav = botNavAgent != null && botNavAgent.isActiveAndEnabled && botNavAgent.isOnNavMesh;
        if (botCanNav)
        {
            botNavAgent.SetDestination(botTarget);
            float timeout = 5f;
            while (botNavAgent.isActiveAndEnabled && botNavAgent.isOnNavMesh
                   && botNavAgent.remainingDistance > 0.5f && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
        }
        else
        {
            if (botAgent != null) botAgent.transform.position = botTarget;
        }
    }

    // ── Timer ─────────────────────────────────────────────────────────

    void UpdateTimer()
    {
        timeLeft -= Time.deltaTime;
        int m = Mathf.FloorToInt(timeLeft / 60);
        int s = Mathf.FloorToInt(timeLeft % 60);
        string ts = $"{m:00}:{s:00}";
        if (timerText) timerText.text = ts;
        if (worldScoreDisplay != null) worldScoreDisplay.UpdateTimer(timeLeft);
        if (timeLeft <= 0f) EndGame();
    }

    // ── Scoring ───────────────────────────────────────────────────────

    public void AddScore(bool isPlayer)
    {
        if (currentState != GameState.Playing) return;
        if (isPlayer) playerScore++;
        else          botScore++;
        UpdateScoreUI();
        if (worldScoreDisplay != null) worldScoreDisplay.UpdateScore(playerScore, botScore);
        PlayOneShot(crowdCheerClip);
        AudienceReaction.TriggerCheer();
    }

    public bool IsGameActive() => currentState == GameState.Playing;

    void UpdateScoreUI()
    {
        if (playerScoreText) playerScoreText.text = $"You: {playerScore}";
        if (botScoreText)    botScoreText.text    = $"Bot: {botScore}";
    }

    // ── Game Over ─────────────────────────────────────────────────────

    void EndGame()
    {
        currentState = GameState.GameOver;
        SetUIState(GameState.GameOver);

        StopAmbientCrowd();
        PlayOneShot(buzzerClip);

        if (playerBallGrabber != null)
        {
            playerBallGrabber.DropBall();
            playerBallGrabber.enabled = false;
        }

        string result = playerScore > botScore ? "You Win!" :
                        botScore > playerScore ? "Bot Wins!" : "It's a Tie!";
        if (gameOverText) gameOverText.text = $"{result}\nYou: {playerScore}  Bot: {botScore}";

        if (worldScoreDisplay != null) worldScoreDisplay.ShowResult(playerScore, botScore);

        UpdateHighScore();

        // Return player to coach area after 3 s
        Invoke(nameof(ReturnPlayerToCoach), 3f);
    }

    void ReturnPlayerToCoach()
    {
        Transform ret = playerFreeRoamReturn != null ? playerFreeRoamReturn : playerFreeRoamStart;
        if (ret != null && playerTransform != null)
            playerTransform.position = ret.position;
        currentState = GameState.FreeRoam;
        SetUIState(GameState.FreeRoam);
        if (worldScoreDisplay != null) worldScoreDisplay.SetVisible(false);
    }

    // ── High Score ────────────────────────────────────────────────────

    void UpdateHighScore()
    {
        int current = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (playerScore > current)
        {
            PlayerPrefs.SetInt(HighScoreKey, playerScore);
            PlayerPrefs.Save();
        }
        UpdateHighScoreUI();
    }

    void UpdateHighScoreUI()
    {
        int hs = PlayerPrefs.GetInt(HighScoreKey, 0);
        if (highScoreText) highScoreText.text = $"Best: {hs}";
    }

    // ── Restart ───────────────────────────────────────────────────────

    public void RestartGame()
    {
        StopAllCoroutines();
        CancelInvoke();

        if (playerBallGrabber != null)
        {
            playerBallGrabber.DropBall();
            playerBallGrabber.enabled = false;
        }
        if (worldScoreDisplay != null) worldScoreDisplay.SetVisible(false);

        playerScore = 0;
        botScore = 0;
        repositioning = false;
        currentState = GameState.FreeRoam;
        SetUIState(GameState.FreeRoam);

        if (playerFreeRoamStart != null && playerTransform != null)
            playerTransform.position = playerFreeRoamStart.position;

        StopAmbientCrowd();
    }

    // ── UI State ──────────────────────────────────────────────────────

    void SetUIState(GameState state)
    {
        if (challengePromptUI) challengePromptUI.SetActive(state == GameState.Prompted);
        if (countdownUI)       countdownUI.SetActive(state == GameState.Countdown);
        if (gameOverUI)        gameOverUI.SetActive(state == GameState.GameOver);

        bool playing = state == GameState.Playing;
        if (timerText)       timerText.gameObject.SetActive(playing);
        if (playerScoreText) playerScoreText.gameObject.SetActive(playing);
        if (botScoreText)    botScoreText.gameObject.SetActive(playing);
    }

    // ── Audio ─────────────────────────────────────────────────────────

    void PlayBGM()
    {
        if (bgmAudioSource == null || bgmClip == null) return;
        bgmAudioSource.clip = bgmClip;
        bgmAudioSource.loop = true;
        bgmAudioSource.volume = bgmVolume;
        bgmAudioSource.playOnAwake = false;
        bgmAudioSource.Play();
    }

    void PlayOneShot(AudioClip clip)
    {
        if (audioSource != null && clip != null) audioSource.PlayOneShot(clip);
    }

    void PlayAmbientCrowd()
    {
        if (audioSource == null || crowdAmbientClip == null) return;
        audioSource.clip = crowdAmbientClip;
        audioSource.loop = true;
        audioSource.Play();
    }

    void StopAmbientCrowd()
    {
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
    }
}
