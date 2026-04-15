using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Menu: Tools > RecRoom Rivals > Setup UI Canvas
/// Creates the full World Space HUD Canvas as a child of Camera Offset,
/// then auto-assigns all references in GameFlowManager.
/// </summary>
public class UISceneSetup : Editor
{
    [MenuItem("Tools/RecRoom Rivals/Setup UI Canvas")]
    static void CreateUICanvas()
    {
        // ── Find XR Rig ───────────────────────────────────────────────
        GameObject xrRig = GameObject.Find("Player (XR Rig)");
        if (xrRig == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Could not find 'Player (XR Rig)' in scene.\nMake sure it's in the hierarchy.",
                "OK");
            return;
        }

        Transform cameraOffset = xrRig.transform.Find("Camera Offset");
        if (cameraOffset == null)
        {
            EditorUtility.DisplayDialog("Error",
                "Could not find 'Camera Offset' under 'Player (XR Rig)'.",
                "OK");
            return;
        }

        // ── Replace existing canvas if any ────────────────────────────
        Transform existing = cameraOffset.Find("UI Canvas");
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog("UI Canvas Exists",
                "A 'UI Canvas' already exists under Camera Offset.\nReplace it?",
                "Replace", "Cancel");
            if (!replace) return;
            DestroyImmediate(existing.gameObject);
        }

        // ─────────────────────────────────────────────────────────────
        // CANVAS
        // Sits 2 m in front of the player rig, 1.8 × 1.0 m in world space
        // (1 unit = 1000 canvas pixels, so sizeDelta 1800×1000 = 1.8×1.0 m)
        // ─────────────────────────────────────────────────────────────
        GameObject canvasGO = new GameObject("UI Canvas");
        canvasGO.transform.SetParent(cameraOffset, false);
        canvasGO.transform.localPosition = new Vector3(0f, 0.1f, 2f);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale = Vector3.one * 0.001f;

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;

        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.dynamicPixelsPerUnit = 1f;
        canvasGO.AddComponent<GraphicRaycaster>();

        RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
        canvasRT.sizeDelta = new Vector2(1800f, 1000f);

        // ─────────────────────────────────────────────────────────────
        // PROMPT UI  (FreeRoam / Prompted states)
        // Semi-transparent bar at bottom-center
        // ─────────────────────────────────────────────────────────────
        GameObject promptUI = MakePanel(canvasRT, "PromptUI",
            new Vector2(0f, -380f), new Vector2(800f, 120f),
            new Color(0f, 0f, 0f, 0.75f));
        MakeTMP(promptUI.transform, "PromptText",
            "Poke the button to start!",
            fontSize: 55f,
            anchoredPos: Vector2.zero,
            size: new Vector2(760f, 100f),
            bold: true);
        promptUI.SetActive(false);

        // ─────────────────────────────────────────────────────────────
        // COUNTDOWN UI  (Countdown state)
        // Large centred panel
        // ─────────────────────────────────────────────────────────────
        GameObject countdownUI = MakePanel(canvasRT, "CountdownUI",
            new Vector2(0f, 0f), new Vector2(420f, 420f),
            new Color(0f, 0f, 0f, 0.65f));
        TextMeshProUGUI countdownText = MakeTMP(countdownUI.transform, "CountdownText",
            "3",
            fontSize: 220f,
            anchoredPos: Vector2.zero,
            size: new Vector2(400f, 400f),
            bold: true);
        countdownText.alignment = TextAlignmentOptions.Center;
        countdownUI.SetActive(false);

        // ─────────────────────────────────────────────────────────────
        // HUD — TIMER  (Playing state, top centre)
        // ─────────────────────────────────────────────────────────────
        TextMeshProUGUI timerText = MakeTMP(canvasRT, "TimerText",
            "02:00",
            fontSize: 100f,
            anchoredPos: new Vector2(0f, 440f),
            size: new Vector2(500f, 130f),
            bold: true);
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = Color.yellow;
        timerText.gameObject.SetActive(false);

        // ─────────────────────────────────────────────────────────────
        // HUD — PLAYER SCORE  (Playing state, top left)
        // ─────────────────────────────────────────────────────────────
        TextMeshProUGUI playerScoreText = MakeTMP(canvasRT, "PlayerScoreText",
            "You: 0",
            fontSize: 72f,
            anchoredPos: new Vector2(-680f, 440f),
            size: new Vector2(380f, 100f),
            bold: false);
        playerScoreText.alignment = TextAlignmentOptions.Left;
        playerScoreText.color = Color.white;
        playerScoreText.gameObject.SetActive(false);

        // ─────────────────────────────────────────────────────────────
        // HUD — BOT SCORE  (Playing state, top right)
        // ─────────────────────────────────────────────────────────────
        TextMeshProUGUI botScoreText = MakeTMP(canvasRT, "BotScoreText",
            "Bot: 0",
            fontSize: 72f,
            anchoredPos: new Vector2(680f, 440f),
            size: new Vector2(380f, 100f),
            bold: false);
        botScoreText.alignment = TextAlignmentOptions.Right;
        botScoreText.color = new Color(1f, 0.5f, 0.5f); // soft red for bot
        botScoreText.gameObject.SetActive(false);

        // ─────────────────────────────────────────────────────────────
        // GAME OVER UI  (GameOver state)
        // Central panel with result + high score
        // ─────────────────────────────────────────────────────────────
        GameObject gameOverUI = MakePanel(canvasRT, "GameOverUI",
            new Vector2(0f, 40f), new Vector2(950f, 540f),
            new Color(0f, 0f, 0f, 0.88f));

        TextMeshProUGUI gameOverText = MakeTMP(gameOverUI.transform, "GameOverText",
            "You Win!\nYou: 0  Bot: 0",
            fontSize: 85f,
            anchoredPos: new Vector2(0f, 80f),
            size: new Vector2(900f, 320f),
            bold: true);
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.color = Color.white;

        // Divider line (thin panel)
        MakePanel(gameOverUI.transform, "Divider",
            new Vector2(0f, -100f), new Vector2(800f, 4f),
            new Color(1f, 1f, 1f, 0.25f));

        TextMeshProUGUI highScoreText = MakeTMP(gameOverUI.transform, "HighScoreText",
            "Best: 0",
            fontSize: 52f,
            anchoredPos: new Vector2(0f, -140f),
            size: new Vector2(900f, 70f),
            bold: false);
        highScoreText.alignment = TextAlignmentOptions.Center;
        highScoreText.color = Color.yellow;

        // Restart button inside GameOver panel
        GameObject restartBtn = MakeButton(gameOverUI.transform, "RestartButton",
            "PLAY AGAIN",
            anchoredPos: new Vector2(0f, -230f),
            size: new Vector2(360f, 72f),
            bgColor: new Color(0.10f, 0.55f, 0.25f));

        gameOverUI.SetActive(false);

        // ─────────────────────────────────────────────────────────────
        // EVENT SYSTEM — required for mouse/controller UI clicks
        // ─────────────────────────────────────────────────────────────
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[UISetup] EventSystem added to GameScene.");
        }

        // ─────────────────────────────────────────────────────────────
        // AUTO-ASSIGN to GameFlowManager
        // ─────────────────────────────────────────────────────────────
        GameFlowManager gfm = Object.FindFirstObjectByType<GameFlowManager>();
        if (gfm != null)
        {
            SerializedObject so = new SerializedObject(gfm);
            so.FindProperty("challengePromptUI").objectReferenceValue  = promptUI;
            so.FindProperty("countdownUI").objectReferenceValue        = countdownUI;
            so.FindProperty("countdownText").objectReferenceValue      = countdownText;
            so.FindProperty("timerText").objectReferenceValue          = timerText;
            so.FindProperty("playerScoreText").objectReferenceValue    = playerScoreText;
            so.FindProperty("botScoreText").objectReferenceValue       = botScoreText;
            so.FindProperty("gameOverUI").objectReferenceValue         = gameOverUI;
            so.FindProperty("gameOverText").objectReferenceValue       = gameOverText;
            so.FindProperty("highScoreText").objectReferenceValue      = highScoreText;
            so.ApplyModifiedProperties();

            // Wire Restart button → GameFlowManager.RestartGame()
            var btn = restartBtn.GetComponent<Button>();
            if (btn != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    btn.onClick,
                    (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                        typeof(UnityEngine.Events.UnityAction),
                        gfm,
                        gfm.GetType().GetMethod("RestartGame")));
            }
            Debug.Log("[UISetup] All UI references auto-assigned to GameFlowManager.");
        }
        else
        {
            Debug.LogWarning("[UISetup] GameFlowManager not found — assign UI refs manually.");
        }

        // ── Add AudioSource to GameFlowManager if missing ─────────────
        if (gfm != null && gfm.audioSource == null)
        {
            AudioSource src = gfm.gameObject.GetComponent<AudioSource>();
            if (src == null) src = gfm.gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            SerializedObject so2 = new SerializedObject(gfm);
            so2.FindProperty("audioSource").objectReferenceValue = src;
            so2.ApplyModifiedProperties();
            Debug.Log("[UISetup] AudioSource added to GameFlowManager.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        string assignMsg = gfm != null
            ? "All refs auto-assigned to GameFlowManager!"
            : "⚠ GameFlowManager not found — assign refs manually.";

        EditorUtility.DisplayDialog("UI Canvas Created",
            $"Canvas created under Camera Offset.\n\n{assignMsg}\n\nSave the scene (Ctrl+S).",
            "OK");

        Debug.Log("[UISetup] Done! Save your scene.");
    }

    // ── Helpers ───────────────────────────────────────────────────────

    static GameObject MakePanel(Transform parent, string name,
        Vector2 anchoredPos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        Image img = go.AddComponent<Image>();
        img.color = color;
        return go;
    }

    static GameObject MakeButton(Transform parent, string name,
        string label, Vector2 anchoredPos, Vector2 size, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
        btn.colors = cb;
        // Label text
        MakeTMP(go.transform, "Label", label, 34f, Vector2.zero, size, bold: true);
        return go;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name,
        string defaultText, float fontSize, Vector2 anchoredPos, Vector2 size,
        bool bold)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = anchoredPos;
        rt.sizeDelta = size;
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = defaultText;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }
}
