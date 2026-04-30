using UnityEngine;

public class RewardSpawnManager : MonoBehaviour
{
    public static RewardSpawnManager Instance;
    
    [Header("Settings")]
    public float spawnDistanceInFront = 2f;
    public float spawnHeightOffset = -0.3f;
    
    private bool hasSpawned = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void Start()
    {
        if (LocationTriggerManager.Instance != null)
        {
            LocationTriggerManager.Instance.OnLocationTriggered += SpawnReward;
        }
    }

    private void OnDestroy()
    {
        if (LocationTriggerManager.Instance != null)
        {
            LocationTriggerManager.Instance.OnLocationTriggered -= SpawnReward;
        }
    }

    private void SpawnReward()
    {
        if (hasSpawned) return;
        
        Camera mainCam = Camera.main;
        if (mainCam == null)
        {
            Debug.LogError("[RewardSpawnManager] Main camera not found!");
            return;
        }

        // Get the model prefab key from GameSession
        string prefabKey = GameSession.selectedSpotModelPrefabKey;
        GameObject prefab = RewardPreviewModelResolver.ResolvePrefab(prefabKey, "", "");

        if (prefab == null)
        {
            Debug.LogWarning($"[RewardSpawnManager] Prefab not found for key {prefabKey}. Using fallback.");
            prefab = RewardPreviewModelResolver.CreateFallbackPreviewObject();
        }

        // Calculate spawn position in front of the user
        Vector3 spawnPos = mainCam.transform.position + (mainCam.transform.forward * spawnDistanceInFront);
        spawnPos.y += spawnHeightOffset;

        // Instantiate the model directly
        GameObject rewardInstance = Instantiate(prefab, spawnPos, Quaternion.identity);
        rewardInstance.name = "ActiveRewardItem";
        
        // Add a collider and the collect script so the user can tap it
        if (rewardInstance.GetComponent<Collider>() == null)
        {
            rewardInstance.AddComponent<BoxCollider>();
        }
        
        RewardCollectManager collectManager = rewardInstance.AddComponent<RewardCollectManager>();
        collectManager.Initialize(GameSession.rewardName, GameSession.rewardDescription);

        hasSpawned = true;
        Debug.Log("[RewardSpawnManager] Reward spawned directly at: " + spawnPos);
        
        // Notify UI
        UIManager.Instance?.ShowToast("Reward ил боллоо!");
    }
}
