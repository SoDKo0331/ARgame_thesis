using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackendBootstrap : MonoBehaviour
{
    [Serializable]
    private sealed class TourismSpotCacheEnvelope
    {
        public List<TourismSpot> spots = new List<TourismSpot>();
        public string savedAt;
        public string sourceBaseUrl;
    }

    public enum BackendConnectionState
    {
        Idle,
        Connecting,
        Online,
        Degraded,
        Offline
    }

    public static BackendBootstrap Instance { get; private set; }

    private const float NearbySpotRadiusMeters = 5000f;
    private const int NearbySpotLimit = 100;

    private readonly List<TourismSpot> cachedTourismSpots = new List<TourismSpot>();

    private AuthApiService authApiService;
    private HealthApiService healthApiService;
    private SpotApiService spotApiService;
    private RewardApiService rewardApiService;

    private bool isGuestLoginInProgress;
    private bool isSpotFetchInProgress;
    private bool hasBootstrappedSession;
    private Coroutine bootstrapRoutine;
    private Coroutine claimRoutine;
    private int bootstrapRunId;
    private string cachedSpotsPath;

    public bool HasBootstrappedSession => hasBootstrappedSession;
    public bool IsBusy => isGuestLoginInProgress || isSpotFetchInProgress || bootstrapRoutine != null;
    public bool IsConnected => ConnectionState == BackendConnectionState.Online;
    public bool IsClaimInProgress => claimRoutine != null;
    public BackendConnectionState ConnectionState { get; private set; } = BackendConnectionState.Idle;
    public string ConnectionTitle { get; private set; } = "Idle";
    public string ConnectionDetail { get; private set; } = "Waiting to connect.";
    public string LastStatusMessage { get; private set; } = string.Empty;
    public string LastErrorMessage { get; private set; } = string.Empty;
    public string LastSuccessfulSyncTime { get; private set; } = string.Empty;
    public string LastSuccessfulBaseUrl { get; private set; } = string.Empty;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("BackendBootstrap");
        bootstrapObject.AddComponent<BackendBootstrap>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        ApiClient apiClient = new ApiClient();
        authApiService = new AuthApiService(apiClient);
        healthApiService = new HealthApiService(apiClient);
        spotApiService = new SpotApiService(apiClient);
        rewardApiService = new RewardApiService(apiClient);
        cachedSpotsPath = Path.Combine(Application.persistentDataPath, "backend_spots_cache.json");
        LoadCachedTourismSpotsFromDisk();

#if UNITY_IOS || UNITY_ANDROID
        if (!Application.isEditor && ApiConfig.UsesLocalhost)
        {
            Debug.LogWarning(
                "ApiConfig.BaseUrl is pointing to localhost. " +
                "Set PlayerPrefs '" + ApiConfig.BaseUrlOverridePlayerPrefsKey + "' to your backend LAN IP before device testing.");
        }
#endif

        string storedUserId = PlayerPrefs.GetString(ApiConfig.UserIdPlayerPrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(storedUserId))
        {
            GameSession.userId = storedUserId;
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

    private void Start()
    {
        Debug.Log("[BackendBootstrap] Start => bootstrapping backend session.");
        RunBootstrapSession();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[BackendBootstrap] Scene loaded => " + scene.name);
        if (scene.name == "MainScene")
        {
            StartCoroutine(HandleMainSceneLoaded());
        }
    }

    private IEnumerator HandleMainSceneLoaded()
    {
        yield return null;

        TourismSpotManager tourismSpotManager = FindObjectOfType<TourismSpotManager>();
        if (tourismSpotManager != null && cachedTourismSpots.Count > 0)
        {
            tourismSpotManager.SetTourismSpots(CloneTourismSpots(cachedTourismSpots));
        }

        RunBootstrapSession();
    }

    private IEnumerator BootstrapSession(int runId)
    {
        yield return StartCoroutine(ProbeBackendHealth(runId));
        if (runId != bootstrapRunId || ConnectionState != BackendConnectionState.Connecting)
        {
            if (runId == bootstrapRunId)
            {
                bootstrapRoutine = null;
            }

            yield break;
        }

        yield return StartCoroutine(EnsureGuestLogin(runId));
        if (runId != bootstrapRunId || !hasBootstrappedSession)
        {
            if (runId == bootstrapRunId)
            {
                bootstrapRoutine = null;
            }

            yield break;
        }

        yield return StartCoroutine(FetchAndInjectTourismSpots(runId));
        if (runId == bootstrapRunId)
        {
            bootstrapRoutine = null;
        }
    }

    private void RunBootstrapSession()
    {
        if (bootstrapRoutine != null)
        {
            Debug.Log("[BackendBootstrap] RunBootstrapSession ignored because bootstrap is already running.");
            return;
        }

        bootstrapRunId++;
        Debug.Log("[BackendBootstrap] RunBootstrapSession => runId=" + bootstrapRunId);
        bootstrapRoutine = StartCoroutine(BootstrapSession(bootstrapRunId));
    }

    public void RefreshSession()
    {
        Debug.Log("[BackendBootstrap] RefreshSession requested.");
        if (bootstrapRoutine != null)
        {
            StopCoroutine(bootstrapRoutine);
            bootstrapRoutine = null;
        }

        isGuestLoginInProgress = false;
        isSpotFetchInProgress = false;
        hasBootstrappedSession = false;

        GameSession.userId = string.Empty;
        PlayerPrefs.DeleteKey(ApiConfig.UserIdPlayerPrefsKey);
        ApiConfig.ClearAccessToken();
        PlayerPrefs.Save();

        SetConnectionState(
            BackendConnectionState.Connecting,
            "Reconnecting",
            "Trying " + ApiConfig.BaseUrl,
            string.Empty);

        RunBootstrapSession();
    }

    private IEnumerator ProbeBackendHealth(int runId)
    {
        Debug.Log("[BackendBootstrap] ProbeBackendHealth => runId=" + runId + ", baseUrl=" + ApiConfig.BaseUrl);
        SetConnectionState(
            BackendConnectionState.Connecting,
            "Checking",
            "Checking backend server...",
            string.Empty);

        HealthCheckResponseDto response = null;
        ApiClientError error = null;

        yield return StartCoroutine(healthApiService.Ping(
            value => response = value,
            apiError => error = apiError));

        if (runId != bootstrapRunId)
        {
            yield break;
        }

        if (response != null && string.Equals(response.status, "ok", StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log("[BackendBootstrap] Health check OK.");
            SetConnectionState(
                BackendConnectionState.Connecting,
                "Connecting",
                "Backend server is reachable. Starting session...",
                string.Empty);
            yield break;
        }

        string failureMessage = error != null ? error.message : "Backend health check failed.";
        Debug.LogWarning("[BackendBootstrap] Health check failed: " + failureMessage);
        bool hasCache = ApplyCachedTourismSpotsToScene();
        SetConnectionState(
            hasCache ? BackendConnectionState.Degraded : BackendConnectionState.Offline,
            hasCache ? "Cached Fallback" : "Offline Fallback",
            hasCache
                ? "Backend is unreachable. Using cached tourism spots."
                : "Backend is unreachable and no cached spots are available.",
            failureMessage);
    }

    private IEnumerator EnsureGuestLogin(int runId)
    {
        if (hasBootstrappedSession || isGuestLoginInProgress)
        {
            Debug.Log("[BackendBootstrap] EnsureGuestLogin skipped. hasBootstrappedSession=" + hasBootstrappedSession + ", inProgress=" + isGuestLoginInProgress);
            yield break;
        }

        isGuestLoginInProgress = true;
        SetConnectionState(
            BackendConnectionState.Connecting,
            "Connecting",
            "Signing in guest user...",
            string.Empty);

        string deviceId = GetOrCreateDeviceId();
        Debug.Log("[BackendBootstrap] Guest login started. deviceId=" + deviceId);
        GuestLoginResponseDto response = null;
        ApiClientError error = null;

        yield return StartCoroutine(authApiService.GuestLogin(
            deviceId,
            ApiConfig.DefaultGuestDisplayName,
            value => response = value,
            apiError => error = apiError));

        isGuestLoginInProgress = false;

        if (runId != bootstrapRunId)
        {
            yield break;
        }

        if (response != null && response.user != null && !string.IsNullOrEmpty(response.user.id))
        {
            Debug.Log("[BackendBootstrap] Guest login succeeded. userId=" + response.user.id + ", isNewUser=" + response.isNewUser);
            hasBootstrappedSession = true;
            GameSession.userId = response.user.id;
            PlayerPrefs.SetString(ApiConfig.UserIdPlayerPrefsKey, response.user.id);
            ApiConfig.SetAccessToken(response.accessToken);
            PlayerPrefs.Save();

            SetConnectionState(
                BackendConnectionState.Connecting,
                "Connected",
                "Guest session ready. Syncing tourism spots...",
                string.Empty);
            yield break;
        }

        string failureMessage = error != null ? error.message : "Guest login failed.";
        SetConnectionState(
            BackendConnectionState.Offline,
            "Offline Fallback",
            "Guest login failed. Using local fallback session.",
            failureMessage);
        Debug.LogWarning("Backend guest login failed: " + LastErrorMessage);
    }

    private IEnumerator FetchAndInjectTourismSpots(int runId)
    {
        if (!hasBootstrappedSession || isSpotFetchInProgress)
        {
            Debug.Log("[BackendBootstrap] FetchAndInjectTourismSpots skipped. hasBootstrappedSession=" + hasBootstrappedSession + ", inProgress=" + isSpotFetchInProgress);
            yield break;
        }

        isSpotFetchInProgress = true;
        bool useNearbySpots = GameSession.hasCurrentLocation;
        SetConnectionState(
            BackendConnectionState.Connecting,
            "Connected",
            useNearbySpots ? "Loading nearby tourism spots..." : "Loading tourism spots...",
            string.Empty);

        SpotsResponseDto response = null;
        ApiClientError error = null;

        if (useNearbySpots)
        {
            yield return StartCoroutine(spotApiService.GetNearbySpots(
                GameSession.currentLatitude,
                GameSession.currentLongitude,
                NearbySpotRadiusMeters,
                NearbySpotLimit,
                value => response = value,
                apiError => error = apiError));

            if (response == null)
            {
                Debug.LogWarning(
                    "[BackendBootstrap] Nearby spot fetch failed, falling back to full spot list. " +
                    "reason=" + (error != null ? error.message : "unknown"));
                error = null;

                yield return StartCoroutine(spotApiService.GetSpots(
                    value => response = value,
                    apiError => error = apiError));
            }
        }
        else
        {
            yield return StartCoroutine(spotApiService.GetSpots(
                value => response = value,
                apiError => error = apiError));
        }

        isSpotFetchInProgress = false;

        if (runId != bootstrapRunId)
        {
            yield break;
        }

        if (response != null && response.spots != null)
        {
            Debug.Log(
                "[BackendBootstrap] Spot fetch succeeded. nearby=" + useNearbySpots +
                ", count=" + response.spots.Count);
            cachedTourismSpots.Clear();
            cachedTourismSpots.AddRange(MapSpots(response.spots));
            SaveCachedTourismSpotsToDisk();

            TourismSpotManager tourismSpotManager = FindObjectOfType<TourismSpotManager>();
            if (tourismSpotManager != null)
            {
                tourismSpotManager.SetTourismSpots(CloneTourismSpots(cachedTourismSpots));
            }

            LastSuccessfulBaseUrl = ApiConfig.BaseUrl;
            LastSuccessfulSyncTime = DateTime.Now.ToString("HH:mm:ss");
            SetConnectionState(
                BackendConnectionState.Online,
                "Connected",
                response.spots.Count > 0
                    ? (tourismSpotManager != null
                        ? ((useNearbySpots ? "Nearby tourism spots loaded at " : "Backend tourism spots loaded at ") + LastSuccessfulSyncTime)
                        : ((useNearbySpots ? "Nearby tourism spots cached at " : "Backend tourism spots cached at ") + LastSuccessfulSyncTime))
                    : (useNearbySpots
                        ? "Backend is live, but no nearby tourism spots were found."
                        : "Backend is live, but no tourism spots are available yet."),
                string.Empty);
            yield break;
        }

        string fetchFailureMessage = error != null ? error.message : "Failed to load tourism spots.";
        Debug.LogWarning("[BackendBootstrap] Spot fetch failed: " + fetchFailureMessage);
        bool hasCache = ApplyCachedTourismSpotsToScene();
        SetConnectionState(
            hasCache ? BackendConnectionState.Degraded : BackendConnectionState.Offline,
            hasCache ? "Connected with Fallback" : "Offline Fallback",
            hasCache
                ? "Guest session is ready, but cached tourism spots are in use."
                : "Using local tourism spot fallback.",
            fetchFailureMessage);
        Debug.LogWarning("Backend tourism spot fetch failed: " + LastErrorMessage);
    }

    public IEnumerator ClaimSelectedSpot(
        Action<ClaimRewardResponseDto> onSuccess,
        Action<ApiClientError> onError)
    {
        if (!hasBootstrappedSession)
        {
            Debug.LogWarning("[BackendBootstrap] ClaimSelectedSpot blocked because session is not ready.");
            onError?.Invoke(new ApiClientError
            {
                message = "Guest session is not ready yet."
            });
            yield break;
        }

        if (string.IsNullOrEmpty(GameSession.userId) || string.IsNullOrEmpty(GameSession.selectedSpotId))
        {
            Debug.LogWarning("[BackendBootstrap] ClaimSelectedSpot blocked because userId or selectedSpotId is missing.");
            onError?.Invoke(new ApiClientError
            {
                message = "Missing selected spot or user session."
            });
            yield break;
        }

        ClaimRewardResponseDto response = null;
        ApiClientError error = null;

        Debug.Log("[BackendBootstrap] ClaimSelectedSpot started. spotId=" + GameSession.selectedSpotId + ", userId=" + GameSession.userId);

        yield return StartCoroutine(rewardApiService.ClaimReward(
            GameSession.selectedSpotId,
            GameSession.userId,
            value => response = value,
            apiError => error = apiError));

        if (response != null)
        {
            Debug.Log("[BackendBootstrap] ClaimSelectedSpot succeeded. alreadyClaimed=" + response.alreadyClaimed);
            RewardDto reward = response.reward;

            if (reward == null && response.claim != null)
            {
                reward = response.claim.reward;
            }

            if (reward != null)
            {
                GameSession.SetRewardData(
                    reward.name,
                    reward.description,
                    reward.imageUrl,
                    reward.previewPrefabKey,
                    response.alreadyClaimed);
            }

            onSuccess?.Invoke(response);
            yield break;
        }

        Debug.LogWarning("[BackendBootstrap] ClaimSelectedSpot failed: " + ((error != null) ? error.message : "Failed to claim reward."));
        onError?.Invoke(error ?? new ApiClientError
        {
            message = "Failed to claim reward."
        });
    }

    public void StartClaimSelectedSpotRequest(
        Action<ClaimRewardResponseDto> onSuccess,
        Action<ApiClientError> onError)
    {
        if (claimRoutine != null)
        {
            Debug.Log("[BackendBootstrap] StartClaimSelectedSpotRequest ignored because a claim is already running.");
            return;
        }

        Debug.Log("[BackendBootstrap] StartClaimSelectedSpotRequest => begin.");
        claimRoutine = StartCoroutine(RunClaimSelectedSpotRequest(onSuccess, onError));
    }

    private IEnumerator RunClaimSelectedSpotRequest(
        Action<ClaimRewardResponseDto> onSuccess,
        Action<ApiClientError> onError)
    {
        yield return StartCoroutine(ClaimSelectedSpot(onSuccess, onError));
        claimRoutine = null;
    }

    private static string GetOrCreateDeviceId()
    {
        string storedDeviceId = PlayerPrefs.GetString(ApiConfig.DeviceIdPlayerPrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(storedDeviceId))
        {
            return storedDeviceId;
        }

        string rawDeviceId = SystemInfo.deviceUniqueIdentifier;
        if (string.IsNullOrEmpty(rawDeviceId) || rawDeviceId == "n/a" || rawDeviceId == "Unknown")
        {
            rawDeviceId = Guid.NewGuid().ToString("N");
        }

        storedDeviceId = "unity-" + rawDeviceId;
        PlayerPrefs.SetString(ApiConfig.DeviceIdPlayerPrefsKey, storedDeviceId);
        PlayerPrefs.Save();
        return storedDeviceId;
    }

    private void LoadCachedTourismSpotsFromDisk()
    {
        if (string.IsNullOrEmpty(cachedSpotsPath) || !File.Exists(cachedSpotsPath))
        {
            return;
        }

        try
        {
            string json = File.ReadAllText(cachedSpotsPath);
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            TourismSpotCacheEnvelope cache = JsonUtility.FromJson<TourismSpotCacheEnvelope>(json);
            if (cache == null || cache.spots == null || cache.spots.Count == 0)
            {
                return;
            }

            cachedTourismSpots.Clear();
            cachedTourismSpots.AddRange(CloneTourismSpots(cache.spots));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to load cached tourism spots: " + exception.Message);
        }
    }

    private void SaveCachedTourismSpotsToDisk()
    {
        if (string.IsNullOrEmpty(cachedSpotsPath))
        {
            return;
        }

        try
        {
            TourismSpotCacheEnvelope cache = new TourismSpotCacheEnvelope
            {
                spots = CloneTourismSpots(cachedTourismSpots),
                savedAt = DateTime.Now.ToString("o"),
                sourceBaseUrl = ApiConfig.BaseUrl
            };

            File.WriteAllText(cachedSpotsPath, JsonUtility.ToJson(cache));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("Failed to save cached tourism spots: " + exception.Message);
        }
    }

    private bool ApplyCachedTourismSpotsToScene()
    {
        if (cachedTourismSpots.Count <= 0)
        {
            Debug.Log("[BackendBootstrap] No cached tourism spots available.");
            return false;
        }

        TourismSpotManager tourismSpotManager = FindObjectOfType<TourismSpotManager>();
        if (tourismSpotManager != null)
        {
            tourismSpotManager.SetTourismSpots(CloneTourismSpots(cachedTourismSpots));
            Debug.Log("[BackendBootstrap] Applied cached tourism spots to scene. count=" + cachedTourismSpots.Count);
        }
        else
        {
            Debug.Log("[BackendBootstrap] Cached tourism spots available but TourismSpotManager was not found in scene.");
        }

        return true;
    }

    private static List<TourismSpot> MapSpots(List<SpotDto> spots)
    {
        List<TourismSpot> mappedSpots = new List<TourismSpot>();

        foreach (SpotDto spot in spots)
        {
            if (spot == null)
            {
                continue;
            }

            mappedSpots.Add(new TourismSpot
            {
                spotId = spot.id,
                spotName = spot.name,
                description = spot.description,
                latitude = spot.latitude,
                longitude = spot.longitude,
                radiusMeters = spot.radiusMeters,
                modelPrefabKey = !string.IsNullOrEmpty(spot.modelPrefabKey)
                    ? spot.modelPrefabKey
                    : (spot.reward != null ? spot.reward.previewPrefabKey : string.Empty),
                rewardName = spot.reward != null ? spot.reward.name : string.Empty,
                rewardDescription = spot.reward != null ? spot.reward.description : string.Empty,
                rewardImageUrl = spot.reward != null ? spot.reward.imageUrl : string.Empty,
                rewardPreviewPrefabKey = spot.reward != null ? spot.reward.previewPrefabKey : string.Empty
            });
        }

        return mappedSpots;
    }

    private void SetConnectionState(
        BackendConnectionState state,
        string title,
        string detail,
        string errorMessage)
    {
        ConnectionState = state;
        ConnectionTitle = title;
        ConnectionDetail = detail;
        LastStatusMessage = title + ": " + detail;
        LastErrorMessage = errorMessage ?? string.Empty;
        GameSession.backendStatusMessage = LastStatusMessage;
        Debug.Log("[BackendBootstrap] ConnectionState => " + state + " | " + title + " | " + detail + (string.IsNullOrEmpty(LastErrorMessage) ? string.Empty : " | error=" + LastErrorMessage));
    }

    private static List<TourismSpot> CloneTourismSpots(List<TourismSpot> spots)
    {
        List<TourismSpot> clonedSpots = new List<TourismSpot>();

        foreach (TourismSpot spot in spots)
        {
            if (spot == null)
            {
                continue;
            }

            clonedSpots.Add(new TourismSpot
            {
                spotId = spot.spotId,
                spotName = spot.spotName,
                description = spot.description,
                latitude = spot.latitude,
                longitude = spot.longitude,
                radiusMeters = spot.radiusMeters,
                modelPrefabKey = spot.modelPrefabKey,
                rewardName = spot.rewardName,
                rewardDescription = spot.rewardDescription,
                rewardImageUrl = spot.rewardImageUrl,
                rewardPreviewPrefabKey = spot.rewardPreviewPrefabKey
            });
        }

        return clonedSpots;
    }
}
