using UnityEngine;
using UnityEngine.AI;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

/// <summary>
/// ML-Agents basketball bot.
///
/// HOW IT LEARNS TO THROW THROUGH THE HOOP:
///   1. CalculateIdealTrajectory() solves the exact physics arc that passes
///      through the hoop center (hoopTransform) at the correct launch angle.
///   2. The agent outputs 4 corrections to that ideal velocity.
///   3. During flight we track: did the ball pass through the hoop PLANE
///      (Y=hoopY) within the rim radius?  That is a "through" event.
///   4. ScoreDetector.OnScore() fires the largest reward (+3–4) when the
///      physics trigger actually registers a basket.
///   5. Secondary shaped rewards guide early training so the bot gets
///      feedback even before it scores.
///
/// IMPORTANT SCENE SETUP:
///   - hoopTransform  : the center-of-hoop trigger GO (BoxCollider, isTrigger)
///                      NOT the rim mesh — this is what the ideal trajectory
///                      aims at and what through-plane detection uses.
///   - ballSpawnPoint : directly in front of / above the bot.
///   - basketballPrefab: must have tag "Basketball" and a Rigidbody.
/// </summary>
public class ShooterBotAgent : Agent
{
    [Header("References")]
    public Transform  hoopTransform;
    public Transform  ballSpawnPoint;
    public GameObject basketballPrefab;

    [Header("Movement - Game Only")]
    public NavMeshAgent navAgent;

    [Header("Settings")]
    public float minDistance    = 5f;
    public float maxDistance    = 20f;
    public float minLaunchAngle = 52f;
    public float maxLaunchAngle = 72f;

    // ── Runtime ────────────────────────────────────────────────────────
    Rigidbody currentBall;
    bool  ballThrown   = false;
    int   throwsLeft   = 4;
    bool  roundActive  = false;
    float currentDistanceFromHoop;
    Vector3 idealLaunchVelocity;

    // Through-hoop tracking (per throw)
    bool  passedThroughHoopPlane = false;   // ball crossed Y=hoopY going DOWN
    float hoopRadius             = 0.23f;   // standard basketball hoop inner radius
    float prevBallY              = float.MaxValue;

    // ── Game Flow ──────────────────────────────────────────────────────
    public void BeginRound()
    {
        CleanupBall();
        ballThrown  = false;
        throwsLeft  = 4;
        roundActive = true;
        SpawnBall();
    }

    public override void OnEpisodeBegin()
    {
        if (navAgent != null) navAgent.enabled = false;
        CleanupBall();
        ballThrown  = false;
        throwsLeft  = 4;
        roundActive = true;
        passedThroughHoopPlane = false;
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
        passedThroughHoopPlane = false;
        prevBallY = float.MaxValue;
    }

