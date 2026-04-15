using UnityEngine;
using System.Collections.Generic;

public class ThrowVisualizer : MonoBehaviour
{
    [Header("Settings")]
    public int maxThrowsToRecord = 100;
    public float simulationStep = 0.05f;
    public float simulationTime = 5f;

    [Header("Colors")]
    public Color missColor = Color.red;
    public Color closeColor = Color.yellow;
    public Color hitColor = Color.green;
    public Color idealColor = Color.blue; // perfect trajectory

    [System.Serializable]
    public class ThrowRecord
    {
        public List<Vector3> points = new List<Vector3>();
        public float closestDist = float.MaxValue;
        public bool isIdeal = false;
    }

    public List<ThrowRecord> recordedThrows = new List<ThrowRecord>();
    public int throwCount = 0;
    public bool isPaused = false;

    // Record actual throw
    public void RecordThrow(Vector3 startPos, Vector3 velocity, Transform hoop)
    {
        if (isPaused) return;
        RecordLine(startPos, velocity, hoop, false);
        throwCount++;

        if (throwCount >= maxThrowsToRecord)
        {
            isPaused = true;
            int hits = 0, close = 0, miss = 0;
            foreach (var r in recordedThrows)
            {
                if (r.isIdeal) continue;
                if (r.closestDist < 0.4f) hits++;
                else if (r.closestDist < 1.0f) close++;
                else miss++;
            }
            Debug.Log($"✅ Done! 🟢{hits} 🟡{close} 🔴{miss}");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPaused = true;
#endif
        }
    }

    // Record ideal trajectory (blue line)
    public void RecordIdealTrajectory(Vector3 startPos, Vector3 velocity, Transform hoop)
    {
        if (isPaused) return;
        // Only keep latest ideal line — remove old ones
        recordedThrows.RemoveAll(r => r.isIdeal);
        RecordLine(startPos, velocity, hoop, true);
    }

    void RecordLine(Vector3 startPos, Vector3 velocity, Transform hoop, bool isIdeal)
    {
        ThrowRecord record = new ThrowRecord();
        record.isIdeal = isIdeal;

        Vector3 pos = startPos;
        Vector3 vel = velocity;
        float gravity = Physics.gravity.y;
        float closest = float.MaxValue;

        record.points.Add(pos);

        for (float t = 0; t < simulationTime; t += simulationStep)
        {
            vel.y += gravity * simulationStep;
            pos += vel * simulationStep;
            record.points.Add(pos);

            if (hoop != null)
            {
                float d = Vector3.Distance(pos, hoop.position);
                if (d < closest) closest = d;
            }

            if (pos.y < -2f) break;
        }

        record.closestDist = closest;
        recordedThrows.Add(record);
    }

    public void ClearRecordings()
    {
        recordedThrows.Clear();
        throwCount = 0;
        isPaused = false;
    }

    void OnDrawGizmos()
    {
        foreach (var record in recordedThrows)
        {
            if (record.isIdeal)
            {
                // BLUE = perfect trajectory
                Gizmos.color = idealColor;
                // Draw thicker by drawing twice with slight offset
                for (int i = 0; i < record.points.Count - 1; i++)
                {
                    Gizmos.DrawLine(record.points[i], record.points[i + 1]);
                    Gizmos.DrawLine(
                        record.points[i] + Vector3.right * 0.02f,
                        record.points[i + 1] + Vector3.right * 0.02f
                    );
                }
            }
            else
            {
                // Color by closest approach
                Gizmos.color =
                    record.closestDist < 0.4f ? hitColor :
                    record.closestDist < 1.0f ? closeColor : missColor;

                for (int i = 0; i < record.points.Count - 1; i++)
                    Gizmos.DrawLine(record.points[i], record.points[i + 1]);

                // Dot at end
                Gizmos.color = Color.white;
                if (record.points.Count > 0)
                    Gizmos.DrawSphere(record.points[^1], 0.04f);
            }
        }
    }
}