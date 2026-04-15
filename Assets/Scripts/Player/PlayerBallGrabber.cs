using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Basketball throw mechanic (desktop + VR).
///
/// Desktop:
///   Hold T  →  Aim mode: trajectory arc appears, throw force charges up.
///   Press G →  Throw ball along the aimed arc with charged force.
///   Release T before G  →  Cancel aim (no throw).
///
/// VR (Meta Quest):
///   Pull right trigger (axis > threshold)  →  Aim mode; trigger value drives force.
///   Press grip (G key / grip button)       →  Throw.
///
/// Ball spawns 0.3 m forward of the right-hand anchor at throw time —
/// never inside the controller, so there is always room to aim cleanly.
/// </summary>
public class PlayerBallGrabber : MonoBehaviour
{
    // ── Inspector ──────────────────────────────────────────────────────

    [Header("References")]
    [Tooltip("Right Controller transform or its Attach Point child")]
    public Transform rightHandAnchor;
    public GameObject basketballPrefab;
    [Tooltip("Optional: assign a LineRenderer for the arc preview. " +
             "If left empty one is created automatically on this GameObject.")]
    public LineRenderer aimLine;

    [Header("Controls — Desktop")]
    public KeyCode aimKey   = KeyCode.T;   // hold to aim
    public KeyCode throwKey = KeyCode.G;   // press while aiming to throw

    [Header("Controls — VR")]
    [Tooltip("Input axis name for the right trigger (XR Device Simulator compatible)")]
    public string vrTriggerAxis = "XRI_Right_Trigger";
    [Tooltip("Trigger value above which aim mode activates (0–1)")]
    [Range(0f, 1f)] public float vrTriggerThreshold = 0.25f;

    [Header("Throw Force")]
    public float minThrowForce  = 5f;
    public float maxThrowForce  = 18f;
    [Tooltip("Seconds to hold before throw reaches max force")]
    public float maxChargeTime  = 2f;
    [Tooltip("Seconds before the next throw is allowed")]
    public float throwCooldown  = 0.4f;
    [Tooltip("How far in front of the hand anchor to spawn the ball (metres)")]
    public float spawnOffset    = 0.30f;

    [Header("Trajectory Preview")]
    [Tooltip("Number of physics steps to simulate")]
    public int   arcSteps    = 45;
    [Tooltip("Seconds per simulation step (smaller = more accurate arc)")]
    public float arcTimeStep = 0.05f;
    [Tooltip("Colour at the start of the arc")]
    public Color arcColorStart = new Color(1f, 0.92f, 0f, 0.9f);
    [Tooltip("Colour at the end (fades out)")]
    public Color arcColorEnd   = new Color(1f, 0.35f, 0f, 0f);

    // ── Runtime state ──────────────────────────────────────────────────

    bool  isAiming   = false;
    bool  onCooldown = false;
    float chargeTime = 0f;
    float vrTriggerValue = 0f;

    // ─────────────────────────────────────────────────────────────────

    void Awake()
    {
        EnsureLineRenderer();
    }

    void Update()
    {
        if (!IsGameActive())
        {
            CancelAim();
            return;
        }

        bool aimHeld  = ReadAimInput();
        bool throwNow = Input.GetKeyDown(throwKey);

        // Enter aim mode
        if (!isAiming && aimHeld && !onCooldown)
            BeginAim();

        if (isAiming)
        {
            if (!aimHeld)           // player released T / trigger without throwing
            {
                CancelAim();
                return;
            }

            // Charge throw force
            chargeTime = Mathf.Min(chargeTime + Time.deltaTime, maxChargeTime);

            // Update arc preview
            UpdateArcPreview();

            // Throw
            if (throwNow)
                ExecuteThrow();
        }
    }

    // ── Input ─────────────────────────────────────────────────────────

