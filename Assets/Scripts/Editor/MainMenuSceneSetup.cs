using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Menu: Tools > RecRoom Rivals > Create Menu Scenes
/// Builds MainMenu.unity and Tutorial.unity with normal mouse input (no XR).
/// Adds all three scenes to Build Settings.
/// </summary>
public class MainMenuSceneSetup : Editor
{
    // ── Palette ───────────────────────────────────────────────────────
    static readonly Color BG_DARK    = new Color(0.05f, 0.07f, 0.12f, 1f);
    static readonly Color PANEL_CARD = new Color(0.08f, 0.12f, 0.20f, 1f);
    static readonly Color BTN_GREEN  = new Color(0.10f, 0.55f, 0.25f, 1f);
    static readonly Color BTN_BLUE   = new Color(0.10f, 0.38f, 0.75f, 1f);
    static readonly Color BTN_RED    = new Color(0.65f, 0.12f, 0.12f, 1f);
    static readonly Color TITLE_GOLD = new Color(1.0f, 0.85f, 0.20f, 1f);
    static readonly Color TEXT_WHITE = Color.white;
    static readonly Color TEXT_LIGHT = new Color(0.85f, 0.90f, 1.00f, 1f);
    static readonly Color DIVIDER    = new Color(1f, 1f, 1f, 0.12f);

    // ── Entry Point ───────────────────────────────────────────────────

