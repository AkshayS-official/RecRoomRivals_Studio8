using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Attach to the XR Rig (or any persistent GameObject in GameScene).
///
/// On startup:
///   - Real Meta Quest 2 connected  → destroys the XR Device Simulator so
///     only the real controllers are active.
///   - No headset connected          → keeps the simulator alive for
///     keyboard/mouse desktop play.
///
/// Works with Quest Link (USB) and Air Link (Wi-Fi).
/// </summary>
public class VRHardwareDetector : MonoBehaviour
{
    [Header("Optional — leave empty to auto-find by name")]
    [Tooltip("Drag the XR Device Simulator GameObject here, or leave empty " +
             "and it will be found automatically.")]
    public GameObject xrDeviceSimulator;

    [Tooltip("Log which mode was chosen — useful for debugging.")]
    public bool debugLog = true;

    IEnumerator Start()
    {
        // Wait two frames — XR subsystems need a moment to fully initialise
        // before InputDevices reports connected hardware.
        yield return null;
        yield return null;

        bool realHMDFound = DetectRealHMD();

        if (realHMDFound)
        {
            // Real Quest 2 (or any HMD) connected — kill the simulator
            DisableSimulator();
            if (debugLog)
                Debug.Log("[VRDetector] Real HMD detected. XR Device Simulator disabled.");
        }
        else
        {
            // No hardware — keep simulator alive for desktop/mouse play
            if (debugLog)
                Debug.Log("[VRDetector] No HMD found. XR Device Simulator active (keyboard/mouse mode).");
        }
    }

    // ── Detection ──────────────────────────────────────────────────────

    bool DetectRealHMD()
    {
        // Method 1: check InputDevices for a head-mounted display
        var devices = new List<InputDevice>();
        InputDevices.GetDevicesWithCharacteristics(
            InputDeviceCharacteristics.HeadMounted, devices);

        if (devices.Count > 0)
        {
            if (debugLog)
                Debug.Log($"[VRDetector] HMD found via InputDevices: {devices[0].name}");
            return true;
        }

        // Method 2: check XR display subsystems (backup)
        var displays = new List<XRDisplaySubsystem>();
        SubsystemManager.GetSubsystems(displays);
        foreach (var d in displays)
        {
            if (d.running)
            {
                if (debugLog)
                    Debug.Log("[VRDetector] HMD found via XRDisplaySubsystem.");
                return true;
            }
        }

        return false;
    }

    // ── Simulator teardown ─────────────────────────────────────────────

    void DisableSimulator()
    {
        // Use assigned reference first
        if (xrDeviceSimulator != null)
        {
            Destroy(xrDeviceSimulator);
            return;
        }

        // Auto-find by the default name Unity gives it
        string[] simNames = {
            "XR Device Simulator",
            "XRDeviceSimulator",
            "XR Device Simulator(Clone)"
        };

        foreach (string n in simNames)
        {
            GameObject sim = GameObject.Find(n);
            if (sim != null)
            {
                Destroy(sim);
                if (debugLog)
                    Debug.Log($"[VRDetector] Destroyed simulator: {n}");
                return;
            }
        }

        // Last resort — find by component type
        var simComp = Object.FindFirstObjectByType
            <UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation.XRDeviceSimulator>();
        if (simComp != null)
        {
            Destroy(simComp.gameObject);
            if (debugLog)
                Debug.Log("[VRDetector] Destroyed simulator found by component.");
        }
    }
}
