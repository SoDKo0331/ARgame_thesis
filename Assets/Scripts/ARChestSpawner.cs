using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using TMPro;

public class ARChestSpawner : MonoBehaviour
{
    private const float EarthRadiusMeters = 6378137f;
    private const float MinimumUpwardSurfaceDot = 0.75f;
    private const float StableFallbackDistance = 1.5f;
    private const float StableFallbackHeightOffset = -0.2f;
    private const float MinimumVisibleSpawnDistance = 0.75f;

    private static readonly Vector2[] PlaneSampleViewportPoints =
    {
        new Vector2(0.5f, 0.34f),
        new Vector2(0.5f, 0.48f),
        new Vector2(0.35f, 0.34f),
        new Vector2(0.65f, 0.34f),
        new Vector2(0.5f, 0.22f)
    };

    [Header("References")]
    public GameObject chestPrefab;
    public ARRaycastManager raycastManager;
    public Camera arCamera;
    public GameObject rewardPanel;

    [Header("Spawn Settings")]
    public float fallbackDistance = 1.25f;
    public float fallbackHeightOffset = -0.08f;
    public float surfaceOffset = 0.03f;
    public float planeSearchDelay = 0.35f;
    public float planeSearchDuration = 1.8f;
    public float planeRetryInterval = 0.2f;
    public float chestLocalScale = 0.12f;

    [Header("Runtime UI")]
    public bool showStatusOverlay = true;

    [Header("Debug")]
    public bool verboseLogging = true;

    private bool hasSpawned;
    private Coroutine spawnRoutine;
    private Coroutine hideStatusRoutine;
    private float lastComputedSpotDistanceMeters = -1f;
    private Canvas overlayCanvas;
    private RectTransform overlaySafeAreaRect;
    private RectTransform statusPanelRect;
    private TextMeshProUGUI statusText;
    private NomadARController sceneController;
    private static readonly List<ARRaycastHit> hits = new List<ARRaycastHit>();

    private void Awake()
    {
        Input.compass.enabled = true;
        LogInfo("Awake. Collection preview mode = " + GameSession.isCollectionPreviewMode);
        sceneController = GetComponent<NomadARController>();
        if (sceneController == null)
        {
            NomadARController[] controllers = FindObjectsOfType<NomadARController>(true);
            if (controllers.Length > 0)
            {
                sceneController = controllers[0];
            }
        }

        LogInfo("Scene controller found = " + (sceneController != null));
    }

    private void Start()
    {
        if (GameSession.isCollectionPreviewMode)
        {
            LogInfo("Start aborted because collection preview mode is active.");
            enabled = false;
            return;
        }

        if (!HasSelectedSpotPayload())
        {
            LogInfo("Start waiting for React Native spot payload before spawning.");
            enabled = false;
            return;
        }

        BuildStatusOverlay();
        HidePlaneVisuals();
        SetStatus("Locating exact spot model...");
        LogInfo("AR scene ready. Starting exact spot spawn flow.");
        TrySpawnChest();
    }

    public void TrySpawnChest()
    {
        if (hasSpawned || spawnRoutine != null)
        {
            LogInfo("TrySpawnChest ignored because chest already spawned or routine is running.");
            return;
        }

        if (GameSession.isCollectionPreviewMode)
        {
            LogInfo("TrySpawnChest ignored because collection preview mode is active.");
            return;
        }

        if (raycastManager == null || arCamera == null)
        {
            SetStatus("AR setup missing references.");
            Debug.LogWarning("ARChestSpawner: Missing reference.");
            return;
        }

        LogInfo(
            "TrySpawnChest started. Camera = " + arCamera.name +
            ", selectedSpot = " + GameSession.selectedSpotName +
            ", fallbackDistance = " + fallbackDistance.ToString("F2"));
        spawnRoutine = StartCoroutine(SpawnChestWhenReady());
    }

    public void StopSpawning()
    {
        if (spawnRoutine != null)
        {
            StopCoroutine(spawnRoutine);
            spawnRoutine = null;
        }

        if (hideStatusRoutine != null)
        {
            StopCoroutine(hideStatusRoutine);
            hideStatusRoutine = null;
        }

        if (overlayCanvas != null)
        {
            overlayCanvas.gameObject.SetActive(false);
        }
    }

