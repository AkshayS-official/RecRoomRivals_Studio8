using UnityEngine;

public class CoachNPC : MonoBehaviour
{
    [Header("Coach idle animation")]
    public Animator animator;
    public string idleAnim = "Idle";
    public string talkAnim = "Talking";

    bool playerNear = false;

    void Start()
    {
        if (animator) animator.Play(idleAnim);
    }

    void Update()
    {
        // Face player when nearby
        if (playerNear && GameFlowManager.Instance != null)
        {
            Transform player = GameFlowManager.Instance.playerTransform;
            if (player != null)
            {
                Vector3 dir = player.position - transform.position;
                dir.y = 0;
                if (dir != Vector3.zero)
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(dir),
                        Time.deltaTime * 3f);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNear = true;
        if (animator) animator.Play(talkAnim);
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerNear = false;
        if (animator) animator.Play(idleAnim);
    }
}