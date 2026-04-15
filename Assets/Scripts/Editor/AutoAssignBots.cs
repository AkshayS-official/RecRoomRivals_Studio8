using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;

public class AutoAssignBots : EditorWindow
{
    // Exact local position from your working NetTopSpawnPoint
    static readonly Vector3 NET_TOP_LOCAL_POS = new Vector3(-0.053f, 0.45f, -0.188f);

    [MenuItem("Tools/Setup All Training Bots")]
    static void SetupAll()
    {
        AssignAll();
        RemoveNavMeshAgents();
        Debug.Log("✅ Full training bot setup complete!");
    }

    [MenuItem("Tools/Auto Assign Bot References")]
    static void AssignAll()
    {
        ShooterBotAgent[] allBots = FindObjectsByType<ShooterBotAgent>(
            FindObjectsSortMode.None);
        int assigned = 0;

        foreach (ShooterBotAgent bot in allBots)
        {
            Transform parent = bot.transform.parent;
            if (parent == null) continue;

            Transform net = FindInChildren(parent, "Net");

            if (net != null)
            {
                Transform netTopSpawn = net.Find("NetTopSpawnPoint");

                if (netTopSpawn == null)
                {
                    // Create fresh
                    GameObject go = new GameObject("NetTopSpawnPoint");
                    go.transform.SetParent(net);
                    go.transform.localPosition = NET_TOP_LOCAL_POS;
                    go.transform.localRotation = Quaternion.identity;
                    go.transform.localScale = Vector3.one;
                    netTopSpawn = go.transform;
                    Debug.Log($"Created NetTopSpawnPoint on {parent.name}");
                }
                else
                {
                    // FORCE correct position regardless of current value
                    netTopSpawn.localPosition = NET_TOP_LOCAL_POS;
                    netTopSpawn.localRotation = Quaternion.identity;
                    netTopSpawn.localScale = Vector3.one;
                    EditorUtility.SetDirty(netTopSpawn.gameObject);
                }

                bot.hoopTransform = netTopSpawn;
                assigned++;
            }

            Transform ballSpawn = FindInChildren(bot.transform, "BallSpawnPoint");
            if (ballSpawn != null)
                bot.ballSpawnPoint = ballSpawn;

            bot.minDistance = 5f;
            bot.maxDistance = 20f;
            bot.minLaunchAngle = 52f;
            bot.maxLaunchAngle = 72f;
            bot.MaxStep = 300;

            EditorUtility.SetDirty(bot);
        }

        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"✅ Assigned {assigned} bots. All NetTopSpawnPoint → {NET_TOP_LOCAL_POS}");
    }

    [MenuItem("Tools/Remove NavMeshAgent From Training Bots")]
    static void RemoveNavMeshAgents()
    {
        ShooterBotAgent[] allBots = FindObjectsByType<ShooterBotAgent>(
            FindObjectsSortMode.None);
        int removed = 0;

        foreach (ShooterBotAgent bot in allBots)
        {
            NavMeshAgent nma = bot.GetComponent<NavMeshAgent>();
            if (nma != null)
            {
                DestroyImmediate(nma);
                bot.navAgent = null;
                EditorUtility.SetDirty(bot);
                removed++;
            }
        }

        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"✅ Removed NavMeshAgent from {removed} bots!");
    }

    [MenuItem("Tools/Verify Bot Setup")]
    static void VerifySetup()
    {
        ShooterBotAgent[] allBots = FindObjectsByType<ShooterBotAgent>(
            FindObjectsSortMode.None);

        int ok = 0, broken = 0;
        foreach (ShooterBotAgent bot in allBots)
        {
            bool hasHoop = bot.hoopTransform != null;
            bool hasBall = bot.ballSpawnPoint != null;
            bool hasPrefab = bot.basketballPrefab != null;

            if (hasHoop && hasBall && hasPrefab)
            {
                ok++;
                Vector3 lp = bot.hoopTransform.localPosition;
                bool posOk = Vector3.Distance(lp, NET_TOP_LOCAL_POS) < 0.01f;
                if (!posOk)
                    Debug.LogWarning($"⚠️ {bot.transform.parent?.name} " +
                        $"wrong pos: {lp} (expected {NET_TOP_LOCAL_POS})");
                else
                    Debug.Log($"✅ {bot.transform.parent?.name} OK");
            }
            else
            {
                broken++;
                Debug.LogError($"❌ {bot.transform.parent?.name}: " +
                    $"hoop={hasHoop} ball={hasBall} prefab={hasPrefab}");
            }
        }

        Debug.Log($"Verify: ✅{ok} ok ❌{broken} broken / {allBots.Length} total");
    }

    static Transform FindInChildren(Transform parent, string name)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
            if (child.name == name) return child;
        return null;
    }
}