    private IEnumerator SpawnChestWhenReady()
    {
        yield return new WaitForSeconds(planeSearchDelay);

        LogInfo("Plane search started after delay = " + planeSearchDelay.ToString("F2") + "s");
        SetStatus("Move phone slowly to place the exact spot...");

        float deadline = Time.time + planeSearchDuration;
        while (!hasSpawned && Time.time < deadline)
        {
            HidePlaneVisuals();

            if (TrySpawnOnDetectedPlane())
            {
                ShowSpawnReadyStatus();
                spawnRoutine = null;
                yield break;
            }

            yield return new WaitForSeconds(planeRetryInterval);
        }

        if (!hasSpawned)
        {
            LogInfo("No suitable plane found. Switching to exact GPS fallback.");
            SetStatus("Using exact GPS fallback placement...");
            SpawnFallbackChest();
            ShowSpawnReadyStatus();
        }

        HidePlaneVisuals();
        spawnRoutine = null;
    }

    private bool TrySpawnOnDetectedPlane()
    {
        for (int index = 0; index < PlaneSampleViewportPoints.Length; index++)
        {
            Vector2 viewportPoint = PlaneSampleViewportPoints[index];
            Vector2 screenPoint = new Vector2(Screen.width * viewportPoint.x, Screen.height * viewportPoint.y);
            if (!raycastManager.Raycast(screenPoint, hits, TrackableType.PlaneWithinPolygon))
            {
                continue;
            }

            Pose hitPose = hits[0].pose;
            if (Vector3.Dot(hitPose.up, Vector3.up) < MinimumUpwardSurfaceDot)
            {
                LogInfo("Plane hit rejected because it is not horizontal enough. Up dot = " +
                    Vector3.Dot(hitPose.up, Vector3.up).ToString("F2"));
                continue;
            }

            Vector3 forward = GetFlatForward(arCamera.transform.forward);
            Quaternion rotation = forward.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(forward, Vector3.up)
                : hitPose.rotation;

            GameObject spawnedReward = new GameObject("RewardCollectibleSpot");
            spawnedReward.transform.position = GetGeoAnchoredSpawnPosition(hitPose.position.y);
            spawnedReward.transform.rotation = rotation;

            PrepareSpawnedReward(spawnedReward);

            hasSpawned = true;
            LogInfo("Reward spawned on detected plane. Position = " + spawnedReward.transform.position);
            return true;
        }

        return false;
    }

    private void SpawnFallbackChest()
    {
        Vector3 fallbackPosition = GetGeoAnchoredSpawnPosition(GetFallbackGroundY());
        Vector3 flatForward = GetFlatForward(arCamera.transform.forward);
        Quaternion rotation = Quaternion.LookRotation(flatForward, Vector3.up);

        GameObject spawnedReward = new GameObject("RewardCollectibleSpot");
        spawnedReward.transform.position = fallbackPosition;
        spawnedReward.transform.rotation = rotation;

        PrepareSpawnedReward(spawnedReward);

        hasSpawned = true;
        LogInfo("Reward spawned in front of camera (fallback). Position = " + spawnedReward.transform.position);
    }

    private void PrepareSpawnedReward(GameObject spawnedObject)
    {
        ARChestRewardCollectible collectible = spawnedObject.AddComponent<ARChestRewardCollectible>();
        collectible.Initialize(
            GetCollectibleDisplayName(),
            GetCollectibleModelPrefabKey(),
            arCamera,
            HandleRewardCollected);
            
        LogInfo("Prepared advanced reward collectible.");
    }
    
    private void HandleRewardCollected()
    {
        LogInfo("Reward collectible tapped. Showing reward panel and starting claim.");

        StartBackendClaimIfNeeded();

        if (rewardPanel != null)
        {
            rewardPanel.SetActive(true);
            RewardPanelController panel = rewardPanel.GetComponent<RewardPanelController>();
            if (panel != null) panel.Refresh();
        }
    }

