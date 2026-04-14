using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float mouseSensitivity = 2f;
    public CharacterController characterController;

    [Header("Game Setup")]
    public int throwsPerPosition = 4;
    public Transform[] courtPositions;
    public GameObject ballStandPrefab;
    public GameObject basketballPrefab;

    int throwsLeft;
    int currentPosIndex = 0;
    GameObject currentStand;
    float rotationY = 0f;

    void Start()
    {
        throwsLeft = throwsPerPosition;
        if (courtPositions != null && courtPositions.Length > 0)
            MoveToPosition(courtPositions[0]);
        SpawnBallStand();
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsGameActive()) return;
        HandleMovement();
    }

    void HandleMovement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = transform.right * h + transform.forward * v;
        if (characterController != null)
            characterController.Move(move * moveSpeed * Time.deltaTime);

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        rotationY += mouseX;
        transform.rotation = Quaternion.Euler(0f, rotationY, 0f);
    }

    public void OnBallThrown()
    {
        throwsLeft--;
        if (throwsLeft <= 0)
            Invoke(nameof(MoveToNextPosition), 1.5f);
    }

    void MoveToNextPosition()
    {
        if (courtPositions == null || courtPositions.Length == 0) return;
        currentPosIndex = Random.Range(0, courtPositions.Length);
        MoveToPosition(courtPositions[currentPosIndex]);
        throwsLeft = throwsPerPosition;
        SpawnBallStand();
    }

    void MoveToPosition(Transform target)
    {
        transform.position = target.position;
    }

    [Header("Ball Spawn Points on Stand")]
    public Transform[] ballSpawnPoints; // drag the 4 child points from your stand prefab

    void SpawnBallStand()
    {
        if (currentStand != null) Destroy(currentStand);

        Vector3 standPos = transform.position + transform.right * 1.5f;
        standPos.y = 0f;

        if (ballStandPrefab != null)
        {
            currentStand = Instantiate(ballStandPrefab, standPos, Quaternion.identity);

            // Use the stand's own child spawn points
            for (int i = 0; i < currentStand.transform.childCount && i < 4; i++)
            {
                Transform spawnPt = currentStand.transform.GetChild(i);
                if (basketballPrefab != null)
                {
                    GameObject ball = Instantiate(basketballPrefab, spawnPt.position, spawnPt.rotation);
                    Rigidbody rb = ball.GetComponent<Rigidbody>();
                    if (rb != null) rb.isKinematic = true;
                }
            }
        }
    }
}