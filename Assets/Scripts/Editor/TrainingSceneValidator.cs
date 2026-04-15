using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Menu: Tools > RecRoom Rivals > Validate Training Scene
/// Checks every ShooterBotAgent in the OPEN scene for missing refs,
/// wrong collider setup, missing tags, etc.
/// Run this while the Training scene is open.
/// </summary>
public class TrainingSceneValidator : Editor
{
    [MenuItem("Tools/RecRoom Rivals/Validate Training Scene")]
    static void Validate()
    {
        var agents = Object.FindObjectsByType<ShooterBotAgent>(FindObjectsSortMode.None);
        var detectors = Object.FindObjectsByType<ScoreDetector>(FindObjectsSortMode.None);

        int errors   = 0;
        int warnings = 0;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine($"=== Training Scene Validation ({agents.Length} agents) ===\n");

        // ── 1. Check agents ───────────────────────────────────────────
        if (agents.Length == 0)
        {
            sb.AppendLine("[ERROR] No ShooterBotAgent found in scene!");
            sb.AppendLine("  -> Make sure the Training scene is open (not GameScene).");
            errors++;
        }

        foreach (var agent in agents)
        {
            string prefix = $"[{agent.gameObject.name}] ";

            if (agent.hoopTransform == null)
            {
                sb.AppendLine($"{prefix}ERROR: hoopTransform is NULL");
                sb.AppendLine("  -> Assign the TRIGGER (BoxCollider center) Transform, not the rim.");
                errors++;
            }
            else
            {
                // Check for trigger on the transform itself, its children, OR its parents.
                // hoopTransform is often the aim-point child (no collider); the trigger
                // is on the parent Net — both are valid and correct.
                bool hasTrigger = false;

                foreach (var c in agent.hoopTransform.GetComponents<Collider>())
                    if (c.isTrigger) { hasTrigger = true; break; }

                if (!hasTrigger)
                    foreach (var c in agent.hoopTransform.GetComponentsInChildren<Collider>())
                        if (c.isTrigger) { hasTrigger = true; break; }

                if (!hasTrigger)
                {
                    Transform p = agent.hoopTransform.parent;
                    while (p != null && !hasTrigger)
                    {
                        foreach (var c in p.GetComponents<Collider>())
                            if (c.isTrigger) { hasTrigger = true; break; }
                        p = p.parent;
                    }
                }

                sb.AppendLine(hasTrigger
                    ? $"{prefix}OK  hoopTransform assigned (trigger found in hierarchy)"
                    : $"{prefix}WARNING: No trigger collider found anywhere near hoopTransform");

                if (!hasTrigger) warnings++;
            }

            if (agent.ballSpawnPoint == null)
            {
                sb.AppendLine($"{prefix}ERROR: ballSpawnPoint is NULL");
                errors++;
            }
            else
            {
                sb.AppendLine($"{prefix}OK  ballSpawnPoint assigned");
            }

            if (agent.basketballPrefab == null)
            {
                sb.AppendLine($"{prefix}ERROR: basketballPrefab is NULL");
                errors++;
            }
            else
            {
                // Check basketball tag
                if (agent.basketballPrefab.tag != "Basketball")
                {
                    sb.AppendLine($"{prefix}ERROR: basketballPrefab tag is '{agent.basketballPrefab.tag}', needs 'Basketball'");
                    sb.AppendLine("  -> Select the prefab, set Tag = Basketball in Inspector.");
                    errors++;
                }
                else
                {
                    sb.AppendLine($"{prefix}OK  basketballPrefab has 'Basketball' tag");
                }

                // Check Rigidbody
                if (agent.basketballPrefab.GetComponent<Rigidbody>() == null)
                {
                    sb.AppendLine($"{prefix}ERROR: basketballPrefab has no Rigidbody");
                    errors++;
                }
            }
        }

        sb.AppendLine();

        // ── 2. Check ScoreDetectors ───────────────────────────────────
        if (detectors.Length == 0)
        {
            sb.AppendLine("[ERROR] No ScoreDetector found in training scene!");
            sb.AppendLine("  -> Each hoop's trigger needs a ScoreDetector with isPlayerHoop = false.");
            errors++;
        }
        else
        {
            int botDetectors = 0;
            foreach (var d in detectors)
            {
                if (!d.isPlayerHoop) botDetectors++;
                Collider col = d.GetComponent<Collider>();
                if (col == null)
                {
                    sb.AppendLine($"[{d.gameObject.name}] ERROR: ScoreDetector has no Collider on same GO");
                    errors++;
                }
                else if (!col.isTrigger)
                {
                    sb.AppendLine($"[{d.gameObject.name}] ERROR: Collider is NOT a trigger — balls won't register");
                    sb.AppendLine("  -> Check 'Is Trigger' on the BoxCollider.");
                    errors++;
                }
                else
                {
                    sb.AppendLine($"[{d.gameObject.name}] OK  ScoreDetector trigger  isPlayerHoop={d.isPlayerHoop}");
                }
            }

            if (agents.Length > 0 && botDetectors < agents.Length)
            {
                sb.AppendLine($"[WARNING] {agents.Length} agents but only {botDetectors} bot ScoreDetectors");
                sb.AppendLine("  -> Every training hoop needs its own ScoreDetector (isPlayerHoop=false).");
                warnings++;
            }
        }

        sb.AppendLine();

        // ── 3. NavMesh check ──────────────────────────────────────────
        UnityEngine.AI.NavMeshTriangulation tri = UnityEngine.AI.NavMesh.CalculateTriangulation();
        if (tri.vertices.Length == 0)
        {
            sb.AppendLine("[WARNING] No NavMesh baked in this scene");
            sb.AppendLine("  -> Open Window > AI > Navigation and bake, OR ignore if bot nav is disabled during training.");
            warnings++;
        }
        else
        {
            sb.AppendLine($"[OK] NavMesh present ({tri.vertices.Length} verts)");
        }

        sb.AppendLine();

        // ── 4. Summary ────────────────────────────────────────────────
        sb.AppendLine($"=== Result: {errors} error(s)  {warnings} warning(s) ===");

        if (errors == 0 && warnings == 0)
            sb.AppendLine("Everything looks good! You can start training.");
        else if (errors > 0)
            sb.AppendLine("Fix the ERRORs before training — reward will stay 0 otherwise.");

        string report = sb.ToString();
        Debug.Log(report);

        EditorUtility.DisplayDialog(
            errors > 0 ? $"Training Scene: {errors} ERROR(s)" : "Training Scene: OK",
            report,
            "OK");
    }

    // ── Quick-fix helper ──────────────────────────────────────────────

    [MenuItem("Tools/RecRoom Rivals/Fix Basketball Tag (All Prefabs)")]
    static void FixBasketballTag()
    {
        // Ensure the tag exists
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");
        bool found = false;
        for (int i = 0; i < tagsProp.arraySize; i++)
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == "Basketball") { found = true; break; }
        if (!found)
        {
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = "Basketball";
            tagManager.ApplyModifiedProperties();
            Debug.Log("[Validator] Added 'Basketball' tag to project.");
        }

        // Find all basketball prefabs and tag them
        string[] guids = AssetDatabase.FindAssets("Basketball t:Prefab");
        int fixed_ = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null && prefab.tag != "Basketball")
            {
                prefab.tag = "Basketball";
                PrefabUtility.SavePrefabAsset(prefab);
                Debug.Log($"[Validator] Tagged: {path}");
                fixed_++;
            }
        }
        EditorUtility.DisplayDialog("Basketball Tag Fix",
            $"Added 'Basketball' tag to {fixed_} prefab(s).\nRun Validate again to confirm.",
            "OK");
    }
}
