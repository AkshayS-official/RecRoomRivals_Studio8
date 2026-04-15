using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu: Tools > RecRoom Rivals > Fix Training Area Links
///
/// For every ScoreDetector in the training scene:
///   1. Sets isPlayerHoop = false  (all hoops are bot hoops in training)
///   2. Walks up the hierarchy to find the ShooterBotAgent in the same
///      TrainingArea parent and assigns it to ScoreDetector.linkedBot
///
/// Run once after opening the Training scene, then Ctrl+S.
/// </summary>
public class TrainingAreaLinker : Editor
{
    [MenuItem("Tools/RecRoom Rivals/Fix Training Area Links")]
    static void LinkTrainingAreas()
    {
        var detectors = Object.FindObjectsByType<ScoreDetector>(FindObjectsSortMode.None);
        var agents    = Object.FindObjectsByType<ShooterBotAgent>(FindObjectsSortMode.None);

        if (detectors.Length == 0)
        {
            EditorUtility.DisplayDialog("Not Found",
                "No ScoreDetectors found. Open the Training scene first.", "OK");
            return;
        }

        int linked   = 0;
        int skipped  = 0;
        int notFound = 0;

        foreach (var det in detectors)
        {
            // All hoops in training are bot hoops
            det.isPlayerHoop = false;

            // ── Find the ShooterBotAgent in the same TrainingArea ─────
            // Strategy: walk up the hierarchy from the detector until we
            // find a common parent that also contains a ShooterBotAgent.
            ShooterBotAgent match = FindAgentInSameArea(det.transform, agents);

            if (match != null)
            {
                det.linkedBot = match;
                EditorUtility.SetDirty(det);
                linked++;
                Debug.Log($"[Linker] {det.gameObject.name} -> {match.gameObject.name}");
            }
            else
            {
                notFound++;
                Debug.LogWarning($"[Linker] Could not find agent for {det.gameObject.name} in {det.transform.root.name}");
            }
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        string summary =
            $"ScoreDetectors found:  {detectors.Length}\n" +
            $"Successfully linked:   {linked}\n" +
            $"Already linked:        {skipped}\n" +
            $"Could not match:       {notFound}\n\n" +
            (notFound > 0
                ? "Check the Console for which detectors couldn't be matched.\n" +
                  "Assign linkedBot manually in those cases.\n\n"
                : "") +
            "All isPlayerHoop values set to FALSE.\n\n" +
            "Save the scene (Ctrl+S), then rebuild the Training EXE.";

        EditorUtility.DisplayDialog(
            notFound > 0 ? "Training Links — Some Missing" : "Training Links — All Done!",
            summary, "OK");
    }

    /// <summary>
    /// Walk up from the detector's transform. At each ancestor, check if
    /// any ShooterBotAgent shares that ancestor. Return the closest match.
    /// </summary>
    static ShooterBotAgent FindAgentInSameArea(Transform detectorTrans,
                                                ShooterBotAgent[] allAgents)
    {
        Transform current = detectorTrans.parent;

        while (current != null)
        {
            // Check each agent: does it share this ancestor?
            foreach (var agent in allAgents)
            {
                if (IsChildOf(agent.transform, current))
                    return agent;
            }
            current = current.parent;
        }
        return null;
    }

    static bool IsChildOf(Transform child, Transform potentialParent)
    {
        Transform t = child;
        while (t != null)
        {
            if (t == potentialParent) return true;
            t = t.parent;
        }
        return false;
    }
}