    private void StartBackendClaimIfNeeded()
    {
        if (GameSession.rewardClaimRequested || BackendBootstrap.Instance == null) return;

        if (!BackendBootstrap.Instance.HasBootstrappedSession ||
            string.IsNullOrEmpty(GameSession.selectedSpotId) ||
            string.IsNullOrEmpty(GameSession.userId)) return;

        GameSession.rewardClaimRequested = true;
        GameSession.backendStatusMessage = "Collection-д нэмж байна...";
        LogInfo("Starting backend claim for spotId = " + GameSession.selectedSpotId);

        BackendBootstrap.Instance.StartClaimSelectedSpotRequest(
            response =>
            {
                if (response == null) return;
                
                // NATIVE BRIDGE COMMUNICATION (To React Native)
                if (NativeBridgeManager.Instance != null && !response.alreadyClaimed)
                {
                    NativeBridgeManager.Instance.SendSuccessToRN(GameSession.selectedSpotId);
                }

                if (rewardPanel != null && rewardPanel.activeInHierarchy)
                {
                    RewardPanelController panel = rewardPanel.GetComponent<RewardPanelController>();
                    if (panel != null)
                    {
                        panel.Refresh();
                        panel.ShowClaimStatus(response.alreadyClaimed ? "Энэ шагнал аль хэдийн collection-д байна." : "Collection-д нэмэгдлээ!");
                    }
                }
            },
            error =>
            {
                GameSession.rewardClaimRequested = false;
                if (rewardPanel != null && rewardPanel.activeInHierarchy)
                {
                    RewardPanelController panel = rewardPanel.GetComponent<RewardPanelController>();
                    if (panel != null) panel.ShowClaimStatus("Интернетгүй тул локал reward мэдээлэл харуулж байна.");
                }
                if (error != null) Debug.LogWarning("[ARChestSpawner] Claim failed: " + error.message, this);
            });
    }

