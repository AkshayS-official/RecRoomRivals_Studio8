using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Menu: Tools > RecRoom Rivals > Setup World Score Display
/// 1. Finds the "WorldScore" GameObject already in the scene.
/// 2. Adds WorldScoreDisplay component and creates its child TMP 3D texts.
/// 3. Auto-assigns it to GameFlowManager.worldScoreDisplay.
/// 4. Removes the old BallStand from the scene (no longer needed).
/// 5. Adds a BGM AudioSource to GameFlowManager if missing.
/// </summary>
public class WorldScoreSetup : Editor
{
    [MenuItem("Tools/RecRoom Rivals/Setup World Score Display")]
    static void Setup()
    {
        // ── 1. Find the WorldScore GameObject ─────────────────────────
        GameObject wsGO = GameObject.Find("WorldScore");
        if (wsGO == null)
        {
            EditorUtility.DisplayDialog("Not Found",
                "Could not find a GameObject named 'WorldScore' in the scene.\n\n" +
                "Make sure you have an empty GO named exactly 'WorldScore' placed above Ring (2).",
                "OK");
            return;
        }

        // ── 2. Add WorldScoreDisplay component if missing ─────────────
        WorldScoreDisplay wsd = wsGO.GetComponent<WorldScoreDisplay>();
        if (wsd == null)
            wsd = wsGO.AddComponent<WorldScoreDisplay>();

        // ── 3. Remove old auto-created children and rebuild them ──────
        // (clears stale defaults from a previous run)
        foreach (Transform child in wsGO.transform)
            GameObject.DestroyImmediate(child.gameObject);

        // Force-build the TMP children now in Editor (Awake won't run yet)
        wsd.playerScoreText = MakeTMP(wsGO.transform, "PlayerScore",
            new Vector3(-1.1f, 0f, 0f), 1.4f, new Color(0.3f, 0.8f, 1f));

        wsd.botScoreText = MakeTMP(wsGO.transform, "BotScore",
            new Vector3(1.1f, 0f, 0f), 1.4f, new Color(1f, 0.4f, 0.4f));

        wsd.timerText = MakeTMP(wsGO.transform, "Timer",
            new Vector3(0f, 0.9f, 0f), 1.0f, Color.yellow);

        wsd.statusText = MakeTMP(wsGO.transform, "Status",
            new Vector3(0f, -0.75f, 0f), 0.9f, Color.white);

        // Default preview text
        wsd.playerScoreText.text = "YOU\n0";
        wsd.botScoreText.text    = "BOT\n0";
        wsd.timerText.text       = "02:00";
        wsd.statusText.text      = "";

        // Hide all children by default (shown only when game is Playing)
        foreach (Transform child in wsGO.transform)
            child.gameObject.SetActive(false);

        EditorUtility.SetDirty(wsGO);

        // ── 4. Assign to GameFlowManager ──────────────────────────────
        GameFlowManager gfm = Object.FindFirstObjectByType<GameFlowManager>();
        if (gfm != null)
        {
            SerializedObject so = new SerializedObject(gfm);
            so.FindProperty("worldScoreDisplay").objectReferenceValue = wsd;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gfm);
            Debug.Log("[WorldScoreSetup] worldScoreDisplay assigned to GameFlowManager.");
        }
        else
        {
            Debug.LogWarning("[WorldScoreSetup] GameFlowManager not found — assign manually.");
        }

        // ── 5. Add BGM AudioSource to GameFlowManager if missing ──────
        if (gfm != null && gfm.bgmAudioSource == null)
        {
            // Add a second AudioSource (first is SFX, this is BGM)
            AudioSource bgmSrc = gfm.gameObject.AddComponent<AudioSource>();
            bgmSrc.playOnAwake = false;
            bgmSrc.loop = true;
            bgmSrc.volume = 0.35f;
            bgmSrc.spatialBlend = 0f;

            SerializedObject so = new SerializedObject(gfm);
            so.FindProperty("bgmAudioSource").objectReferenceValue = bgmSrc;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(gfm);
            Debug.Log("[WorldScoreSetup] BGM AudioSource added to GameFlowManager.");
        }

        // ── 6. Remove BallStand from scene (no longer used) ───────────
        GameObject ballStand = GameObject.Find("BallStand");
        if (ballStand != null)
        {
            Undo.DestroyObjectImmediate(ballStand);
            Debug.Log("[WorldScoreSetup] BallStand removed from scene.");
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("World Score Display Ready!",
            "✓ WorldScoreDisplay set up on '" + wsGO.name + "'\n" +
            "✓ Assigned to GameFlowManager\n" +
            (gfm?.bgmAudioSource != null ? "✓ BGM AudioSource added\n" : "") +
            (ballStand != null ? "✓ BallStand removed\n" : "  BallStand not found (already removed?)\n") +
            "\nNow assign your BGM clip to:\n" +
            "GameFlowManager → Background Music → Bgm Clip\n\n" +
            "Save the scene (Ctrl+S).",
            "OK");
    }

    // ── Helper ────────────────────────────────────────────────────────

    static TextMeshPro MakeTMP(Transform parent, string name,
        Vector3 localPos, float size, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
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
