using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class BasketballBall : MonoBehaviour
{
    public float destroyDelay = 15f;
    bool thrown = false;
    Rigidbody rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Hook into XR grab if available
        var grab = GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.selectExited.AddListener(OnXRReleased);
    }

    void OnXRReleased(SelectExitEventArgs args)
    {
        RegisterThrow();
    }

    // Desktop click-to-throw fallback
    void Update()
    {
        if (thrown) return;
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.gameObject == gameObject)
            {
                GameObject hoopObj = GameObject.FindWithTag("Hoop");
                if (hoopObj != null)
                {
                    rb.isKinematic = false;
                    Vector3 dir = (hoopObj.transform.position - transform.position).normalized;
                    rb.AddForce((dir + Vector3.up * 0.6f) * 9f, ForceMode.Impulse);
                }
                RegisterThrow();
            }
        }
    }

    void RegisterThrow()
    {
        if (thrown) return;
        thrown = true;
        PlayerController pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) pc.OnBallThrown();
        Invoke(nameof(SelfDestruct), destroyDelay);
    }

    void SelfDestruct() => Destroy(gameObject);
}