using UnityEngine;

public class ScoreDetector : MonoBehaviour
{
    [Header("Set true if this hoop scores for the PLAYER")]
    public bool isPlayerHoop = true;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Basketball")) return;

        Rigidbody rb = other.GetComponent<Rigidbody>();
        if (rb == null || rb.linearVelocity.y >= 0) return; // must be falling

        if (GameManager.Instance == null) return;

        if (isPlayerHoop)
        {
            GameManager.Instance.AddScore(true);
            Debug.Log("🏀 Player Scores!");
        }
        else
        {
            // Check if it came from the bot
            ShooterBotAgent bot = FindFirstObjectByType<ShooterBotAgent>();
            if (bot != null) bot.OnScore();
            else GameManager.Instance.AddScore(false);
            Debug.Log("🤖 Bot Scores!");
        }
    }
}