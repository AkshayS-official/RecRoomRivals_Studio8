using UnityEngine;
using TMPro;

public class CoachNPC : MonoBehaviour
{
    [Header("References")]
    public GameObject challengeButtonStand; // drag "Button Stand" here
    public TextMeshPro speechBubbleText;
    public Animator animator;

    [Header("Settings")]
    public float triggerRadius = 3f;
    public string idleAnim = "Idle";
    public string talkAnim = "Talking";

    bool playerNear = false;

    void Start()
    {
        // Hide button at start
        if (challengeButtonStand != null)
            challengeButtonStand.SetActive(false);

        if (speechBubbleText != null)
            speechBubbleText.text = "";

        if (animator) animator.Play(idleAnim);

        Debug.Log("CoachNPC ready — radius: " + triggerRadius);
    }

    void Update()
    {
        if (GameFlowManager.Instance == null) return;
        if (GameFlowManager.Instance.currentState != GameFlowManager.GameState.FreeRoam &&
            GameFlowManager.Instance.currentState != GameFlowManager.GameState.Prompted)
            return;

        CheckPlayerProximity();

        // Face player
        if (playerNear && GameFlowManager.Instance.playerTransform != null)
        {
            Vector3 dir = GameFlowManager.Instance.playerTransform.position
                        - transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir),
                    Time.deltaTime * 3f);
        }
    }

    void CheckPlayerProximity()
    {
        if (GameFlowManager.Instance?.playerTransform == null) return;

        float dist = Vector3.Distance(
            transform.position,
            GameFlowManager.Instance.playerTransform.position);

        bool wasNear = playerNear;
        playerNear = dist < triggerRadius;

        if (playerNear && !wasNear) OnPlayerEnter();
        if (!playerNear && wasNear) OnPlayerExit();
    }

    void OnPlayerEnter()
    {
        Debug.Log("✅ Player near coach!");

        if (animator) animator.Play(talkAnim);

        if (speechBubbleText != null)
            speechBubbleText.text = "Ready for a challenge?\nPoke the button!";

        if (challengeButtonStand != null)
            challengeButtonStand.SetActive(true);

        // Reset button state
        ChallengeButton btn = challengeButtonStand?
            .GetComponentInChildren<ChallengeButton>();
        if (btn != null) btn.ResetButton();

        if (GameFlowManager.Instance != null)
            GameFlowManager.Instance.currentState = GameFlowManager.GameState.Prompted;
    }

    void OnPlayerExit()
    {
        Debug.Log("Player left coach zone");

        if (animator) animator.Play(idleAnim);

        if (speechBubbleText != null)
            speechBubbleText.text = "";

        if (challengeButtonStand != null)
            challengeButtonStand.SetActive(false);

        if (GameFlowManager.Instance?.currentState == GameFlowManager.GameState.Prompted)
            GameFlowManager.Instance.currentState = GameFlowManager.GameState.FreeRoam;
    }
}