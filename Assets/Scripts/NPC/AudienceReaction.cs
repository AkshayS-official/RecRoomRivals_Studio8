using UnityEngine;

/// <summary>
/// Drives crowd animator reactions. Place on any GameObject in the scene.
/// GameFlowManager calls TriggerCheer() / TriggerBoo() statically.
/// </summary>
public class AudienceReaction : MonoBehaviour
{
    static AudienceReaction _instance;

    [Header("Audience Animators")]
    public Animator[] audienceAnimators;

    [Header("Animation Names")]
    public string idleAnim = "Idle";
    public string cheerAnim = "Cheer";
    public string booAnim = "Boo";

    [Header("Settings")]
    public float returnToIdleDelay = 2f;

    void Awake() => _instance = this;

    // ── Static API (called from GameFlowManager) ──────────────────────

    public static void TriggerCheer()
    {
        if (_instance != null) _instance.PlayCheer();
    }

    public static void TriggerBoo()
    {
        if (_instance != null) _instance.PlayBoo();
    }

    // ── Internal ──────────────────────────────────────────────────────

    void PlayCheer()
    {
        SetAnimation(cheerAnim);
        CancelInvoke(nameof(ReturnToIdle));
        Invoke(nameof(ReturnToIdle), returnToIdleDelay);
    }

    void PlayBoo()
    {
        SetAnimation(booAnim);
        CancelInvoke(nameof(ReturnToIdle));
        Invoke(nameof(ReturnToIdle), returnToIdleDelay);
    }

    void ReturnToIdle() => SetAnimation(idleAnim);

    void SetAnimation(string animName)
    {
        foreach (var anim in audienceAnimators)
            if (anim != null) anim.Play(animName);
    }
}
