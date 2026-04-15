using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Attach to the Button Stand (or Push Button child).
/// Auto-adds XRSimpleInteractable and a trigger collider at runtime
/// if they are missing, so the button always works without manual setup.
/// </summary>
public class ChallengeButton : MonoBehaviour
{
    [Header("Visual Feedback")]
    public Animator buttonAnimator;
    public string pressAnimName = "Press";

    [Header("Settings")]
    public float pressCooldown = 1.5f;

    float lastPressTime = -999f;

    void Start()
    {
        // ── Ensure a trigger collider exists ─────────────────────────
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.radius    = 0.15f;
            sc.isTrigger = true;
        }
        else if (!col.isTrigger)
        {
            col.isTrigger = true;
        }

        // ── Ensure XRSimpleInteractable exists ────────────────────────
        var interactable = GetComponent<XRSimpleInteractable>();
        if (interactable == null)
        {
            interactable = gameObject.AddComponent<XRSimpleInteractable>();
            Debug.Log("[ChallengeButton] Auto-added XRSimpleInteractable to " + gameObject.name);
        }

        interactable.selectEntered.AddListener(OnPoked);
    }

    void OnPoked(SelectEnterEventArgs args)
    {
        if (Time.time - lastPressTime < pressCooldown) return;
        lastPressTime = Time.time;

        if (buttonAnimator != null) buttonAnimator.Play(pressAnimName);
        GameFlowManager.Instance?.StartChallengeFromButton();
        Debug.Log("[ChallengeButton] Pressed — challenge starting!");
    }
}
