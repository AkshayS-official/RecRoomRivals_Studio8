using UnityEngine;
using UnityEditor;
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;

/// <summary>
/// Menu: Tools > RecRoom Rivals > Fix XR Build Settings
///
/// Opens XR Plugin Management and shows exactly what to enable
/// so the XR Device Simulator works in PC standalone builds.
/// (XRPackageMetadataStore is not available in this Unity version —
///  we open the settings page and guide the user instead.)
/// </summary>
public class XRBuildSetup : Editor
{
    [MenuItem("Tools/RecRoom Rivals/Fix XR Build Settings")]
    static void FixXRForStandaloneBuild()
    {
        // ── Report current loader state ───────────────────────────────
        var settings = XRGeneralSettingsPerBuildTarget
            .XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

        string loaderList = "  (none)";
        if (settings != null && settings.Manager != null)
        {
            var loaders = settings.Manager.activeLoaders;
            if (loaders != null && loaders.Count > 0)
            {
                loaderList = "";
                foreach (var l in loaders)
                    loaderList += $"  + {l?.GetType().Name ?? "null"}\n";
            }
        }

        // ── Open XR Plugin Management in Project Settings ─────────────
        SettingsService.OpenProjectSettings("Project/XR Plug-in Management");

        // ── Show instructions ─────────────────────────────────────────
        EditorUtility.DisplayDialog(
            "XR Build Settings — Manual Step Required",

            "The Project Settings window just opened to XR Plug-in Management.\n\n" +

            "CURRENT PC Standalone loaders:\n" + loaderList + "\n" +

            "WHAT TO DO:\n" +
            "1. Click the  PC / Monitor  icon tab (Standalone).\n" +
            "2. Check  'OpenXR'  (works with real headset + simulator).\n" +
            "   OR check  'Mock HMD Provider'  (keyboard-only, no headset needed).\n" +
            "3. Close Project Settings.\n" +
            "4. Rebuild:  File > Build Settings > Build.\n\n" +

            "NOTE: The XR Device Simulator 'Editor Only' flag has already\n" +
            "been turned OFF, so the simulator will run in your build\n" +
            "once a loader is active.",

            "OK — I'll enable OpenXR");
    }
}
