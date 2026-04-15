using UnityEngine;

public class TrainingManager : MonoBehaviour
{
    public static TrainingManager Instance;

    void Awake()
    {
        Instance = this;
        Time.timeScale = 100f;         // maximum simulation speed
        Time.fixedDeltaTime = 0.02f;
        Application.targetFrameRate = 0;
        QualitySettings.vSyncCount = 0;
        QualitySettings.SetQualityLevel(0); // lowest quality = fastest

        // Disable camera rendering — pure simulation, no graphics cost
        foreach (Camera cam in FindObjectsByType<Camera>(FindObjectsSortMode.None))
            cam.enabled = false;
    }

    public bool IsGameActive() => true;
    public void AddScore(bool isPlayer) { }
}