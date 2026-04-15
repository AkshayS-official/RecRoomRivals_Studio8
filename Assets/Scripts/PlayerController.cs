using UnityEngine;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [Header("Game Setup")]
    public int throwsPerPosition = 4;
    public Transform[] courtPositions;
    public GameObject ballStandPrefab;
    public GameObject basketballPrefab;

    [Header("Ball Spawn Points on Stand")]
    public Transform[] ballSpawnPoints;

    int throwsLeft;
    int currentPosIndex = 0;
    GameObject currentStand;
    CharacterController cc;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        throwsLeft = throwsPerPosition;

        // Start at first court position
        if (courtPositions != null && courtPositions.Length > 0)
            TeleportTo(courtPositions[0]);

        SpawnBallStand();
    }

    void TeleportTo(Transform target)
    {
        if (cc != null) cc.enabled = false;
        transform.position = target.position;
        if (cc != null) cc.enabled = true;
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
        TeleportTo(courtPositions[currentPosIndex]);
        throwsLeft = throwsPerPosition;
        SpawnBallStand();
    }

    void SpawnBallStand()
    {
        if (currentStand != null) Destroy(currentStand);

        Vector3 standPos = transform.position + transform.right * 1.5f;
        standPos.y = 0.5f; // sit on floor correctly

        if (ballStandPrefab != null)
            currentStand = Instantiate(ballStandPrefab, standPos, Quaternion.identity);

        // Wait 1 second then spawn balls
        if (currentStand != null && basketballPrefab != null)
            StartCoroutine(SpawnBallsAfterDelay(1f));
    }

    IEnumerator SpawnBallsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (currentStand == null) yield break;

        for (int i = 0; i < currentStand.transform.childCount && i < 4; i++)
        {
            Transform spawnPoint = currentStand.transform.GetChild(i);

            GameObject ball = Instantiate(basketballPrefab,
                spawnPoint.position, spawnPoint.rotation);

            // Parent to spawn point
            ball.transform.SetParent(spawnPoint);

            // Reset local position to exactly 0,0,0
            ball.transform.localPosition = Vector3.zero;
            ball.transform.localRotation = Quaternion.identity;
            ball.transform.localScale = Vector3.one;

            Rigidbody rb = ball.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }
    }
}