    void RepositionBot()
    {
        if (hoopTransform == null) return;

        // Curriculum: start close, expand range as training progresses
        float stepFraction = Mathf.Clamp01((float)StepCount / 5000000f);
        float minDist = Mathf.Lerp(minDistance, minDistance + 2f, stepFraction);
        float maxDist = Mathf.Lerp(minDistance + 3f, maxDistance, stepFraction);
        currentDistanceFromHoop = Random.Range(minDist, maxDist);

        Vector3 hoopPos = hoopTransform.position;
        Vector3 toCenter = new Vector3(-hoopPos.x, 0f, -hoopPos.z).normalized;
        if (toCenter == Vector3.zero) toCenter = Vector3.forward;

        Vector3 rightDir = Vector3.Cross(Vector3.up, toCenter).normalized;
        float spread = Random.Range(-0.5f, 0.5f);
        Vector3 dir  = (toCenter + rightDir * spread).normalized;

        Vector3 spawnPos = new Vector3(
            hoopPos.x + dir.x * currentDistanceFromHoop,
            0f,
            hoopPos.z + dir.z * currentDistanceFromHoop);
        transform.position = spawnPos;

        currentDistanceFromHoop = Vector3.Distance(
            new Vector3(spawnPos.x, 0f, spawnPos.z),
            new Vector3(hoopPos.x, 0f, hoopPos.z));

        Vector3 look = hoopPos - transform.position;
        look.y = 0f;
        if (look.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(look);

        CalculateIdealTrajectory();
    }

    void CalculateIdealTrajectory()
    {
        if (hoopTransform == null || ballSpawnPoint == null) return;

        Vector3 startPos  = ballSpawnPoint.position;
        Vector3 targetPos = hoopTransform.position;
        Vector3 toTarget  = targetPos - startPos;
        float horizDist   = new Vector3(toTarget.x, 0f, toTarget.z).magnitude;
        float heightDiff  = targetPos.y - startPos.y;
        float gravity     = Mathf.Abs(Physics.gravity.y);

        float distFraction = Mathf.InverseLerp(minDistance, maxDistance,
                                                currentDistanceFromHoop);
        float launchAngle  = Mathf.Lerp(maxLaunchAngle, minLaunchAngle,
                                         distFraction) * Mathf.Deg2Rad;

        float cosA = Mathf.Cos(launchAngle);
        float sinA = Mathf.Sin(launchAngle);
        float tanA = Mathf.Tan(launchAngle);

        float denom = 2f * cosA * cosA * (horizDist * tanA - heightDiff);
        denom = Mathf.Max(denom, 0.1f);
        float speed = Mathf.Sqrt(gravity * horizDist * horizDist / denom);
        speed = Mathf.Clamp(speed, 3f, 30f);

        Vector3 flatDir    = new Vector3(toTarget.x, 0f, toTarget.z).normalized;
        idealLaunchVelocity = flatDir * speed * cosA + Vector3.up * speed * sinA;
    }

    // ── Observations ──────────────────────────────────────────────────
    public override void CollectObservations(VectorSensor sensor)
    {
        if (hoopTransform == null)
        {
            sensor.AddObservation(new float[10]);
            return;
        }

        Vector3 toHoop = hoopTransform.position - transform.position;

        sensor.AddObservation(transform.localPosition);         // 3 — where am I
        sensor.AddObservation(toHoop);                          // 3 — hoop relative direction
        sensor.AddObservation(toHoop.magnitude / maxDistance);  // 1 — normalised distance
        sensor.AddObservation(idealLaunchVelocity.normalized);  // 3 — ideal aim hint
        // Total: 10  (matches Behavior Parameters > Space Size)
    }

    // ── Actions ────────────────────────────────────────────────────────
    public override void OnActionReceived(ActionBuffers actions)
    {
        if (!roundActive || ballThrown || currentBall == null) return;
        if (hoopTransform == null) return;

        // AI outputs corrections to the ideal trajectory.
        // [0] lateral offset,  [1] vertical offset,  [2] forward bias,  [3] speed scale
        float vx    = actions.ContinuousActions[0];
        float vy    = actions.ContinuousActions[1];
        float vz    = actions.ContinuousActions[2];
        float scale = actions.ContinuousActions[3];

        float speedScale = Mathf.Lerp(0.75f, 1.25f, (scale + 1f) / 2f);

        // Noise decays from large (explore) to zero (exploit) over first 3M steps
        float noiseDecay = Mathf.Lerp(1f, 0f,
            Mathf.Clamp01((float)StepCount / 3000000f));

        Vector3 noise = new Vector3(
            Random.Range(-1f, 1f) * noiseDecay * 2.5f,
            Random.Range(-0.5f, 0.5f) * noiseDecay * 1.5f,
            Random.Range(-1f, 1f) * noiseDecay * 2.5f);

        Vector3 flatDir  = new Vector3(idealLaunchVelocity.x, 0f, idealLaunchVelocity.z).normalized;
        Vector3 rightDir = Vector3.Cross(Vector3.up, flatDir).normalized;

        Vector3 aiCorrection = rightDir  * vx * 2f
                             + Vector3.up * vy * 2f
                             + flatDir    * vz * 1f;

        Vector3 finalVelocity = (idealLaunchVelocity + aiCorrection + noise) * speedScale;

        currentBall.isKinematic   = false;
        currentBall.linearVelocity = finalVelocity;
        ballThrown                = true;
        throwsLeft--;
        passedThroughHoopPlane    = false;
        prevBallY                 = currentBall.transform.position.y;

        AddReward(-0.002f); // small step penalty — prefer fewer wasted actions

        float flightTime = Mathf.Clamp(currentDistanceFromHoop / 8f, 1f, 4f);
        Invoke(nameof(AfterThrow), flightTime + 1.5f);

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
    }

    void Update()
    {
        if (!roundActive || currentBall == null || hoopTransform == null) return;

        // ── Through-hoop-plane detection ─────────────────────────────
        // Detect when the ball crosses Y=hoopY going DOWNWARD and is
        // within the hoop radius in XZ. This is the physics definition
        // of "ball passed through hoop".
        float ballY  = currentBall.transform.position.y;
        float hoopY  = hoopTransform.position.y;

        if (ballThrown && prevBallY > hoopY && ballY <= hoopY)
        {
            // Ball just crossed the hoop plane going downward
            Vector2 ballXZ = new Vector2(currentBall.transform.position.x,
                                          currentBall.transform.position.z);
            Vector2 hoopXZ = new Vector2(hoopTransform.position.x,
                                          hoopTransform.position.z);
            float xzDist = Vector2.Distance(ballXZ, hoopXZ);

            if (xzDist < hoopRadius * 1.5f)
            {
                // Ball passed through or very near the hoop opening!
                passedThroughHoopPlane = true;
                // Bonus proportional to accuracy (centre = +1.0, rim edge = +0.1)
                float accuracyBonus = Mathf.Lerp(0.1f, 1.0f,
                    1f - Mathf.Clamp01(xzDist / (hoopRadius * 1.5f)));
                AddReward(accuracyBonus);
                Debug.Log($"[Bot] Through-plane bonus: {accuracyBonus:F2}  xzDist={xzDist:F2}");
            }
        }
        prevBallY = ballY;
    }

    void AfterThrow()
    {
        // ── Reward shaping ────────────────────────────────────────────
        // Primary: did the ball pass through the hoop plane downward?
        // Secondary: angle of approach (steep arcs score more consistently)
        float reward = 0f;

        if (passedThroughHoopPlane)
        {
            // Already got accuracy bonus in Update; add small extra for completing arc
            reward += 0.3f;
        }
        else
        {
            // Punish flat/wide shots mildly
            reward -= 0.05f;
        }

        // Angle bonus: reward the ball arriving steeply (good arc)
        if (currentBall != null)
        {
            float vy = currentBall.linearVelocity.y;
            float vxz = new Vector2(currentBall.linearVelocity.x,
                                     currentBall.linearVelocity.z).magnitude;
            if (vxz > 0.1f)
            {
                float approachAngleDeg = Mathf.Atan2(Mathf.Abs(vy), vxz) * Mathf.Rad2Deg;
                if (vy < 0f && approachAngleDeg > 35f) // falling + steep enough
                    reward += 0.05f;
            }
        }

        AddReward(reward);
        CleanupBall();
        ballThrown = false;

        if (throwsLeft > 0)
        {
            SpawnBall();
        }
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
            currentBall.isKinematic           = true;
            currentBall.collisionDetectionMode = CollisionDetectionMode.Continuous;
            currentBall.interpolation          = RigidbodyInterpolation.Interpolate;
        }

        passedThroughHoopPlane = false;
        prevBallY = pos.y;
        CalculateIdealTrajectory();
    }

