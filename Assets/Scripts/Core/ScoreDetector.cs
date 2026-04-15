using UnityEngine;

/// <summary>
/// Attach to the hoop trigger (BoxCollider, isTrigger = true).
/// For training: set isPlayerHoop = false and assign linkedBot directly —
/// with 24 agents in the scene, FindFirstObjectByType always finds the
/// SAME bot. Direct reference ensures each hoop rewards its own agent.
/// </summary>
public class ScoreDetector : MonoBehaviour
{
    [Header("Set true if this hoop scores for the PLAYER")]
    public bool isPlayerHoop = false;

    [Header("Training: drag the ShooterBotAgent from the SAME TrainingArea")]
    [Tooltip("Assign in Inspector. Auto-filled by Tools > Fix Training Area Links.")]
    public ShooterBotAgent linkedBot;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Basketball")) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null || rb.linearVelocity.y >= 0f) return; // must be falling down

        if (isPlayerHoop)
        {
            // Game scene: tell GameFlowManager the player scored
            GameFlowManager.Instance?.AddScore(true);
            Debug.Log("[Hoop] Player scores!");
        }
        else
        {
            // Training / game scene: reward the correct bot
            ShooterBotAgent bot = linkedBot;

            // Fallback: walk up the hierarchy to find the agent in the same area
            if (bot == null)
                bot = GetComponentInParent<ShooterBotAgent>();

            // Last resort (only safe when there is exactly 1 agent in scene)
            if (bot == null)
                bot = Object.FindFirstObjectByType<ShooterBotAgent>();

            if (bot != null)
                bot.OnScore();
            else
                GameFlowManager.Instance?.AddScore(false);

            Debug.Log($"[Hoop] Bot scores! ({(bot != null ? bot.gameObject.name : "unknown")})");
        }
    }
}
