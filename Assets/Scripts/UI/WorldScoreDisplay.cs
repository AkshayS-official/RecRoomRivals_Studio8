using UnityEngine;
using TMPro;

/// <summary>
/// World-space scoreboard. Place an empty GameObject above the hoop,
/// add this component — it auto-creates its own child TMP 3D text objects.
/// GameFlowManager calls the public API to update it.
///
/// Assign this GO to GameFlowManager.worldScoreDisplay in the Inspector.
/// </summary>
public class WorldScoreDisplay : MonoBehaviour
{
    [Header("Text References (auto-created on Start if null)")]
    public TextMeshPro playerScoreText;
    public TextMeshPro botScoreText;
    public TextMeshPro timerText;
    public TextMeshPro statusText;

    [Header("Display Settings")]
    [Tooltip("Base font size for score digits")]
    public float scoreTextSize = 1.2f;
    [Tooltip("Rotate each frame so the display faces the camera")]
    public bool billboardToCamera = true;

    Camera mainCam;

    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        mainCam = Camera.main;
        BuildTextsIfMissing();
        SetVisible(false);
    }

    void LateUpdate()
    {
        if (!billboardToCamera) return;
        if (mainCam == null) mainCam = Camera.main;
        if (mainCam == null) return;

        // Y-axis-only billboard: TMP 3D visible face is on -Z, so point +Z
        // *away* from camera so the front of the text faces the player.
        Vector3 dir = mainCam.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(-dir);
    }

    // ── Public API (called by GameFlowManager) ────────────────────────

    public void UpdateScore(int player, int bot)
    {
        if (playerScoreText) playerScoreText.text = $"YOU\n{player}";
        if (botScoreText)    botScoreText.text    = $"BOT\n{bot}";
    }

    public void UpdateTimer(float timeLeft)
    {
        if (timerText == null) return;
        int m = Mathf.FloorToInt(timeLeft / 60);
        int s = Mathf.FloorToInt(timeLeft % 60);
        timerText.text = $"{m:00}:{s:00}";
    }

    public void ShowGamePlay(int player, int bot, float timeLeft)
    {
        SetVisible(true);
        if (statusText) statusText.text = "";
        UpdateScore(player, bot);
        UpdateTimer(timeLeft);
    }

    public void ShowResult(int player, int bot)
    {
        SetVisible(true);
        string msg = player > bot ? "YOU WIN!" : bot > player ? "BOT WINS!" : "TIE!";
        if (statusText) statusText.text = msg;
        UpdateScore(player, bot);
        if (timerText) timerText.text = "00:00";
    }

    public void SetVisible(bool visible)
    {
        foreach (Transform child in transform)
            child.gameObject.SetActive(visible);
    }

    // ── Auto-build child TMP objects ──────────────────────────────────

    void BuildTextsIfMissing()
    {
        // YOU score — left of centre
        if (playerScoreText == null)
            playerScoreText = MakeTMP("PlayerScore",
                new Vector3(-1.1f, 0f, 0f), scoreTextSize * 1.4f,
                new Color(0.3f, 0.8f, 1f));

        // BOT score — right of centre
        if (botScoreText == null)
            botScoreText = MakeTMP("BotScore",
                new Vector3(1.1f, 0f, 0f), scoreTextSize * 1.4f,
                new Color(1f, 0.4f, 0.4f));

        // Timer — top centre
        if (timerText == null)
            timerText = MakeTMP("Timer",
                new Vector3(0f, 0.8f, 0f), scoreTextSize * 1.0f,
                Color.yellow);

        // Status (WIN/LOSE/TIE) — bottom centre
        if (statusText == null)
            statusText = MakeTMP("Status",
                new Vector3(0f, -0.7f, 0f), scoreTextSize * 0.9f,
                Color.white);
    }

    TextMeshPro MakeTMP(string name, Vector3 localPos, float size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(transform, false);
        go.transform.localPosition = localPos;
        var tmp = go.AddComponent<TextMeshPro>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        return tmp;
    }
}
