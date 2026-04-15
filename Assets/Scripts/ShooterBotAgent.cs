using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class ShooterBotAgent : Agent
{
    [Header("References")]
    public Transform hoopTransform;
    public Transform ballSpawnPoint;
    public GameObject basketballPrefab;

    [Header("Movement - Game Only")]
    public NavMeshAgent navAgent;

    [Header("Settings")]
    public float minDistance = 5f;
    public float maxDistance = 20f;
    public float minLaunchAngle = 52f;
    public float maxLaunchAngle = 72f;

    Rigidbody currentBall;
    bool ballThrown = false;
    int throwsLeft = 4;
    bool roundActive = false;
    float currentDistanceFromHoop;
    float closestDistDuringFlight = float.MaxValue;
    Vector3 idealLaunchVelocity;

    // ── Game Flow ──────────────────────────────────────────────
    public void BeginRound()
    {
        CleanupBall();
        ballThrown = false;
        throwsLeft = 4;
        roundActive = true;
        SpawnBall();
    }

    public override void OnEpisodeBegin()
    {
        if (navAgent != null) navAgent.enabled = false;
        CleanupBall();
        ballThrown = false;
        throwsLeft = 4;
        roundActive = true;
        closestDistDuringFlight = float.MaxValue;
        RepositionBot();
        SpawnBall();
    }

    void CleanupBall()
    {
        CancelInvoke(nameof(AfterThrow));
        if (currentBall != null)
        {
            Destroy(currentBall.gameObject);
            currentBall = null;
        }
    }

    void RepositionBot()
    {
        if (hoopTransform == null) return;

        float stepFraction = Mathf.Clamp01((float)StepCount / 5000000f);
        float minDist = Mathf.Lerp(minDistance, minDistance + 2f, stepFraction);
        float maxDist = Mathf.Lerp(minDistance + 3f, maxDistance, stepFraction);
        currentDistanceFromHoop = Random.Range(minDist, maxDist);

        Vector3 hoopPos = hoopTransform.position;
        Vector3 toCenter = new Vector3(-hoopPos.x, 0f, -hoopPos.z).normalized;
        if (toCenter == Vector3.zero) toCenter = Vector3.forward;

        Vector3 rightDir = Vector3.Cross(Vector3.up, toCenter).normalized;
        float spread = Random.Range(-0.5f, 0.5f);
        Vector3 dir = (toCenter + rightDir * spread).normalized;

        Vector3 spawnPos = new Vector3(
            hoopPos.x + dir.x * currentDistanceFromHoop,
            0f,
            hoopPos.z + dir.z * currentDistanceFromHoop
        );
        transform.position = spawnPos;

        currentDistanceFromHoop = Vector3.Distance(
            new Vector3(spawnPos.x, 0f, spawnPos.z),
            new Vector3(hoopPos.x, 0f, hoopPos.z)
        );

        Vector3 look = hoopPos - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(look);

        CalculateIdealTrajectory();
    }

    void CalculateIdealTrajectory()
    {
        if (hoopTransform == null || ballSpawnPoint == null) return;

        Vector3 startPos = ballSpawnPoint.position;
        Vector3 targetPos = hoopTransform.position;
        Vector3 toTarget = targetPos - startPos;
        float horizDist = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
        float heightDiff = targetPos.y - startPos.y;
        float gravity = Mathf.Abs(Physics.gravity.y);

        float distFraction = Mathf.InverseLerp(minDistance, maxDistance,
                                                currentDistanceFromHoop);
        float launchAngle = Mathf.Lerp(maxLaunchAngle, minLaunchAngle,
                                         distFraction) * Mathf.Deg2Rad;

        float cosA = Mathf.Cos(launchAngle);
        float sinA = Mathf.Sin(launchAngle);
        float tanA = Mathf.Tan(launchAngle);

        float denom = 2f * cosA * cosA * (horizDist * tanA - heightDiff);
        denom = Mathf.Max(denom, 0.1f);
        float speed = Mathf.Sqrt(gravity * horizDist * horizDist / denom);
        speed = Mathf.Clamp(speed, 3f, 30f);

        Vector3 flatDir = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
        idealLaunchVelocity = flatDir * speed * cosA + Vector3.up * speed * sinA;
    }

    // ── Observations — give AI everything it needs ─────────────
    public override void CollectObservations(VectorSensor sensor)
    {
        if (hoopTransform == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        Vector3 toHoop = hoopTransform.position - transform.position;

        // Where am I (3)
        sensor.AddObservation(transform.localPosition);

        // Where is hoop relative to me (3)
        sensor.AddObservation(toHoop);

        // How far normalized (1)
        sensor.AddObservation(toHoop.magnitude / maxDistance);

        // What's the ideal velocity direction (3)
        // AI can use this as a hint but is FREE to deviate
        sensor.AddObservation(idealLaunchVelocity.normalized);

        // Total: 10
    }

    // ── Actions — AI controls full throw freely ────────────────
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!roundActive || ballThrown || currentBall == null) return;
        if (hoopTransform == null) return;

        // AI outputs 4 raw values — full freedom
        // [0] = X velocity component
        // [1] = Y velocity component (up)
        // [2] = Z velocity component
        // [3] = overall speed scale

        float vx = actions.ContinuousActions[0]; // -1 to 1
        float vy = actions.ContinuousActions[1]; // -1 to 1
        float vz = actions.ContinuousActions[2]; // -1 to 1
        float scale = actions.ContinuousActions[3]; // -1 to 1

        // Use ideal velocity as the BASE — AI adds corrections
        // This gives it a good starting point while allowing full learning
        float speedScale = Mathf.Lerp(0.7f, 1.3f, (scale + 1f) / 2f);

        // Early training: lots of noise for exploration
        // Later training: AI's own output dominates
        float noiseDecay = Mathf.Lerp(1f, 0f,
            Mathf.Clamp01((float)StepCount / 3000000f));

        Vector3 noise = new Vector3(
            Random.Range(-1f, 1f) * noiseDecay * 3f,
            Random.Range(-0.5f, 0.5f) * noiseDecay * 2f,
            Random.Range(-1f, 1f) * noiseDecay * 3f
        );

        // AI correction vector
        Vector3 flatDir = new Vector3(
            idealLaunchVelocity.x, 0f, idealLaunchVelocity.z).normalized;
        Vector3 rightDir = Vector3.Cross(Vector3.up, flatDir).normalized;

        Vector3 aiCorrection = rightDir * vx * 2f
                             + Vector3.up * vy * 2f
                             + flatDir * vz * 1f;

        Vector3 finalVelocity = (idealLaunchVelocity + aiCorrection + noise) * speedScale;

        currentBall.isKinematic = false;
        currentBall.linearVelocity = finalVelocity;
        ballThrown = true;
        throwsLeft--;
        closestDistDuringFlight = float.MaxValue;

#if UNITY_EDITOR
        ThrowVisualizer vis = FindFirstObjectByType<ThrowVisualizer>();
        if (vis != null)
        {
            vis.RecordThrow(currentBall.transform.position,
                            finalVelocity, hoopTransform);
            vis.RecordIdealTrajectory(currentBall.transform.position,
                                      idealLaunchVelocity, hoopTransform);
        }
#endif

        AddReward(-0.002f); // tiny step penalty

        float flightTime = Mathf.Clamp(currentDistanceFromHoop / 8f, 1f, 4f);
        Invoke(nameof(AfterThrow), flightTime + 1.5f);
    }

    void Update()
    {
        // Track ball's closest approach to hoop every frame
        if (roundActive && currentBall != null && hoopTransform != null)
        {
            float d = Vector3.Distance(
                currentBall.transform.position,
                hoopTransform.position);
            if (d < closestDistDuringFlight)
                closestDistDuringFlight = d;
        }
    }

    void AfterThrow()
    {
        float dist = closestDistDuringFlight < float.MaxValue
            ? closestDistDuringFlight
            : 999f;

        // Simple strong reward — no weak middle rewards
        float reward;
        if (dist < 0.3f) reward = 2.0f;   // SCORE — massive reward
        else if (dist < 0.6f) reward = 0.5f;   // near miss
        else if (dist < 1.5f) reward = 0.05f;  // ok
        else reward = -0.05f; // miss penalty

        AddReward(reward);
        CleanupBall();
        ballThrown = false;
        closestDistDuringFlight = float.MaxValue;

        if (throwsLeft > 0)
            SpawnBall();
        else
        {
            roundActive = false;
            if (GameFlowManager.Instance != null)
                GameFlowManager.Instance.RequestReposition();
            else
                EndEpisode();
        }
    }

    void SpawnBall()
    {
        if (basketballPrefab == null) return;

        Vector3 pos = ballSpawnPoint != null
            ? ballSpawnPoint.position
            : transform.position + Vector3.up * 1.5f;

        GameObject ball = Instantiate(basketballPrefab, pos, Quaternion.identity);
        ball.transform.localScale = Vector3.one;

        currentBall = ball.GetComponent<Rigidbody>();
        if (currentBall != null)
        {
            currentBall.isKinematic = true;
            currentBall.collisionDetectionMode = CollisionDetectionMode.Continuous;
            currentBall.interpolation = RigidbodyInterpolation.Interpolate;
        }

        CalculateIdealTrajectory();
    }

    public void OnScore()
    {
        // Big reward for actual physics trigger score
        float distBonus = Mathf.Lerp(1f, 2f,
            currentDistanceFromHoop / maxDistance);
        AddReward(2.0f + distBonus);
        GameFlowManager.Instance?.AddScore(false);
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var ca = actionsOut.ContinuousActions;
        ca[0] = 0f;
        ca[1] = 0f;
        ca[2] = 0f;
        ca[3] = 0f; // perfect trajectory = zero corrections
    }

    void OnDrawGizmos()
    {
        if (hoopTransform == null) return;

        if (idealLaunchVelocity != Vector3.zero && ballSpawnPoint != null)
        {
            Gizmos.color = Color.blue;
            for (float t = 0; t < 4f; t += 0.05f)
            {
                Vector3 p1 = ballSpawnPoint.position
                           + idealLaunchVelocity * t
                           + new Vector3(0f, 0.5f * Physics.gravity.y * t * t, 0f);
                Vector3 p2 = ballSpawnPoint.position
                           + idealLaunchVelocity * (t + 0.05f)
                           + new Vector3(0f, 0.5f * Physics.gravity.y
                             * (t + 0.05f) * (t + 0.05f), 0f);
                Gizmos.DrawLine(p1, p2);
                if (p1.y < -1f) break;
            }
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(hoopTransform.position, 0.3f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, hoopTransform.position);
    }
}