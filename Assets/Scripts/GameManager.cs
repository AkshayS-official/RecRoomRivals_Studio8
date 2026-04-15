using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    void Awake() => Instance = this;
    public void AddScore(bool isPlayer) => GameFlowManager.Instance?.AddScore(isPlayer);
    public bool IsGameActive() => GameFlowManager.Instance?.IsGameActive() ?? true;
}