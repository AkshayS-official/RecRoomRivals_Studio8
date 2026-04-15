using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ChallengeButton : MonoBehaviour
{
    [Header("References")]
    public CoachNPC coachNPC; // assign in inspector

    bool pressed = false;

    void Start()
    {
        // Hook into the existing XRSimpleInteractable
        var interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable != null)
            interactable.selectEntered.AddListener(OnPressed);

        Debug.Log("ChallengeButton ready!");
    }

    void OnPressed(SelectEnterEventArgs args)
    {
        if (pressed) return;
        if (GameFlowManager.Instance == null) return;
        if (GameFlowManager.Instance.currentState != GameFlowManager.GameState.Prompted)
        {
            Debug.Log("Button pressed but not in Prompted state — ignored");
            return;
        }

        pressed = true;
        Debug.Log("Challenge Button PRESSED — starting game!");

        // Hide the whole button stand
        transform.parent.gameObject.SetActive(false);

        // Start challenge
        GameFlowManager.Instance.StartChallengeFromButton();
    }

    // Reset for next game
    public void ResetButton()
    {
        pressed = false;
        transform.parent.gameObject.SetActive(false); // hidden by default
    }
}