    [MenuItem("Tools/RecRoom Rivals/Create Menu Scenes")]
    static void CreateMenuScenes()
    {
        string path = "Assets/Scenes";
        if (!System.IO.Directory.Exists(path))
            System.IO.Directory.CreateDirectory(path);

        CreateMainMenuScene(path);
        CreateTutorialScene(path);
        AddToBuildSettings(path);

        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done!",
            "Created:\n• Assets/Scenes/MainMenu.unity\n• Assets/Scenes/Tutorial.unity\n\n" +
            "All 3 scenes added to Build Settings (MainMenu = index 0).\n\n" +
            "Use File > Open Scene to preview them.",
            "OK");
    }

    // ─────────────────────────────────────────────────────────────────
    // MAIN MENU
    // ─────────────────────────────────────────────────────────────────

    static void CreateMainMenuScene(string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        // Camera
        CreateCamera(scene, BG_DARK);

        // EventSystem — required for mouse clicks (StandaloneInputModule, NO XR)
        CreateEventSystem(scene);

        // Canvas (Screen Space Overlay, 1920×1080 reference)
        var canvasRT = CreateScreenCanvas(scene, "Canvas");

        // ── Full-screen background ─────────────────────────────────
        var bg = MakeImage(canvasRT, "Background", Vector2.zero, Vector2.zero, BG_DARK);
        Stretch(bg);

        // ── Centre card ────────────────────────────────────────────
        var card = MakeImage(canvasRT, "CentreCard", new Vector2(0, 20), new Vector2(640, 620), PANEL_CARD);

        // Title
        var title = MakeTMP(card.transform, "Title", "REC ROOM\nRIVALS",
            72f, new Vector2(0, 210), new Vector2(600, 200), bold: true);
        title.color = TITLE_GOLD;

        // Subtitle
        var sub = MakeTMP(card.transform, "Subtitle", "BASKETBALL CHALLENGE",
            26f, new Vector2(0, 112), new Vector2(600, 46), bold: false);
        sub.color = TEXT_LIGHT;
        sub.characterSpacing = 5f;

        // Divider
        MakeImage(card.transform, "Divider", new Vector2(0, 70), new Vector2(500, 2), DIVIDER);

        // Buttons
        var startBtn = MakeButton(card.transform, "StartButton",  "▶  START GAME",  new Vector2(0, -20),  new Vector2(440, 80), BTN_GREEN);
        var tutBtn   = MakeButton(card.transform, "TutorialButton","?  HOW TO PLAY", new Vector2(0, -120), new Vector2(440, 80), BTN_BLUE);
        var quitBtn  = MakeButton(card.transform, "QuitButton",   "✕  QUIT",         new Vector2(0, -220), new Vector2(440, 80), BTN_RED);

        // Footer
        var footer = MakeTMP(canvasRT, "Footer", "Studio 8  ·  Rec Room Rivals",
            20f, new Vector2(0, -490), new Vector2(800, 36), bold: false);
        footer.color = new Color(1, 1, 1, 0.22f);

        // Manager + wire buttons
        var mgrGO = CreateGO(scene, "MainMenuManager");
        var mgr = mgrGO.AddComponent<MainMenuManager>();
        Wire(startBtn, mgr, "OnStartGame");
        Wire(tutBtn,   mgr, "OnTutorial");
        Wire(quitBtn,  mgr, "OnQuit");

        EditorSceneManager.SaveScene(scene, path + "/MainMenu.unity");
        EditorSceneManager.CloseScene(scene, true);
    }

    // ─────────────────────────────────────────────────────────────────
    // TUTORIAL
    // ─────────────────────────────────────────────────────────────────

    static void CreateTutorialScene(string path)
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

        CreateCamera(scene, BG_DARK);
        CreateEventSystem(scene);
        var canvasRT = CreateScreenCanvas(scene, "Canvas");

        // Background
        var bg = MakeImage(canvasRT, "Background", Vector2.zero, Vector2.zero, BG_DARK);
        Stretch(bg);

        // Main card
        var card = MakeImage(canvasRT, "TutorialCard", new Vector2(0, 10), new Vector2(1200, 820), PANEL_CARD);

        // Title
        var title = MakeTMP(card.transform, "Title", "HOW TO PLAY",
            56f, new Vector2(0, 355), new Vector2(1160, 72), bold: true);
        title.color = TITLE_GOLD;
        title.characterSpacing = 4f;

        MakeImage(card.transform, "TitleDivider", new Vector2(0, 305), new Vector2(1000, 2), DIVIDER);

        // ── LEFT column — What is Rec Room ─────────────────────────
        var leftCol = new GameObject("LeftColumn");
        leftCol.transform.SetParent(card.transform, false);
        var leftRT = leftCol.AddComponent<RectTransform>();
        leftRT.anchoredPosition = new Vector2(-295, -20);
        leftRT.sizeDelta = new Vector2(540, 560);

        var lh = MakeTMP(leftCol.transform, "Header", "WELCOME TO REC ROOM",
            24f, new Vector2(0, 245), new Vector2(520, 38), bold: true);
        lh.color = new Color(0.4f, 0.8f, 1f);

        var lb = MakeTMP(leftCol.transform, "Body",
            "Rec Room is a social VR space packed\n" +
            "with mini-games and activities.\n\n" +
            "Explore the arena and jump into any\n" +
            "activity you find!\n\n" +
            "────────────────────────────\n\n" +
            "BASKETBALL CHALLENGE\n\n" +
            "Head-to-head against an AI opponent\n" +
            "in a 2-minute shooting contest.\n\n" +
            "Score more baskets than the Bot\n" +
            "to WIN!",
            22f, new Vector2(0, -30), new Vector2(520, 480), bold: false);
        lb.color = TEXT_LIGHT;
        lb.alignment = TextAlignmentOptions.TopLeft;
        lb.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Column divider
        MakeImage(card.transform, "ColDivider", new Vector2(0, -20), new Vector2(2, 560), DIVIDER);

        // ── RIGHT column — Game Steps ──────────────────────────────
        var rightCol = new GameObject("RightColumn");
        rightCol.transform.SetParent(card.transform, false);
        var rightRT = rightCol.AddComponent<RectTransform>();
        rightRT.anchoredPosition = new Vector2(295, -20);
        rightRT.sizeDelta = new Vector2(540, 560);

        var rh = MakeTMP(rightCol.transform, "Header", "GAME STEPS",
            24f, new Vector2(0, 245), new Vector2(520, 38), bold: true);
        rh.color = new Color(0.4f, 0.8f, 1f);

        var rb = MakeTMP(rightCol.transform, "Body",
            "1.  Spawn at center court — free to roam\n\n" +
            "2.  Walk up to the Coach NPC\n\n" +
            "3.  A button appears — POKE it!\n" +
            "    (or press  E  on keyboard)\n\n" +
            "4.  Countdown:  3 … 2 … 1 … GO!\n\n" +
            "5.  Teleport to a shooting position\n\n" +
            "6.  Hold  T  to enter AIM mode\n" +
            "    A trajectory arc shows the ball path\n" +
            "    Hold longer = more throw force\n\n" +
            "7.  Press  G  to THROW along the arc!\n" +
            "    Release T before G to cancel aim\n\n" +
            "8.  Aim for YOUR hoop — score points!\n\n" +
            "9.  Bot shoots at the same time\n\n" +
            "10. Positions change every round\n\n" +
            "11. 2-min timer — most baskets WINS!",
            22f, new Vector2(0, -30), new Vector2(520, 480), bold: false);
        rb.color = TEXT_LIGHT;
        rb.alignment = TextAlignmentOptions.TopLeft;
        rb.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Controls strip
        MakeImage(card.transform, "ControlsBG", new Vector2(0, -353), new Vector2(1160, 68), new Color(0, 0, 0, 0.35f));
        var ctrl = MakeTMP(card.transform, "Controls",
            "Desktop:  Hold  T  to aim (see arc + charge force)  →  Press  G  to throw     |     VR:  Pull trigger to aim  →  Press grip to throw",
            20f, new Vector2(0, -353), new Vector2(1140, 60), bold: false);
        ctrl.color = new Color(1, 1, 1, 0.55f);

        // Back button
        var backBtn = MakeButton(canvasRT, "BackButton", "← BACK",
            new Vector2(-480, -460), new Vector2(220, 58), BTN_BLUE);

        var mgrGO = CreateGO(scene, "TutorialManager");
        var mgr = mgrGO.AddComponent<TutorialManager>();
        Wire(backBtn, mgr, "OnBack");

        EditorSceneManager.SaveScene(scene, path + "/Tutorial.unity");
        EditorSceneManager.CloseScene(scene, true);
    }

    // ─────────────────────────────────────────────────────────────────
    // BUILD SETTINGS
    // ─────────────────────────────────────────────────────────────────

    static void AddToBuildSettings(string scenesPath)
    {
        var desired = new[]
        {
            scenesPath + "/MainMenu.unity",
            scenesPath + "/Tutorial.unity",
            "Assets/Scenes/GameScene.unity",
        };

        // Collect existing entries, removing old versions of ours
        var keep = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var e in EditorBuildSettings.scenes)
        {
            bool ours = System.Array.Exists(desired, d => d == e.path);
            if (!ours) keep.Add(e);
        }

        // Prepend our scenes
        var final = new System.Collections.Generic.List<EditorBuildSettingsScene>();
        foreach (var d in desired)
            final.Add(new EditorBuildSettingsScene(d, true));
        final.AddRange(keep);

        EditorBuildSettings.scenes = final.ToArray();
        Debug.Log("[SceneSetup] Build Settings updated. MainMenu = index 0.");
    }

    // ─────────────────────────────────────────────────────────────────
    // HELPERS
    // ─────────────────────────────────────────────────────────────────

    static GameObject CreateGO(Scene scene, string name)
    {
        var go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    static void CreateCamera(Scene scene, Color bg)
    {
        var go = CreateGO(scene, "Main Camera");
        go.tag = "MainCamera";
        var cam = go.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = bg;
        cam.orthographic = true;
        go.AddComponent<AudioListener>();
    }

    /// <summary>Standard mouse EventSystem — NO XR components.</summary>
    static void CreateEventSystem(Scene scene)
    {
        var go = CreateGO(scene, "EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<StandaloneInputModule>();
    }

    static RectTransform CreateScreenCanvas(Scene scene, string name)
    {
        var go = CreateGO(scene, name);
        var c = go.AddComponent<Canvas>();
        c.renderMode = RenderMode.ScreenSpaceOverlay;
        var cs = go.AddComponent<CanvasScaler>();
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go.GetComponent<RectTransform>();
    }

    static RectTransform MakeImage(Transform parent, string name, Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        go.AddComponent<Image>().color = color;
        return rt;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static TextMeshProUGUI MakeTMP(Transform parent, string name, string text,
        float size, Vector2 pos, Vector2 sizeDelta, bool bold)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = TEXT_WHITE;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TMPro.TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        if (bold) tmp.fontStyle = FontStyles.Bold;
        return tmp;
    }

    static GameObject MakeButton(Transform parent, string name, string label,
        Vector2 pos, Vector2 size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = color;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;
        var cb = btn.colors;
        cb.normalColor      = Color.white;
        cb.highlightedColor = new Color(1.25f, 1.25f, 1.25f);
        cb.pressedColor     = new Color(0.75f, 0.75f, 0.75f);
        btn.colors = cb;

        var txt = MakeTMP(go.transform, "Label", label, 28f, Vector2.zero, size, bold: true);
        txt.color = Color.white;
        return go;
    }

    static void Wire(GameObject btnGO, MonoBehaviour target, string method)
    {
        var btn = btnGO.GetComponent<Button>();
        if (btn == null) return;
        var mi = target.GetType().GetMethod(method);
        if (mi == null) { Debug.LogWarning("[SceneSetup] Method not found: " + method); return; }
        UnityEditor.Events.UnityEventTools.AddPersistentListener(
            btn.onClick,
            (UnityEngine.Events.UnityAction)System.Delegate.CreateDelegate(
                typeof(UnityEngine.Events.UnityAction), target, mi));
    }
}
