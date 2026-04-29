using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NativeBridgeManager : MonoBehaviour
{
    [Serializable]
    private class SpotBridgePayload
    {
        public string mode;
        public string userId;
        public string id;
        public string spotId;
        public string spotName;
        public string spotDescription;
        public double spotLatitude;
        public double spotLongitude;
        public float spotRadiusMeters;
        public string modelPrefabKey;
        public string rewardId;
        public string rewardName;
        public string rewardDescription;
        public string rewardImageUrl;
        public string rewardPreviewPrefabKey;
        public string previewPrefabKey;
        public string claimedAtRaw;
        public bool hasCurrentLocation;
        public double currentLatitude;
        public double currentLongitude;
        public float currentHorizontalAccuracyMeters;
        public bool hasCurrentHeading;
        public float currentHeadingDegrees;
    }

    public static NativeBridgeManager Instance;
    private const string BridgeObjectName = "NativeBridgeManager";
    private const float DuplicatePayloadWindowSeconds = 0.8f;

    // Optional event for other scripts to subscribe
    public event Action<string> OnSpotDataReceived;

    private bool isNomadARLoadPending;
    private string lastPayloadJson = string.Empty;
    private float lastPayloadReceivedAt = -10f;

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void UnityMessage(string message);
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject(BridgeObjectName);
        bootstrapObject.AddComponent<NativeBridgeManager>();
    }

    private void Awake()
    {
        // Prevent IL2CPP from stripping the method called only by iOS Objective-C
        if (Time.frameCount == -9999) 
        {
            ReceiveSpotDataFromRN("");
        }

        if (Instance == null)
        {
            Instance = this;
            gameObject.name = BridgeObjectName;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Called by React Native via postMessage('GameObject', 'ReceiveSpotDataFromRN', jsonString)
    /// </summary>
    public void ReceiveSpotDataFromRN(string jsonString)
    {
        Debug.Log("[NativeBridge] Received spot data from RN: " + jsonString);
        OnSpotDataReceived?.Invoke(jsonString);

        if (IsDuplicatePayload(jsonString))
        {
            Debug.Log("[NativeBridge] Duplicate payload ignored.");
            return;
        }

        SpotBridgePayload payload = TryParseSpotPayload(jsonString);
        if (payload == null)
        {
            GameSession.ClearCollectionPreviewData();
            GameSession.selectedSpotId = jsonString;
            return;
        }

        if (!string.IsNullOrEmpty(payload.userId))
        {
            GameSession.userId = payload.userId;
        }

        ApplyCurrentLocation(payload);
        SendStatusToRN("payload_received");

        if (string.Equals(payload.mode, "collectionPreview", StringComparison.OrdinalIgnoreCase))
        {
            GameSession.SetCollectionPreviewData(
                payload.rewardId,
                payload.rewardName,
                payload.rewardDescription,
                payload.rewardImageUrl,
                payload.previewPrefabKey,
                payload.claimedAtRaw,
                payload.spotName);

            EnsureNomadARLoadedOrRefreshed();
            return;
        }

        GameSession.ClearCollectionPreviewData();
        GameSession.SetSpotData(
            !string.IsNullOrEmpty(payload.spotId) ? payload.spotId : payload.id,
            payload.spotName,
            payload.spotDescription,
            payload.rewardName,
            payload.rewardDescription,
            payload.rewardImageUrl,
            !string.IsNullOrEmpty(payload.rewardPreviewPrefabKey) ? payload.rewardPreviewPrefabKey : payload.previewPrefabKey,
            payload.spotLatitude,
            payload.spotLongitude,
            payload.spotRadiusMeters,
            payload.modelPrefabKey);
        EnsureNomadARLoadedOrRefreshed();
    }

    /// <summary>
    /// Call this from ARChestSpawner or Collectible to notify RN
    /// </summary>
    public void SendSuccessToRN(string rewardId)
    {
        string message = "{\"status\":\"collected\", \"rewardId\":\"" + rewardId + "\"}";
        Debug.Log("[NativeBridge] Sending message to RN: " + message);
        SendMessageToRN(message);
    }

    public void SendReadyToRN()
    {
        SendStatusToRN("ready");
    }

    public void SendErrorToRN(string code, string message)
    {
        SendStatusToRN("error", code, message);
    }

    public void SendStatusToRN(string status, string code = "", string message = "")
    {
        if (string.IsNullOrEmpty(status))
        {
            return;
        }

        string payload = "{\"status\":\"" + EscapeJson(status) + "\"";

        if (!string.IsNullOrEmpty(code))
        {
            payload += ",\"code\":\"" + EscapeJson(code) + "\"";
        }

        if (!string.IsNullOrEmpty(message))
        {
            payload += ",\"message\":\"" + EscapeJson(message) + "\"";
        }

        if (string.Equals(status, "ready", StringComparison.OrdinalIgnoreCase))
        {
            string modeValue = GameSession.isCollectionPreviewMode ? "collectionPreview" : "spot";
            payload += ",\"scene\":\"ARScene\",\"mode\":\"" + EscapeJson(modeValue) + "\"";
        }

        payload += "}";
        Debug.Log("[NativeBridge] Sending status to RN: " + payload);
        SendMessageToRN(payload);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!string.Equals(scene.name, "ARScene", StringComparison.Ordinal))
        {
            return;
        }

        isNomadARLoadPending = false;
    }

    private System.Collections.IEnumerator SendReadyMessageNextFrame()
    {
        yield return null;
        SendReadyToRN();
    }

    private void EnsureNomadARLoadedOrRefreshed()
    {
        if (!Application.CanStreamedLevelBeLoaded("ARScene"))
        {
            SendErrorToRN("ar_scene_missing", "ARScene is not included in the Unity build settings.");
            return;
        }

        string activeSceneName = SceneManager.GetActiveScene().name;
        if (!string.Equals(activeSceneName, "ARScene", StringComparison.Ordinal))
        {
            if (isNomadARLoadPending)
            {
                return;
            }

            isNomadARLoadPending = true;
            SendStatusToRN("loading_scene");
            Debug.Log("[NativeBridge] Loading ARScene from bridge payload.");
            SceneManager.LoadScene("ARScene");
            return;
        }

        if (GameSession.isCollectionPreviewMode)
        {
            CollectedRewardPreviewController.Instance?.RefreshPreviewIfNeeded();
            CollectedRewardARPreviewSpawner.Instance?.RefreshPreviewIfNeeded();
            StartCoroutine(SendReadyMessageNextFrame());
            return;
        }

        if (isNomadARLoadPending)
        {
            return;
        }

        isNomadARLoadPending = true;
        SendStatusToRN("reloading_scene");
        Debug.Log("[NativeBridge] Reloading ARScene to apply updated spot payload.");
        SceneManager.LoadScene("ARScene");
    }

    private void ApplyCurrentLocation(SpotBridgePayload payload)
    {
        if (payload.hasCurrentLocation)
        {
            GameSession.SetCurrentLocation(
                payload.currentLatitude,
                payload.currentLongitude,
                payload.currentHorizontalAccuracyMeters);
        }

        if (payload.hasCurrentHeading &&
            !float.IsNaN(payload.currentHeadingDegrees) &&
            !float.IsInfinity(payload.currentHeadingDegrees))
        {
            GameSession.SetCurrentHeading(payload.currentHeadingDegrees);
        }
    }

    private bool IsDuplicatePayload(string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            return false;
        }

        float now = Time.realtimeSinceStartup;
        bool isDuplicate = string.Equals(lastPayloadJson, jsonString, StringComparison.Ordinal) &&
            (now - lastPayloadReceivedAt) < DuplicatePayloadWindowSeconds;

        lastPayloadJson = jsonString;
        lastPayloadReceivedAt = now;
        return isDuplicate;
    }

    private void SendMessageToRN(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

#if UNITY_IOS && !UNITY_EDITOR
        UnityMessage(message);
#elif UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass jc = new AndroidJavaClass("com.azesmwayreactnativeunity.ReactNativeUnityViewManager"))
            {
                jc.CallStatic("sendMessageToMobileApp", message);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[NativeBridge] Error sending Android message: " + e.Message);
        }
#else
        Debug.Log("[NativeBridge] Simulated message sent to RN: " + message);
#endif
    }

    private static string EscapeJson(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\r", string.Empty)
            .Replace("\n", "\\n");
    }

    private static SpotBridgePayload TryParseSpotPayload(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<SpotBridgePayload>(json);
        }
        catch
        {
            return null;
        }
    }
}
