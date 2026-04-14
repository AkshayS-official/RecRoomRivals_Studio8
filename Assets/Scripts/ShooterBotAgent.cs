using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class ShooterBotAgent : Agent
{
    [Header("References")]
    public Transform hoopTransform;
    public Transform ballSpawnPoint;
    public GameObject basketballPrefab;
    public float throwForce = 8f;
    public Transform[] courtPositions;

    Rigidbody currentBall;
    bool ballThrown = false;
    int throwsLeft = 4;

    public override void OnEpisodeBegin()
    {
        if (courtPositions != null && courtPositions.Length > 0)
            transform.position = courtPositions[Random.Range(0, courtPositions.Length)].position;

        if (currentBall != null) Destroy(currentBall.gameObject);
        ballThrown = false;
        throwsLeft = 4;
        SpawnBall();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(transform.localPosition);
        sensor.AddObservation(hoopTransform != null
            ? hoopTransform.position - transform.position : Vector3.zero);
        sensor.AddObservation(currentBall != null
            ? currentBall.transform.localPosition : Vector3.zero);
        sensor.AddObservation((float)throwsLeft / 4f);
        // Total: 10
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (ballThrown || currentBall == null) return;

        float dirX = actions.ContinuousActions[0];
        float dirY = actions.ContinuousActions[1];
        float dirZ = actions.ContinuousActions[2];
        float forceMult = (actions.ContinuousActions[3] + 1f) / 2f;

        Vector3 throwDir = new Vector3(dirX, dirY + 0.5f, dirZ).normalized;
        currentBall.isKinematic = false;
        currentBall.AddForce(throwDir * throwForce * (0.5f + forceMult), ForceMode.Impulse);
        ballThrown = true;
        throwsLeft--;

        AddReward(-0.01f);
        Invoke(nameof(AfterThrow), 2.5f);
    }

    void AfterThrow()
    {
        if (currentBall != null) Destroy(currentBall.gameObject);

        if (throwsLeft > 0)
        {
            ballThrown = false;
            SpawnBall();
        }
        else
        {
            EndEpisode();
        }
    }

    void SpawnBall()
    {
        if (basketballPrefab == null) return;
        Vector3 spawnPos = ballSpawnPoint != null
            ? ballSpawnPoint.position
            : transform.position + Vector3.up * 1.2f;

        GameObject ball = Instantiate(basketballPrefab, spawnPos, Quaternion.identity);
        currentBall = ball.GetComponent<Rigidbody>();
        if (currentBall != null) currentBall.isKinematic = true;
    }

    public void OnScore()
    {
        AddReward(1.0f);
        if (GameManager.Instance != null)
            GameManager.Instance.AddScore(false);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        if (hoopTransform == null) return;
        Vector3 toHoop = (hoopTransform.position - transform.position).normalized;
        ca[0] = toHoop.x;
        ca[1] = toHoop.y;
        ca[2] = toHoop.z;
        ca[3] = 0.8f;
    }
}