    private static Vector3 GetFlatForward(Vector3 sourceForward)
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(sourceForward, Vector3.up);
        return flatForward.sqrMagnitude > 0.001f ? flatForward.normalized : Vector3.forward;
    }

    private Vector3 GetGeoAnchoredSpawnPosition(float groundY)
    {
        if (TryBuildWorldOffsetForSelectedSpot(out Vector3 worldOffset, out float distanceMeters))
        {
            Vector3 spawnPosition = new Vector3(arCamera.transform.position.x, groundY, arCamera.transform.position.z) + worldOffset;

            if (distanceMeters < MinimumVisibleSpawnDistance)
            {
                spawnPosition += GetFlatForward(arCamera.transform.forward) * MinimumVisibleSpawnDistance;
                lastComputedSpotDistanceMeters = MinimumVisibleSpawnDistance;
            }
            else
            {
                lastComputedSpotDistanceMeters = distanceMeters;
            }

            spawnPosition.y = groundY + surfaceOffset;
            return spawnPosition;
        }

        lastComputedSpotDistanceMeters = -1f;
        Vector3 fallbackPosition = GetFallbackChestPosition();
        fallbackPosition.y = groundY + surfaceOffset;
        return fallbackPosition;
    }

    private Vector3 GetFallbackChestPosition()
    {
        Vector3 flatForward = GetFlatForward(arCamera.transform.forward);
        Vector3 fallbackPosition = arCamera.transform.position + flatForward * StableFallbackDistance;
        fallbackPosition += Vector3.up * StableFallbackHeightOffset;
        return fallbackPosition;
    }

    private float GetFallbackGroundY()
    {
        return arCamera.transform.position.y + StableFallbackHeightOffset;
    }

    private bool TryBuildWorldOffsetForSelectedSpot(out Vector3 worldOffset, out float distanceMeters)
    {
        worldOffset = Vector3.zero;
        distanceMeters = 0f;

        if (GameSession.selectedSpotLatitude == 0d && GameSession.selectedSpotLongitude == 0d)
        {
            LogInfo("Exact spot spawn skipped because selected spot coordinates are unavailable.");
            return false;
        }

        if (!GameSession.hasCurrentLocation)
        {
            LogInfo("Exact spot spawn skipped because current location is unavailable.");
            return false;
        }

        Vector2 eastNorthOffsetMeters = CalculateEastNorthOffsetMeters(
            GameSession.currentLatitude,
            GameSession.currentLongitude,
            GameSession.selectedSpotLatitude,
            GameSession.selectedSpotLongitude);
        distanceMeters = eastNorthOffsetMeters.magnitude;

        Vector3 flatForward = GetFlatForward(arCamera.transform.forward);
        Vector3 flatRight = Vector3.ProjectOnPlane(arCamera.transform.right, Vector3.up).normalized;
        if (flatRight.sqrMagnitude < 0.001f)
        {
            flatRight = Vector3.Cross(Vector3.up, flatForward).normalized;
        }

        float headingDegrees = ResolveHeadingDegrees();
        if (float.IsNaN(headingDegrees) || float.IsInfinity(headingDegrees))
        {
            LogInfo("Exact spot spawn skipped because heading is unavailable.");
            return false;
        }

        float headingRadians = headingDegrees * Mathf.Deg2Rad;
        Vector3 worldNorth = flatForward * Mathf.Cos(headingRadians) - flatRight * Mathf.Sin(headingRadians);
        Vector3 worldEast = flatForward * Mathf.Sin(headingRadians) + flatRight * Mathf.Cos(headingRadians);
        worldOffset = (worldEast * eastNorthOffsetMeters.x) + (worldNorth * eastNorthOffsetMeters.y);
        return true;
    }

    private static Vector2 CalculateEastNorthOffsetMeters(
        double originLatitude,
        double originLongitude,
        double targetLatitude,
        double targetLongitude)
    {
        double averageLatitudeRadians = ((originLatitude + targetLatitude) * 0.5d) * Mathf.Deg2Rad;
        double eastMeters =
            (targetLongitude - originLongitude) *
            Mathf.Deg2Rad *
            EarthRadiusMeters *
            System.Math.Cos(averageLatitudeRadians);
        double northMeters =
            (targetLatitude - originLatitude) *
            Mathf.Deg2Rad *
            EarthRadiusMeters;

        return new Vector2((float)eastMeters, (float)northMeters);
    }

    private static float ResolveHeadingDegrees()
    {
        if (Input.compass.enabled && Input.compass.timestamp > 0d)
        {
            float trueHeading = Input.compass.trueHeading;
            if (trueHeading >= 0f && !float.IsNaN(trueHeading) && !float.IsInfinity(trueHeading))
            {
                return trueHeading;
            }

            float magneticHeading = Input.compass.magneticHeading;
            if (magneticHeading >= 0f && !float.IsNaN(magneticHeading) && !float.IsInfinity(magneticHeading))
            {
                return magneticHeading;
            }
        }

        return GameSession.hasCurrentHeading ? GameSession.currentHeadingDegrees : float.NaN;
    }

    private static string GetCollectibleDisplayName()
    {
        return string.IsNullOrEmpty(GameSession.selectedSpotName)
            ? GameSession.rewardName
            : GameSession.selectedSpotName;
    }

    private static string GetCollectibleModelPrefabKey()
    {
        if (!string.IsNullOrEmpty(GameSession.selectedSpotModelPrefabKey))
        {
            return GameSession.selectedSpotModelPrefabKey;
        }

        return GameSession.rewardPreviewPrefabKey;
    }

    private static bool HasSelectedSpotPayload()
    {
        return !string.IsNullOrEmpty(GameSession.selectedSpotId) ||
            !string.IsNullOrEmpty(GameSession.selectedSpotName) ||
            !string.IsNullOrEmpty(GameSession.rewardName);
    }

    private static void HidePlaneVisuals()
    {
        ARPlane[] planes = FindObjectsOfType<ARPlane>(true);
        for (int planeIndex = 0; planeIndex < planes.Length; planeIndex++)
        {
            Renderer[] renderers = planes[planeIndex].GetComponentsInChildren<Renderer>(true);
            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                renderers[rendererIndex].enabled = false;
            }

            LineRenderer[] lineRenderers = planes[planeIndex].GetComponentsInChildren<LineRenderer>(true);
            for (int lineIndex = 0; lineIndex < lineRenderers.Length; lineIndex++)
            {
                lineRenderers[lineIndex].enabled = false;
            }
        }
    }

    private void BuildStatusOverlay()
    {
        if (!showStatusOverlay || overlayCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject(
            "ARChestStatusOverlay",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        overlayCanvas = canvasObject.GetComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 250;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        GameObject safeAreaObject = CreateUiObject("SafeArea", canvasRect);
        overlaySafeAreaRect = safeAreaObject.GetComponent<RectTransform>();
        RuntimeSafeAreaFitter safeAreaFitter = safeAreaObject.AddComponent<RuntimeSafeAreaFitter>();
        safeAreaFitter.Configure(new Vector2(24f, 18f), new Vector2(24f, 18f));

        CreateButton(
            "BackButton",
            overlaySafeAreaRect,
            "BACK",
            new Vector2(98f, -42f),
            new Vector2(164f, 62f),
            new Color(0.14f, 0.20f, 0.28f, 0.85f), // sleek dark
            OnBackPressed);

        CreateButton(
            "CloseButton",
            overlaySafeAreaRect,
            "CLOSE",
            new Vector2(-98f, -42f),
            new Vector2(164f, 62f),
            new Color(0.14f, 0.20f, 0.28f, 0.85f), // sleek dark
            OnClosePressed,
            anchorPreset: new Vector2(1f, 1f),
            pivot: new Vector2(1f, 1f));

        GameObject panelObject = CreateUiObject("StatusPanel", overlaySafeAreaRect);
        Image panelImage = panelObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyPanelChrome(panelImage, new Color(0.05f, 0.08f, 0.12f, 0.88f));

        statusPanelRect = panelObject.GetComponent<RectTransform>();
        statusPanelRect.anchorMin = new Vector2(0.5f, 1f);
        statusPanelRect.anchorMax = new Vector2(0.5f, 1f);
        statusPanelRect.pivot = new Vector2(0.5f, 1f);
        statusPanelRect.anchoredPosition = new Vector2(0f, -42f);
        statusPanelRect.sizeDelta = new Vector2(500f, 66f);
        panelImage.raycastTarget = false;
        
        var outline = panelObject.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0.18f, 0.68f, 0.75f, 0.4f);
        outline.effectDistance = new Vector2(0f, -1.5f);

        GameObject textObject = new GameObject("StatusText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(statusPanelRect, false);
        statusText = textObject.GetComponent<TextMeshProUGUI>();
        statusText.alignment = TextAlignmentOptions.Center;
        statusText.fontSize = 24f;
        statusText.fontStyle = FontStyles.Bold;
        statusText.color = new Color(0.92f, 0.98f, 1f, 1f);
        statusText.enableWordWrapping = true;
        
        var shadow = textObject.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.5f);
        shadow.effectDistance = new Vector2(0f, -2f);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 8f);
        textRect.offsetMax = new Vector2(-16f, -8f);
    }

    private void SetStatus(string message)
    {
        if (statusText == null)
        {
            return;
        }

        if (hideStatusRoutine != null)
        {
            StopCoroutine(hideStatusRoutine);
            hideStatusRoutine = null;
        }

        statusText.text = message;
        if (overlayCanvas != null)
        {
            overlayCanvas.gameObject.SetActive(true);
        }

        if (statusPanelRect != null)
        {
            statusPanelRect.gameObject.SetActive(true);
        }
    }

    private void ShowSpawnReadyStatus()
    {
        if (lastComputedSpotDistanceMeters >= 1f)
        {
            SetStatus("Exact spot model placed. Walk to it and tap to collect.");
            return;
        }

        SetStatus("Exact spot model ready. Tap it to collect.");
    }

    private void OnBackPressed()
    {
        LogInfo("Back button pressed. Notifying React Native.");
        if (sceneController != null)
        {
            sceneController.BackToMain();
            return;
        }

        GameSession.ClearCollectionPreviewData();
        NativeBridgeManager.Instance?.SendStatusToRN("close_requested");
    }

    private void OnClosePressed()
    {
        LogInfo("Close button pressed.");
        if (rewardPanel != null && rewardPanel.activeInHierarchy)
        {
            rewardPanel.SetActive(false);
            return;
        }

        if (statusPanelRect != null)
        {
            statusPanelRect.gameObject.SetActive(false);
        }
    }

    private static GameObject CreateUiObject(string objectName, RectTransform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Button CreateButton(
        string objectName,
        RectTransform parent,
        string buttonText,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color,
        UnityEngine.Events.UnityAction onClick,
        Vector2? anchorPreset = null,
        Vector2? pivot = null)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        Vector2 anchor = anchorPreset ?? new Vector2(0f, 1f);
        buttonRect.anchorMin = anchor;
        buttonRect.anchorMax = anchor;
        buttonRect.pivot = pivot ?? anchor;
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = sizeDelta;

        Image buttonImage = buttonObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyButtonChrome(buttonImage, color);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);

        GameObject labelObject = CreateUiObject("Label", buttonRect);
        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = buttonText;
        label.fontSize = 20f;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        RuntimeGameUiTheme.StyleButtonLabel(label);

        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 8f);
        labelRect.offsetMax = new Vector2(-8f, -8f);

        return button;
    }

    private void LogInfo(string message)
    {
        if (!verboseLogging)
        {
            return;
        }

        Debug.Log("[ARChestSpawner] " + message, this);
    }

    private void OnDisable()
    {
        StopSpawning();
    }
}