    /// <summary>
    /// Called by ScoreDetector when the ball physically enters the basket trigger.
    /// This is the HIGHEST reward — the only way to get it is a real basket.
    /// </summary>
    public void OnScore()
    {
        float distBonus = Mathf.Lerp(1f, 2f,
            currentDistanceFromHoop / maxDistance);
        AddReward(2.0f + distBonus);    // 3.0 – 4.0 depending on shot distance
        GameFlowManager.Instance?.AddScore(false);
        EndEpisode();
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        // Zero corrections = rely purely on the ideal physics trajectory
        var ca = actionsOut.ContinuousActions;
        ca[0] = 0f; ca[1] = 0f; ca[2] = 0f; ca[3] = 0f;
    }

    void OnDrawGizmos()
    {
        if (hoopTransform == null || ballSpawnPoint == null) return;

        // Ideal trajectory arc (blue)
        if (idealLaunchVelocity != Vector3.zero)
        {
            Gizmos.color = Color.blue;
            for (float t = 0; t < 4f; t += 0.05f)
            {
                Vector3 p1 = ballSpawnPoint.position + idealLaunchVelocity * t
                           + new Vector3(0f, 0.5f * Physics.gravity.y * t * t, 0f);
                Vector3 p2 = ballSpawnPoint.position + idealLaunchVelocity * (t + 0.05f)
                           + new Vector3(0f, 0.5f * Physics.gravity.y * (t + 0.05f) * (t + 0.05f), 0f);
                Gizmos.DrawLine(p1, p2);
                if (p1.y < -1f) break;
            }
        }

        // Hoop trigger zone (green = target, yellow = acceptance ring)
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(hoopTransform.position, 0.05f);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(new Vector3(
            hoopTransform.position.x, hoopTransform.position.y, hoopTransform.position.z),
            hoopRadius);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawLine(transform.position, hoopTransform.position);
    }
}
