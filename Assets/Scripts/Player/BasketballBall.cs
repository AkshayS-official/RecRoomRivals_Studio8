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
        var grab = GetComponent<XRGrabInteractable>();
        if (grab != null)
            grab.selectExited.AddListener(OnXRReleased);
    }

    void OnXRReleased(SelectExitEventArgs args) => RegisterThrow();

    // Desktop click fallback — only runs if Camera.main exists (not in VR)
    void Update()
    {
        if (thrown) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Camera cam = Camera.main;
        if (cam == null) return;   // VR: no main camera, skip entirely

        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if (!Physics.Raycast(ray, out RaycastHit hit)) return;
        if (hit.collider.gameObject != gameObject) return;

        GameObject hoopObj = GameObject.FindWithTag("Hoop");
        if (hoopObj != null)
        {
            rb.isKinematic = false;
            Vector3 dir = (hoopObj.transform.position - transform.position).normalized;
            rb.AddForce((dir + Vector3.up * 0.6f) * 9f, ForceMode.Impulse);
        }
        RegisterThrow();
    }

    void RegisterThrow()
    {
        if (thrown) return;
        thrown = true;
        Invoke(nameof(SelfDestruct), destroyDelay);
    }

    void SelfDestruct() => Destroy(gameObject);
}
