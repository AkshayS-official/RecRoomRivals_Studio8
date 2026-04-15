using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Filtering;

/// <summary>
/// Menu: Tools > RecRoom Rivals > Fix Challenge Button
/// Finds every ChallengeButton in the scene and adds the missing
/// XRSimpleInteractable + makes its collider a trigger.
/// </summary>
public class ChallengeButtonSetup : Editor
{
    [MenuItem("Tools/RecRoom Rivals/Fix Challenge Button")]
    static void Fix()
    {
        var buttons = Object.FindObjectsByType<ChallengeButton>(FindObjectsSortMode.None);

        if (buttons.Length == 0)
        {
            EditorUtility.DisplayDialog("Not Found",
                "No ChallengeButton component found in the scene.\n" +
                "Make sure the GameScene is open.", "OK");
            return;
        }

        int fixed_ = 0;
        foreach (var btn in buttons)
        {
            GameObject go = btn.gameObject;

            // ── 1. Ensure trigger collider exists ─────────────────────
            Collider col = go.GetComponent<Collider>();
            if (col == null)
            {
                // Add a sensible sphere trigger (button press zone)
                SphereCollider sc = go.AddComponent<SphereCollider>();
                sc.radius = 0.15f;
                sc.isTrigger = true;
                col = sc;
                Debug.Log($"[ButtonSetup] Added SphereCollider trigger to {go.name}");
            }
            else if (!col.isTrigger)
            {
                col.isTrigger = true;
                Debug.Log($"[ButtonSetup] Set existing collider to trigger on {go.name}");
            }
            EditorUtility.SetDirty(col);

            // ── 2. Add XRSimpleInteractable if missing ─────────────────
            var xrInteractable = go.GetComponent<XRSimpleInteractable>();
            if (xrInteractable == null)
            {
                xrInteractable = go.AddComponent<XRSimpleInteractable>();

                // Allow all interaction types (poke, grab, gaze)
                xrInteractable.interactionLayers =
                    UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask.GetMask("Default");

                Debug.Log($"[ButtonSetup] Added XRSimpleInteractable to {go.name}");
            }
            EditorUtility.SetDirty(xrInteractable);

            // ── 3. Add XRPokeFilter for physical push interaction ──────
            // (Allows the player to poke the button with a controller)
            var pokeFilter = go.GetComponent<XRPokeFilter>();
            if (pokeFilter == null)
            {
                pokeFilter = go.AddComponent<XRPokeFilter>();
                Debug.Log($"[ButtonSetup] Added XRPokeFilter to {go.name}");
            }
            EditorUtility.SetDirty(pokeFilter);

            EditorUtility.SetDirty(go);
            fixed_++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("Challenge Button Fixed!",
            $"Fixed {fixed_} ChallengeButton(s):\n\n" +
            "  + Collider set to trigger\n" +
            "  + XRSimpleInteractable added\n" +
            "  + XRPokeFilter added\n\n" +
            "Save the scene (Ctrl+S).",
            "OK");
    }
}
