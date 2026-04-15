using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ThrowVisualizer))]
public class ThrowVisualizerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        ThrowVisualizer vis = (ThrowVisualizer)target;
        DrawDefaultInspector();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("── Status ──", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Recorded: {vis.throwCount} / {vis.maxThrowsToRecord}");
        EditorGUILayout.LabelField($"State: {(vis.isPaused ? "⏸ PAUSED" : "🔴 Recording")}");

        EditorGUILayout.Space(5);
        if (GUILayout.Button("▶ Resume / Clear & Record Again"))
        {
            vis.ClearRecordings();
            EditorApplication.isPaused = false;
            SceneView.RepaintAll();
        }
        if (GUILayout.Button("⏸ Force Pause"))
        {
            vis.isPaused = true;
            EditorApplication.isPaused = true;
        }

        if (vis.recordedThrows.Count > 0)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("── Stats ──", EditorStyles.boldLabel);

            int hits = 0, close = 0, miss = 0;
            float avg = 0f;
            int realThrows = 0;

            foreach (var r in vis.recordedThrows)
            {
                if (r.isIdeal) continue; // skip ideal line
                realThrows++;
                avg += r.closestDist;
                if (r.closestDist < 0.4f) hits++;
                else if (r.closestDist < 1.0f) close++;
                else miss++;
            }

            if (realThrows > 0) avg /= realThrows;

            EditorGUILayout.LabelField($"🟢 Near hoop (<0.4m): {hits}");
            EditorGUILayout.LabelField($"🟡 Close   (0.4-1m): {close}");
            EditorGUILayout.LabelField($"🔴 Miss      (>1m):  {miss}");
            EditorGUILayout.LabelField($"📏 Avg closest:      {avg:F2}m");
            EditorGUILayout.LabelField($"🔵 Ideal lines shown: {vis.recordedThrows.FindAll(r => r.isIdeal).Count}");
        }
    }
}