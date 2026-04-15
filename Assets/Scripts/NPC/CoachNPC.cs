using UnityEngine;

public class CoachNPC : MonoBehaviour
{
    [Header("Animations")]
    public Animator animator;
    public string idleAnim   = "Idle";
    public string talkAnim   = "Talking";

    [Header("Speech Bubble")]
    [Tooltip("The speech-bubble / world-space text Transform. " +
             "Billboard fix: it will always face the camera on the Y axis only (no flipping).")]
    public Transform speechBubble;

    [Header("Challenge Button Stand")]
    [Tooltip("Shown when player is nearby, hidden otherwise")]
    public GameObject challengeButtonStand;

    bool playerNear = false;
    Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        if (animator) animator.Play(idleAnim);
        if (challengeButtonStand != null) challengeButtonStand.SetActive(false);
    }

    void Update()
    {
        // ── Face the player (coach body) ──────────────────────────────
        if (playerNear && GameFlowManager.Instance != null)
        {
            Transform player = GameFlowManager.Instance.playerTransform;
            if (player != null)
            {
                Vector3 dir = player.position - transform.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * 3f);
            }
        }

    }

    void LateUpdate()
    {
        // ── Billboard speech bubble toward camera (Y-axis only) ───────
        // TMP 3D text is visible from its -Z side; we point +Z *away*
        // from the camera so the front face is always toward the player.
        if (speechBubble != null)
        {
            if (mainCam == null) mainCam = Camera.main;
            if (mainCam != null)
            {
                Vector3 dir = mainCam.transform.position - speechBubble.position;
                dir.y = 0f;
                if (dir.sqrMagnitude > 0.001f)
                    speechBubble.rotation = Quaternion.LookRotation(-dir);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNear = true;
        if (animator) animator.Play(talkAnim);

        // Show button only if no game is in progress
        if (challengeButtonStand != null &&
            GameFlowManager.Instance != null &&
            GameFlowManager.Instance.currentState == GameFlowManager.GameState.FreeRoam)
        {
            challengeButtonStand.SetActive(true);
            Debug.Log("[CoachNPC] Player nearby — challenge button shown");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNear = false;
        if (animator) animator.Play(idleAnim);
        if (challengeButtonStand != null) challengeButtonStand.SetActive(false);
    }
}