    bool ReadAimInput()
    {
        // VR right trigger axis
        vrTriggerValue = 0f;
        try { vrTriggerValue = Input.GetAxis(vrTriggerAxis); } catch { }
        if (vrTriggerValue >= vrTriggerThreshold) return true;

        // Desktop T key
        return Input.GetKey(aimKey);
    }

    // ── Aim mode ──────────────────────────────────────────────────────

    void BeginAim()
    {
        isAiming   = true;
        chargeTime = 0f;
        if (aimLine != null) aimLine.enabled = true;
        Debug.Log("[BallGrabber] Aim mode — release T to cancel, G to throw.");
    }

    void CancelAim()
    {
        isAiming = false;
        chargeTime = 0f;
        if (aimLine != null) aimLine.enabled = false;
    }

    // ── Throw ─────────────────────────────────────────────────────────

    void ExecuteThrow()
    {
        if (basketballPrefab == null || rightHandAnchor == null)
        {
            Debug.LogWarning("[BallGrabber] basketballPrefab or rightHandAnchor not assigned!");
            CancelAim();
            return;
        }

        // Resolve force: use trigger pressure when in VR, otherwise charge time
        float fraction = Mathf.Clamp01(chargeTime / maxChargeTime);
        if (vrTriggerValue >= vrTriggerThreshold)
            fraction = Mathf.Max(fraction, vrTriggerValue);     // pressure overrides time

        float force = Mathf.Lerp(minThrowForce, maxThrowForce, fraction);

        // Spawn ball 0.3 m forward of the hand — well clear of the controller
        Vector3 spawnPos = rightHandAnchor.position + rightHandAnchor.forward * spawnOffset;
        GameObject ball = Instantiate(basketballPrefab, spawnPos, rightHandAnchor.rotation);
        ball.transform.localScale = Vector3.one;

        // Disable XR grab so it doesn't self-select mid-flight
        var xrGrab = ball.GetComponent<XRGrabInteractable>();
        if (xrGrab != null) xrGrab.enabled = false;

        // Apply throw velocity
        var rb = ball.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic   = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.linearVelocity = rightHandAnchor.forward * force;
            Debug.Log($"[BallGrabber] Thrown! force={force:F1} speed={rb.linearVelocity.magnitude:F1} m/s");
        }

        CancelAim();
        onCooldown = true;
        Invoke(nameof(ClearCooldown), throwCooldown);
    }

    // ── Arc preview ────────────────────────────────────────────────────

    void UpdateArcPreview()
    {
        if (aimLine == null || rightHandAnchor == null) return;

        float fraction = Mathf.Clamp01(chargeTime / maxChargeTime);
        float force    = Mathf.Lerp(minThrowForce, maxThrowForce, fraction);

        Vector3 pos = rightHandAnchor.position + rightHandAnchor.forward * spawnOffset;
        Vector3 vel = rightHandAnchor.forward * force;

        aimLine.positionCount = arcSteps;
        for (int i = 0; i < arcSteps; i++)
        {
            aimLine.SetPosition(i, pos);
            vel += Physics.gravity * arcTimeStep;
            pos += vel * arcTimeStep;
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────

    void EnsureLineRenderer()
    {
        if (aimLine != null) return;

        aimLine = gameObject.AddComponent<LineRenderer>();
        aimLine.startWidth    = 0.025f;
        aimLine.endWidth      = 0.008f;
        aimLine.useWorldSpace = true;
        aimLine.material      = new Material(Shader.Find("Sprites/Default"));
        aimLine.startColor    = arcColorStart;
        aimLine.endColor      = arcColorEnd;
        aimLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        aimLine.enabled       = false;
    }

    void ClearCooldown() => onCooldown = false;

    /// <summary>
    /// Cancel aim and clean up. Called by GameFlowManager when a round ends.
    /// The old API had DropBall(); this name is kept for compatibility.
    /// </summary>
    public void DropBall() => CancelAim();

    bool IsGameActive() =>
        GameFlowManager.Instance != null && GameFlowManager.Instance.IsGameActive();

    void OnDisable() => CancelAim();
}
