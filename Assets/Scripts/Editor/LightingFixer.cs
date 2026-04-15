using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Menu: Tools > RecRoom Rivals > Fix Scene Lighting
/// Boosts the arena from dark/gloomy to bright and well-lit.
/// </summary>
public class LightingFixer : Editor
{
    [MenuItem("Tools/RecRoom Rivals/Fix Scene Lighting")]
    static void FixLighting()
    {
        int lightsBoosted = 0;

        // ── 1. Boost directional and spot/point lights ────────────────
        foreach (var light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            bool changed = false;

            if (light.type == LightType.Directional)
            {
                // Main sun/sky light should be at least 1.0
                if (light.intensity < 1.0f)
                {
                    light.intensity = 1.1f;
                    changed = true;
                }
                // Warm white colour
                light.color = new Color(1.0f, 0.96f, 0.88f);
                changed = true;
            }

            if (light.type == LightType.Spot || light.type == LightType.Point)
            {
                // Arena spotlights — ensure they contribute
                if (light.intensity < 0.8f)
                {
                    light.intensity = Mathf.Max(light.intensity, 0.8f);
                    changed = true;
                }
            }

            if (changed)
            {
                EditorUtility.SetDirty(light);
                lightsBoosted++;
            }
        }

        // ── 2. Set bright trilight ambient ────────────────────────────
        // Trilight (sky / equator / ground) gives good bounce-light fill
        // without needing baked lightmaps.
        RenderSettings.ambientMode        = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor    = new Color(0.58f, 0.64f, 0.72f); // cool sky ceiling
        RenderSettings.ambientEquatorColor = new Color(0.44f, 0.44f, 0.44f); // neutral mid
        RenderSettings.ambientGroundColor  = new Color(0.22f, 0.20f, 0.16f); // warm floor bounce
        RenderSettings.ambientIntensity    = 1.25f;

        // ── 3. Disable fog (fog darkens arenas significantly) ─────────
        RenderSettings.fog = false;

        // ── 4. Mark scene dirty so Ctrl+S saves the changes ───────────
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Debug.Log($"[LightingFixer] Done — boosted {lightsBoosted} lights, set trilight ambient, disabled fog.");

        EditorUtility.DisplayDialog("Lighting Fixed!",
            $"Adjusted {lightsBoosted} light(s).\n" +
            "Ambient: Trilight (sky → equator → ground).\n" +
            "Fog: disabled.\n\n" +
            "Save your scene (Ctrl+S).\n\n" +
            "For fully baked lightmaps:\n" +
            "Window → Rendering → Lighting → Generate Lighting",
            "OK");
    }
}
