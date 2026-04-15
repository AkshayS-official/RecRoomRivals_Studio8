using UnityEngine;
using UnityEditor;

/// <summary>
/// Menu: Tools > RecRoom Rivals > Organize Project Files
/// Moves scripts into logical subfolders.
/// Uses AssetDatabase.MoveAsset so GUIDs (and scene references) are preserved.
/// </summary>
public class ProjectOrganizer : Editor
{
    const string ROOT = "Assets/Scripts";

    // Map each script filename to its destination subfolder
    static readonly (string file, string folder)[] MOVES =
    {
        // ── Core ──────────────────────────────────────────────────────
        ("GameFlowManager.cs",   "Core"),
        ("GameManager.cs",       "Core"),
        ("ScoreDetector.cs",     "Core"),

        // ── Player ────────────────────────────────────────────────────
        ("PlayerController.cs",  "Player"),
        ("BasketballBall.cs",    "Player"),
        ("PlayerBallGrabber.cs", "Player"),

        // ── Bot ───────────────────────────────────────────────────────
        ("ShooterBotAgent.cs",   "Bot"),
        ("TrainingManager.cs",   "Bot"),
        ("ThrowVisualizer.cs",   "Bot"),

        // ── NPC ───────────────────────────────────────────────────────
        ("CoachNPC.cs",          "NPC"),
        ("AudienceReaction.cs",  "NPC"),

        // ── UI ────────────────────────────────────────────────────────
        ("ChallengeButton.cs",   "UI"),
        ("MainMenuManager.cs",   "UI"),
        ("TutorialManager.cs",   "UI"),
        ("WorldScoreDisplay.cs", "UI"),
    };

    [MenuItem("Tools/RecRoom Rivals/Organize Project Files")]
    static void Organize()
    {
        bool ok = EditorUtility.DisplayDialog(
            "Organize Project Files",
            "Scripts will be moved into:\n\n" +
            "  Scripts/Core    → GameFlowManager, GameManager, ScoreDetector\n" +
            "  Scripts/Player  → PlayerController, BasketballBall, PlayerBallGrabber\n" +
            "  Scripts/Bot     → ShooterBotAgent, TrainingManager, ThrowVisualizer\n" +
            "  Scripts/NPC     → CoachNPC, AudienceReaction\n" +
            "  Scripts/UI      → ChallengeButton, Menus, WorldScoreDisplay\n\n" +
            "Scene references are preserved (GUIDs don't change).\n\n" +
            "Proceed?",
            "Yes, Organize", "Cancel");

        if (!ok) return;

        // Create subfolders if they don't exist
        foreach (var folder in new[] { "Core", "Player", "Bot", "NPC", "UI" })
        {
            string path = ROOT + "/" + folder;
            if (!AssetDatabase.IsValidFolder(path))
                AssetDatabase.CreateFolder(ROOT, folder);
        }

        int moved = 0, skipped = 0, failed = 0;

        foreach (var (file, folder) in MOVES)
        {
            string src = ROOT + "/" + file;
            string dst = ROOT + "/" + folder + "/" + file;

            // Already at destination — skip
            if (AssetDatabase.AssetPathToGUID(dst) != "")
            {
                // If source still exists too, delete the flat duplicate
                if (AssetDatabase.AssetPathToGUID(src) != "")
                    AssetDatabase.DeleteAsset(src);
                skipped++;
                continue;
            }

            // Source doesn't exist in flat folder (maybe already moved, or missing)
            if (AssetDatabase.AssetPathToGUID(src) == "")
            {
                skipped++;
                continue;
            }

            string error = AssetDatabase.MoveAsset(src, dst);
            if (string.IsNullOrEmpty(error))
                moved++;
            else
            {
                Debug.LogWarning($"[Organizer] Could not move {file}: {error}");
                failed++;
            }
        }

        AssetDatabase.Refresh();

        string summary = $"Moved: {moved}  |  Already done: {skipped}  |  Failed: {failed}";
        Debug.Log("[Organizer] " + summary);

        EditorUtility.DisplayDialog("Organize Complete", summary +
            (failed > 0 ? "\n\nCheck Console for details on failed moves." : "\n\nAll files organized!"),
            "OK");
    }
}
