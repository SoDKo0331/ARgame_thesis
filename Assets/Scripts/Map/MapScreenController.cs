using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapScreenController : MonoBehaviour
{
    private const float RefreshIntervalSeconds = 0.25f;
    private const float MinInteractiveZoom = 12.5f;
    private const float MaxInteractiveZoom = 18.25f;
    private const float GestureSnapshotCooldownSeconds = 0.18f;
    private const string UserAvatarResourcePath = "low-poly-wizard-traveler/source/obj";
    private const int UserAvatarLayer = 30;
    private const int UserAvatarRenderTextureSize = 512;
    private static readonly Vector2 UserAvatarUiSize = new Vector2(108f, 144f);

    private static Sprite defaultSprite;
    private static Sprite circleSprite;
    private static Texture2D gridTexture;
    private static readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    private readonly List<MapMarkerView> markerViews = new List<MapMarkerView>();
    private readonly MapboxStaticMapService mapService = new MapboxStaticMapService();

    private LocationTracker locationTracker;
    private TourismSpotManager tourismSpotManager;

    private RectTransform rootRect;
    private RectTransform safeAreaRect;
    private RectTransform mapViewportRect;
    private RectTransform markerLayerRect;
    private RectTransform userAvatarRect;
    private RawImage mapImage;
    private RawImage userAvatarImage;
    private Image fallbackBackgroundImage;
    private Image userMarkerImage;
    private Image routeLineImage;
    private Image selectedSpotRangeImage;
    private TMP_Text mapStatusText;
    private TMP_Text nameText;
    private TMP_Text descriptionText;
    private TMP_Text distanceText;
    private TMP_Text arStatusText;
    private TMP_Text navigationHintText;
    private Button navigateButton;
    private Button openArButton;
    private Button recenterButton;

    private Coroutine refreshRoutine;
    private string selectedSpotKey = string.Empty;
    private Texture2D currentSnapshotTexture;
    private RenderTexture userAvatarRenderTexture;
    private GameObject userAvatarRigRoot;
    private Transform userAvatarPivot;
    private Camera userAvatarCamera;
    private Vector2 userMarkerAnchoredPosition;
    private float lastSnapshotRequestTime = -999f;
    private float lastSnapshotZoom = -999f;
    private float lastUserAvatarLoadAttemptTime = -999f;
    private bool isSnapshotLoading;
    private bool hasUserMarkerAnchoredPosition;
    private bool userAvatarReady;
    private string lastMapStatusMessage = string.Empty;
    private Vector2 lastSnapshotCenter;
    private Vector2 manualMapCenter;
    private float manualMapZoom = MapboxMapConfig.DefaultZoom;
    private bool hasManualMapCenter;
    private bool hasManualMapZoom;
    private bool isMouseDraggingMap;
    private Vector2 lastMousePosition;
    private int activePanFingerId = -1;
    private float lastPinchDistance = -1f;
    private Vector2 lastPinchMidpoint;
    private float lastManualGestureTime = -999f;

    public void Initialize(LocationTracker tracker, TourismSpotManager spotManager)
    {
        locationTracker = tracker;
        tourismSpotManager = spotManager;

        BuildUi();

        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
        }

        refreshRoutine = StartCoroutine(RefreshLoop());
        RefreshNow();
    }

    private IEnumerator RefreshLoop()
    {
        while (true)
        {
            RefreshNow();
            yield return new WaitForSeconds(RefreshIntervalSeconds);
        }
    }

    private void RefreshNow()
    {
        if (mapViewportRect == null)
        {
            return;
        }

        TourismSpot selectedSpot = FindSelectedSpot();
        if (selectedSpot == null && tourismSpotManager != null && tourismSpotManager.CurrentNearbySpot != null)
        {
            selectedSpot = tourismSpotManager.CurrentNearbySpot;
            selectedSpotKey = GetSpotKey(selectedSpot);
        }

        Vector2 center = GetMapCenter(selectedSpot);
        float zoom = GetMapZoom(selectedSpot, center);

        UpdateMapSnapshot(center, zoom);
        UpdateUserMarker(center, zoom);
        UpdateMarkers(center, zoom);
        UpdateInfoCard(selectedSpot);
        UpdateMapStatusMessage(selectedSpot);
        UpdateRecenterButtonState();
    }

    private void Update()
    {
        if (HandleMapGestures())
        {
            RefreshNow();
        }
    }

    private void BuildUi()
    {
        RectTransform selfRect = gameObject.GetComponent<RectTransform>();
        if (selfRect == null)
        {
            selfRect = gameObject.AddComponent<RectTransform>();
        }

        rootRect = selfRect;
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;
        rootRect.SetSiblingIndex(0);

        GameObject safeAreaObject = CreateUiObject("SafeArea", rootRect);
        safeAreaRect = safeAreaObject.GetComponent<RectTransform>();
        RuntimeSafeAreaFitter safeAreaFitter = safeAreaObject.AddComponent<RuntimeSafeAreaFitter>();
        safeAreaFitter.Configure(new Vector2(16f, 16f), new Vector2(16f, 16f));

        GameObject mapViewport = CreateUiObject("MapViewport", rootRect);
        mapViewportRect = mapViewport.GetComponent<RectTransform>();
        StretchRect(mapViewportRect, Vector2.zero, Vector2.zero);

        fallbackBackgroundImage = mapViewport.AddComponent<Image>();
        fallbackBackgroundImage.sprite = GetDefaultSprite();
        fallbackBackgroundImage.color = new Color(0.90f, 0.96f, 0.93f, 1f);

        GameObject topGlowObject = CreateUiObject("TopGlow", mapViewportRect);
        topGlowObject.SetActive(false); // Hide clutter for clean map

        GameObject bottomGlowObject = CreateUiObject("BottomGlow", mapViewportRect);
        bottomGlowObject.SetActive(false); // Hide clutter for clean map

        GameObject gridOverlayObject = CreateUiObject("GridOverlay", mapViewportRect);
        gridOverlayObject.SetActive(false); // Hide clutter for clean map

        GameObject mapImageObject = CreateUiObject("MapImage", mapViewportRect);
        RectTransform mapImageRect = mapImageObject.GetComponent<RectTransform>();
        StretchRect(mapImageRect, Vector2.zero, Vector2.zero);

        mapImage = mapImageObject.AddComponent<RawImage>();
        mapImage.color = new Color(1f, 1f, 1f, 0f);

        GameObject markerLayer = CreateUiObject("MarkerLayer", mapViewportRect);
        markerLayerRect = markerLayer.GetComponent<RectTransform>();
        StretchRect(markerLayerRect, Vector2.zero, Vector2.zero);

        GameObject selectedSpotRange = CreateUiObject("SelectedSpotRange", markerLayerRect);
        RectTransform selectedSpotRangeRect = selectedSpotRange.GetComponent<RectTransform>();
        selectedSpotRangeRect.anchorMin = new Vector2(0.5f, 0.5f);
        selectedSpotRangeRect.anchorMax = new Vector2(0.5f, 0.5f);
        selectedSpotRangeRect.pivot = new Vector2(0.5f, 0.5f);
        selectedSpotRangeImage = selectedSpotRange.AddComponent<Image>();
        selectedSpotRangeImage.sprite = GetCircleSprite();
        selectedSpotRangeImage.color = new Color(0.32f, 0.86f, 0.58f, 0.25f);
        selectedSpotRangeImage.raycastTarget = false;
        selectedSpotRangeImage.gameObject.SetActive(false);

        GameObject routeLine = CreateUiObject("RouteLine", markerLayerRect);
        RectTransform routeLineRect = routeLine.GetComponent<RectTransform>();
        routeLineRect.anchorMin = new Vector2(0.5f, 0.5f);
        routeLineRect.anchorMax = new Vector2(0.5f, 0.5f);
        routeLineRect.pivot = new Vector2(0.5f, 0.5f);
        routeLineImage = routeLine.AddComponent<Image>();
        routeLineImage.sprite = GetDefaultSprite();
        routeLineImage.color = new Color(0.18f, 0.68f, 0.90f, 0.8f);
        routeLineImage.raycastTarget = false;
        routeLineImage.gameObject.SetActive(false);

        GameObject userMarker = CreateUiObject("UserMarker", markerLayerRect);
        RectTransform userMarkerRect = userMarker.GetComponent<RectTransform>();
        userMarkerRect.anchorMin = new Vector2(0.5f, 0.5f);
        userMarkerRect.anchorMax = new Vector2(0.5f, 0.5f);
        userMarkerRect.pivot = new Vector2(0.5f, 0.5f);
        userMarkerRect.sizeDelta = new Vector2(36f, 36f);

        userMarkerImage = userMarker.AddComponent<Image>();
        userMarkerImage.sprite = GetCircleSprite();
        userMarkerImage.color = new Color(0.9f, 0.2f, 0.35f, 1f); // Bright player dot
        userMarkerImage.raycastTarget = false;

        GameObject userAvatar = CreateUiObject("UserAvatarMarker", markerLayerRect);
        userAvatarRect = userAvatar.GetComponent<RectTransform>();
        userAvatarRect.anchorMin = new Vector2(0.5f, 0.5f);
        userAvatarRect.anchorMax = new Vector2(0.5f, 0.5f);
        userAvatarRect.pivot = new Vector2(0.5f, 0.08f);
        userAvatarRect.sizeDelta = UserAvatarUiSize;

        userAvatarImage = userAvatar.AddComponent<RawImage>();
        userAvatarImage.color = Color.white;
        userAvatarImage.raycastTarget = false;
        userAvatarImage.gameObject.SetActive(false);

        EnsureUserAvatarPreview();

        GameObject mapStatus = CreatePanel(
            "MapStatus",
            safeAreaRect,
            new Vector2(0f, -16f),
            new Vector2(360f, 44f), // Widened pill for text
            new Color(1f, 1f, 1f, 0.90f),
            TextAnchor.UpperCenter);
        RectTransform mapStatusRect = mapStatus.GetComponent<RectTransform>();
        mapStatusRect.anchorMin = new Vector2(0.5f, 1f);
        mapStatusRect.anchorMax = new Vector2(0.5f, 1f);
        mapStatusRect.pivot = new Vector2(0.5f, 1f);
        mapStatusRect.anchoredPosition = new Vector2(0f, -16f);
        ApplyShadow(mapStatus.GetComponent<Image>(), new Color(0f, 0f, 0f, 0.15f), new Vector2(0f, -4f));
        RuntimeGameUiTheme.ApplyPanelChrome(mapStatus.GetComponent<Image>(), new Color(1f, 1f, 1f, 0.95f));

        mapStatusText = CreateText(
            "MapStatusText",
            mapStatus.transform as RectTransform,
            "Loading map...",
            18,
            FontStyles.Bold,
            new Color(0.2f, 0.25f, 0.3f, 1f),
            TextAlignmentOptions.Center);
        mapStatusText.enableWordWrapping = false;
        mapStatusText.overflowMode = TextOverflowModes.Ellipsis;
        StretchRect(mapStatusText.rectTransform, new Vector2(16f, 4f), new Vector2(-16f, -4f));
        RuntimeGameUiTheme.StyleAccentText(mapStatusText, new Color(0.2f, 0.25f, 0.3f, 1f));

        GameObject infoCard = CreatePanel(
            "SpotInfoCard",
            safeAreaRect,
            new Vector2(0f, 0f),
            new Vector2(0f, 220f),
            Color.white,
            TextAnchor.LowerLeft);

        RectTransform infoCardRect = infoCard.GetComponent<RectTransform>();
        infoCardRect.anchorMin = new Vector2(0f, 0f);
        infoCardRect.anchorMax = new Vector2(1f, 0f);
        infoCardRect.offsetMin = new Vector2(12f, 12f);
        infoCardRect.offsetMax = new Vector2(-12f, 232f);
        ApplyShadow(infoCard.GetComponent<Image>(), new Color(0f, 0f, 0f, 0.20f), new Vector2(0f, -8f));
        RuntimeGameUiTheme.ApplyPanelChrome(infoCard.GetComponent<Image>(), Color.white);

        GameObject handleObject = CreateUiObject("SheetHandle", infoCardRect);
        RectTransform handleRect = handleObject.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 1f);
        handleRect.anchorMax = new Vector2(0.5f, 1f);
        handleRect.pivot = new Vector2(0.5f, 1f);
        handleRect.anchoredPosition = new Vector2(0f, -12f);
        handleRect.sizeDelta = new Vector2(72f, 8f);
        Image handleImage = handleObject.AddComponent<Image>();
        handleImage.sprite = GetDefaultSprite();
        handleImage.color = new Color(0.74f, 0.80f, 0.78f, 1f);
        ApplyShadow(handleImage, new Color(1f, 1f, 1f, 0.28f), new Vector2(0f, 1f));

        nameText = CreateText(
            "NameText",
            infoCardRect,
            "Tap a marker",
            28,
            FontStyles.Bold,
            new Color(0.08f, 0.12f, 0.17f, 1f),
            TextAlignmentOptions.Left);
        nameText.rectTransform.anchorMin = new Vector2(0f, 1f);
        nameText.rectTransform.anchorMax = new Vector2(1f, 1f);
        nameText.rectTransform.pivot = new Vector2(0f, 1f);
        nameText.rectTransform.offsetMin = new Vector2(18f, -58f);
        nameText.rectTransform.offsetMax = new Vector2(-18f, -18f);
        RuntimeGameUiTheme.StyleTitleText(nameText, new Color(0.10f, 0.14f, 0.19f, 1f));

        descriptionText = CreateText(
            "DescriptionText",
            infoCardRect,
            "Explore the city and tap a tourism spot to inspect it.",
            19,
            FontStyles.Normal,
            new Color(0.23f, 0.28f, 0.34f, 1f),
            TextAlignmentOptions.TopLeft);
        descriptionText.rectTransform.anchorMin = new Vector2(0f, 1f);
        descriptionText.rectTransform.anchorMax = new Vector2(1f, 1f);
        descriptionText.rectTransform.pivot = new Vector2(0f, 1f);
        descriptionText.rectTransform.offsetMin = new Vector2(18f, -128f);
        descriptionText.rectTransform.offsetMax = new Vector2(-18f, -62f);

        RectTransform distanceChipRect = CreateChip(
            "DistanceChip",
            infoCardRect,
            new Vector2(18f, 96f),
            new Vector2(188f, 34f),
            new Color(0.89f, 0.95f, 0.99f, 1f));

        distanceText = CreateText(
            "DistanceText",
            distanceChipRect,
            "Distance: -",
            16,
            FontStyles.Bold,
            new Color(0.08f, 0.35f, 0.56f, 1f),
            TextAlignmentOptions.Center);
        StretchRect(distanceText.rectTransform, new Vector2(10f, 4f), new Vector2(-10f, -4f));
        RuntimeGameUiTheme.StyleAccentText(distanceText, new Color(0.10f, 0.39f, 0.62f, 1f));

        RectTransform arChipRect = CreateChip(
            "ArChip",
            infoCardRect,
            new Vector2(18f, 54f),
            new Vector2(266f, 34f),
            new Color(0.89f, 0.98f, 0.93f, 1f));

        arStatusText = CreateText(
            "ArStatusText",
            arChipRect,
            "AR status: waiting",
            16,
            FontStyles.Bold,
            new Color(0.16f, 0.43f, 0.31f, 1f),
            TextAlignmentOptions.Center);
        StretchRect(arStatusText.rectTransform, new Vector2(10f, 4f), new Vector2(-10f, -4f));
        RuntimeGameUiTheme.StyleAccentText(arStatusText, new Color(0.16f, 0.44f, 0.32f, 1f));

        navigationHintText = CreateText(
            "NavigationHintText",
            infoCardRect,
            "Route: select a spot to see its range.",
            15,
            FontStyles.Normal,
            new Color(0.24f, 0.31f, 0.39f, 1f),
            TextAlignmentOptions.Left);
        navigationHintText.rectTransform.anchorMin = new Vector2(0f, 0f);
        navigationHintText.rectTransform.anchorMax = new Vector2(0.52f, 0f);
        navigationHintText.rectTransform.pivot = new Vector2(0f, 0f);
        navigationHintText.rectTransform.offsetMin = new Vector2(18f, 16f);
        navigationHintText.rectTransform.offsetMax = new Vector2(-8f, 42f);

        navigateButton = CreateButton(
            "NavigateButton",
            infoCardRect,
            "Navigate",
            new Vector2(-176f, 20f),
            new Vector2(152f, 52f),
            new Color(0.18f, 0.68f, 0.90f, 1f), // Bright Blue
            OnNavigatePressed);

        openArButton = CreateButton(
            "OpenArButton",
            infoCardRect,
            "Open AR",
            new Vector2(-16f, 20f),
            new Vector2(152f, 52f),
            new Color(0.32f, 0.86f, 0.58f, 1f), // Poke Green
            OnOpenArPressed);

        recenterButton = CreateButton(
            "RecenterButton",
            safeAreaRect,
            "◉", // Simple icon instead of Location text
            new Vector2(-24f, 260f),
            new Vector2(56f, 56f), // Keep as circular FAB
            Color.white,
            OnRecenterPressed);
        // Style Recenter as a floating pill above the card
        TMP_Text recenterText = recenterButton.GetComponentInChildren<TMP_Text>();
        if (recenterText != null)
        {
            recenterText.color = new Color(0.2f, 0.55f, 0.9f, 1f);
            recenterText.fontSize = 32;
        }
        RectTransform recenterRect = recenterButton.GetComponent<RectTransform>();
        recenterRect.anchorMin = new Vector2(1f, 0f);
        recenterRect.anchorMax = new Vector2(1f, 0f);
        recenterRect.pivot = new Vector2(1f, 0f);

        // Keep the full-screen map behind the safe-area overlays so the
        // status strip and bottom info card stay visible and tappable.
        mapViewportRect.SetAsFirstSibling();
        safeAreaRect.SetAsLastSibling();

        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
    }

    private void UpdateMapSnapshot(Vector2 center, float zoom)
    {
        if (!MapboxMapConfig.HasAccessToken)
        {
            ApplySnapshotTexture(null);
            return;
        }

        if (isSnapshotLoading || Time.unscaledTime - lastManualGestureTime < GestureSnapshotCooldownSeconds)
        {
            return;
        }

        float distanceSinceLastSnapshot = lastSnapshotRequestTime < 0f
            ? float.MaxValue
            : GeoCoordinateUtils.HaversineMeters(lastSnapshotCenter.y, lastSnapshotCenter.x, center.y, center.x);

        bool shouldRequestSnapshot =
            currentSnapshotTexture == null ||
            Time.unscaledTime - lastSnapshotRequestTime >= MapboxMapConfig.SnapshotRefreshSeconds ||
            distanceSinceLastSnapshot >= MapboxMapConfig.SnapshotMoveRefreshMeters ||
            Mathf.Abs(lastSnapshotZoom - zoom) >= 0.18f;

        if (!shouldRequestSnapshot)
        {
            return;
        }

        Vector2Int snapshotSize = GetSnapshotRequestSize(mapViewportRect.rect.size);

        StartCoroutine(LoadSnapshot(center.y, center.x, snapshotSize.x, snapshotSize.y, zoom));
    }

    private IEnumerator LoadSnapshot(double centerLatitude, double centerLongitude, int width, int height, float zoom)
    {
        isSnapshotLoading = true;

        Texture2D loadedTexture = null;
        string errorMessage = null;

        yield return StartCoroutine(mapService.LoadSnapshot(
            centerLatitude,
            centerLongitude,
            width,
            height,
            zoom,
            texture => loadedTexture = texture,
            error => errorMessage = error));

        isSnapshotLoading = false;

        if (loadedTexture != null)
        {
            if (currentSnapshotTexture != null && currentSnapshotTexture != loadedTexture)
            {
                Destroy(currentSnapshotTexture);
            }

            currentSnapshotTexture = loadedTexture;
            ApplySnapshotTexture(currentSnapshotTexture);
            lastSnapshotRequestTime = Time.unscaledTime;
            lastSnapshotCenter = new Vector2((float)centerLongitude, (float)centerLatitude);
            lastSnapshotZoom = zoom;
            lastMapStatusMessage = string.Empty;
            yield break;
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            ApplySnapshotTexture(null);
            lastMapStatusMessage = errorMessage;
            Debug.LogWarning("Map snapshot failed: " + errorMessage);
        }
    }

    private void ApplySnapshotTexture(Texture texture)
    {
        if (mapImage == null)
        {
            return;
        }

        mapImage.texture = texture;
        mapImage.color = texture != null
            ? Color.white
            : new Color(1f, 1f, 1f, 0f);
    }

    private void UpdateMarkers(Vector2 center, float zoom)
    {
        List<TourismSpot> spots = tourismSpotManager != null ? tourismSpotManager.tourismSpots : null;
        int spotCount = spots != null ? spots.Count : 0;

        EnsureMarkerCount(spotCount);

        if (spots == null || mapViewportRect.rect.width <= 0f || mapViewportRect.rect.height <= 0f)
        {
            for (int i = 0; i < markerViews.Count; i++)
            {
                markerViews[i].gameObject.SetActive(false);
            }

            HideSelectedSpotOverlay();
            return;
        }

        TourismSpot selectedSpot = FindSelectedSpot();
        Vector2 viewportSize = mapViewportRect.rect.size;
        bool hasSelectedSpotOverlay = false;
        Vector2 selectedSpotPosition = Vector2.zero;
        float selectedSpotRadiusPixels = 0f;

        for (int i = 0; i < markerViews.Count; i++)
        {
            MapMarkerView markerView = markerViews[i];

            if (i >= spots.Count || spots[i] == null)
            {
                markerView.gameObject.SetActive(false);
                continue;
            }

            TourismSpot spot = spots[i];
            float distance = GetDistanceToSpot(spot);
            bool isNearby = distance >= 0f && distance <= MapboxMapConfig.NearbyHighlightMeters;
            bool canOpenAr = distance >= 0f && distance <= spot.OpenArRadiusMeters;
            bool isSelected = IsSameSpot(spot, selectedSpot);

            markerView.Bind(spot, HandleMarkerClicked);

            if (GeoCoordinateUtils.TryGetAnchoredPosition(
                spot.latitude,
                spot.longitude,
                center.y,
                center.x,
                zoom,
                viewportSize,
                24f,
                out Vector2 anchoredPosition))
            {
                markerView.gameObject.SetActive(true);
                markerView.SetAnchoredPosition(anchoredPosition);
                markerView.SetVisualState(isNearby, isSelected, canOpenAr);

                if (isSelected)
                {
                    markerView.transform.SetAsLastSibling();
                    hasSelectedSpotOverlay = true;
                    selectedSpotPosition = anchoredPosition;
                    selectedSpotRadiusPixels = Mathf.Clamp(
                        GeoCoordinateUtils.MetersToPixels(spot.latitude, zoom, spot.OpenArRadiusMeters),
                        64f,
                        viewportSize.x * 0.8f);
                }
            }
            else
            {
                markerView.gameObject.SetActive(false);
            }
        }

        if (hasSelectedSpotOverlay)
        {
            ShowSelectedSpotOverlay(selectedSpotPosition, selectedSpotRadiusPixels);
        }
        else
        {
            HideSelectedSpotOverlay();
        }
    }

    private void UpdateUserMarker(Vector2 center, float zoom)
    {
        if (!userAvatarReady && Time.unscaledTime - lastUserAvatarLoadAttemptTime >= 1.5f)
        {
            EnsureUserAvatarPreview();
        }

        hasUserMarkerAnchoredPosition = false;

        if (locationTracker == null || !locationTracker.IsLocationReady || mapViewportRect == null)
        {
            SetUserMarkerVisualState(false, Vector2.zero);
            return;
        }

        if (!GeoCoordinateUtils.TryGetAnchoredPosition(
                locationTracker.Latitude,
                locationTracker.Longitude,
                center.y,
                center.x,
                zoom,
                mapViewportRect.rect.size,
                24f,
                out Vector2 anchoredPosition))
        {
            SetUserMarkerVisualState(false, Vector2.zero);
            return;
        }

        hasUserMarkerAnchoredPosition = true;
        userMarkerAnchoredPosition = anchoredPosition;
        SetUserMarkerVisualState(true, anchoredPosition);
    }

    private void SetUserMarkerVisualState(bool isVisible, Vector2 anchoredPosition)
    {
        bool showAvatar = isVisible && userAvatarReady && userAvatarImage != null;

        if (userMarkerImage != null)
        {
            userMarkerImage.gameObject.SetActive(isVisible && !showAvatar);
            if (isVisible)
            {
                userMarkerImage.rectTransform.anchoredPosition = anchoredPosition;
            }
        }

        if (userAvatarImage != null)
        {
            userAvatarImage.gameObject.SetActive(showAvatar);
            if (showAvatar)
            {
                userAvatarRect.anchoredPosition = anchoredPosition;
                userAvatarRect.SetAsLastSibling();
            }
        }

        if (userAvatarCamera != null)
        {
            userAvatarCamera.enabled = showAvatar;
        }
    }

    private void EnsureUserAvatarPreview()
    {
        lastUserAvatarLoadAttemptTime = Time.unscaledTime;

        if (userAvatarReady || userAvatarImage == null)
        {
            return;
        }

        GameObject avatarPrefab = Resources.Load<GameObject>(UserAvatarResourcePath);
        if (avatarPrefab == null)
        {
            return;
        }

        CleanupUserAvatarPreview();

        userAvatarRenderTexture = new RenderTexture(
            UserAvatarRenderTextureSize,
            UserAvatarRenderTextureSize,
            24,
            RenderTextureFormat.ARGB32);
        userAvatarRenderTexture.name = "MapUserAvatarRenderTexture";
        userAvatarRenderTexture.antiAliasing = 4;
        userAvatarRenderTexture.Create();

        userAvatarRigRoot = new GameObject("MapUserAvatarPreviewRig");

        userAvatarPivot = new GameObject("AvatarPivot").transform;
        userAvatarPivot.SetParent(userAvatarRigRoot.transform, false);

        GameObject avatarInstance = Instantiate(avatarPrefab, userAvatarPivot);
        DisableAuxiliaryAvatarComponents(avatarInstance);
        SetLayerRecursively(avatarInstance, UserAvatarLayer);

        if (!TryFrameAvatar(avatarInstance, out Vector3 cameraTarget, out float cameraDistance))
        {
            CleanupUserAvatarPreview();
            return;
        }

        MapUserAvatarMotion motion = userAvatarPivot.gameObject.AddComponent<MapUserAvatarMotion>();
        motion.Configure(Vector3.zero, Quaternion.Euler(0f, 180f, 0f));

        CreateAvatarPreviewLight(
            "AvatarKeyLight",
            userAvatarRigRoot.transform,
            new Vector3(34f, 148f, 0f),
            1.2f,
            new Color(1f, 0.97f, 0.9f, 1f));
        CreateAvatarPreviewLight(
            "AvatarFillLight",
            userAvatarRigRoot.transform,
            new Vector3(328f, 212f, 0f),
            0.45f,
            new Color(0.74f, 0.86f, 1f, 1f));

        GameObject cameraObject = new GameObject("AvatarCamera", typeof(Camera));
        cameraObject.transform.SetParent(userAvatarRigRoot.transform, false);
        userAvatarCamera = cameraObject.GetComponent<Camera>();
        userAvatarCamera.enabled = false;
        userAvatarCamera.cullingMask = 1 << UserAvatarLayer;
        userAvatarCamera.clearFlags = CameraClearFlags.SolidColor;
        userAvatarCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        userAvatarCamera.fieldOfView = 25f;
        userAvatarCamera.nearClipPlane = 0.03f;
        userAvatarCamera.farClipPlane = 20f;
        userAvatarCamera.allowHDR = false;
        userAvatarCamera.allowMSAA = true;
        userAvatarCamera.useOcclusionCulling = false;
        userAvatarCamera.targetTexture = userAvatarRenderTexture;

        Transform cameraTransform = userAvatarCamera.transform;
        cameraTransform.localPosition = cameraTarget + new Vector3(0f, 0.06f, -cameraDistance);
        cameraTransform.LookAt(cameraTarget);

        userAvatarRigRoot.transform.position = new Vector3(4000f, -4000f, 4000f);
        userAvatarImage.texture = userAvatarRenderTexture;
        userAvatarReady = true;
    }

    private bool TryFrameAvatar(GameObject avatarInstance, out Vector3 cameraTarget, out float cameraDistance)
    {
        cameraTarget = new Vector3(0f, 1f, 0f);
        cameraDistance = 4f;

        if (avatarInstance == null)
        {
            return false;
        }

        if (!TryGetAvatarBounds(avatarInstance, out Bounds bounds))
        {
            return false;
        }

        float initialHeight = Mathf.Max(bounds.size.y, 0.1f);
        float scaleMultiplier = 1.8f / initialHeight;
        avatarInstance.transform.localScale = avatarInstance.transform.localScale * scaleMultiplier;

        if (!TryGetAvatarBounds(avatarInstance, out bounds))
        {
            return false;
        }

        avatarInstance.transform.localPosition = new Vector3(
            -bounds.center.x,
            -bounds.min.y,
            -bounds.center.z);

        if (!TryGetAvatarBounds(avatarInstance, out bounds))
        {
            return false;
        }

        float avatarHeight = Mathf.Max(bounds.size.y, 1f);
        float avatarWidth = Mathf.Max(bounds.size.x, bounds.size.z, 0.7f);
        float fieldOfViewRadians = 25f * 0.5f * Mathf.Deg2Rad;
        float framingRadius = Mathf.Max(avatarWidth * 0.72f, avatarHeight * 0.62f);

        cameraTarget = new Vector3(0f, bounds.min.y + avatarHeight * 0.57f, 0f);
        cameraDistance = framingRadius / Mathf.Tan(fieldOfViewRadians) + avatarWidth * 0.45f;
        cameraDistance = Mathf.Clamp(cameraDistance, 2.4f, 6.5f);
        return true;
    }

    private static bool TryGetAvatarBounds(GameObject avatarRoot, out Bounds bounds)
    {
        Renderer[] renderers = avatarRoot != null
            ? avatarRoot.GetComponentsInChildren<Renderer>(true)
            : null;

        if (renderers == null || renderers.Length == 0)
        {
            bounds = new Bounds(Vector3.zero, Vector3.zero);
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return true;
    }

    private static void DisableAuxiliaryAvatarComponents(GameObject avatarRoot)
    {
        if (avatarRoot == null)
        {
            return;
        }

        Camera[] cameras = avatarRoot.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].enabled = false;
        }

        Light[] lights = avatarRoot.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            lights[i].enabled = false;
        }
    }

    private static void SetLayerRecursively(GameObject root, int layer)
    {
        if (root == null)
        {
            return;
        }

        Transform[] children = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < children.Length; i++)
        {
            children[i].gameObject.layer = layer;
        }
    }

    private static void CreateAvatarPreviewLight(
        string objectName,
        Transform parent,
        Vector3 eulerAngles,
        float intensity,
        Color color)
    {
        GameObject lightObject = new GameObject(objectName, typeof(Light));
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localRotation = Quaternion.Euler(eulerAngles);

        Light light = lightObject.GetComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = intensity;
        light.color = color;
        light.shadows = LightShadows.None;
        light.cullingMask = 1 << UserAvatarLayer;
    }

    private void CleanupUserAvatarPreview()
    {
        userAvatarReady = false;

        if (userAvatarCamera != null)
        {
            userAvatarCamera.targetTexture = null;
        }

        if (userAvatarImage != null)
        {
            userAvatarImage.texture = null;
            userAvatarImage.gameObject.SetActive(false);
        }

        if (userAvatarRigRoot != null)
        {
            Destroy(userAvatarRigRoot);
            userAvatarRigRoot = null;
        }

        if (userAvatarRenderTexture != null)
        {
            userAvatarRenderTexture.Release();
            Destroy(userAvatarRenderTexture);
            userAvatarRenderTexture = null;
        }

        userAvatarPivot = null;
        userAvatarCamera = null;
    }

    private void UpdateInfoCard(TourismSpot selectedSpot)
    {
        if (selectedSpot == null)
        {
            nameText.text = "Tap a marker";
            descriptionText.text = "Explore the map and inspect a tourism spot to see its details.";
            distanceText.text = "Distance: -";
            arStatusText.text = "AR status: select a spot";
            navigationHintText.text = "Route: select a spot to see its range.";
            navigateButton.interactable = false;
            openArButton.interactable = false;
            return;
        }

        float distance = GetDistanceToSpot(selectedSpot);
        bool canOpenAr = distance >= 0f && distance <= selectedSpot.OpenArRadiusMeters;

        nameText.text = string.IsNullOrEmpty(selectedSpot.spotName) ? "Tourism Spot" : selectedSpot.spotName;
        descriptionText.text = string.IsNullOrEmpty(selectedSpot.description)
            ? "No description yet."
            : selectedSpot.description;

        distanceText.text = distance >= 0f
            ? $"Distance: {distance:F1} m"
            : "Distance: waiting for GPS";

        arStatusText.text = canOpenAr
            ? "AR status: ready to open"
            : "AR status: move closer to unlock";
        navigationHintText.text = canOpenAr
            ? "Route: you are inside the treasure range."
            : "Route: follow the blue line, or use Navigate for turn-by-turn directions.";

        navigateButton.interactable = true;
        openArButton.interactable = canOpenAr;
    }

    private void UpdateMapStatusMessage(TourismSpot selectedSpot)
    {
        if (mapStatusText == null)
        {
            return;
        }

        string message;

        if (!MapboxMapConfig.HasAccessToken)
        {
            message = selectedSpot != null
                ? "Preview map mode. Drag to move, pinch to zoom."
                : string.Empty;
            mapStatusText.transform.parent.gameObject.SetActive(!string.IsNullOrEmpty(message));
            mapStatusText.text = message;
            return;
        }

        if (!string.IsNullOrEmpty(lastMapStatusMessage))
        {
            message = lastMapStatusMessage;
            mapStatusText.transform.parent.gameObject.SetActive(true);
            mapStatusText.text = message;
            return;
        }

        if (locationTracker == null || !locationTracker.IsLocationReady)
        {
            message = "Waiting for GPS to center the map...";
            mapStatusText.transform.parent.gameObject.SetActive(true);
            mapStatusText.text = message;
            return;
        }

        if (selectedSpot != null)
        {
            float distance = GetDistanceToSpot(selectedSpot);
            if (distance >= 0f && distance <= MapboxMapConfig.NearbyHighlightMeters)
            {
                message = hasManualMapCenter || hasManualMapZoom
                    ? "Manual map mode. Tap Recenter to follow GPS again."
                    : "Nearby spots glow green. Drag to move, pinch to zoom.";
                mapStatusText.transform.parent.gameObject.SetActive(true);
                mapStatusText.text = message;
                return;
            }
        }

        message = hasManualMapCenter || hasManualMapZoom
            ? "Manual map mode. Tap Recenter to follow GPS again."
            : "Drag to move, pinch to zoom, tap a marker to inspect.";
        mapStatusText.transform.parent.gameObject.SetActive(true);
        mapStatusText.text = message;
    }

    private void EnsureMarkerCount(int requiredCount)
    {
        while (markerViews.Count < requiredCount)
        {
            GameObject markerObject = CreateUiObject("SpotMarker", markerLayerRect);
            MapMarkerView markerView = markerObject.AddComponent<MapMarkerView>();
            markerView.Initialize();
            markerViews.Add(markerView);
        }
    }

    private void HandleMarkerClicked(TourismSpot spot)
    {
        Debug.Log("[MapScreenController] Marker clicked => " + (spot != null ? spot.spotName : "null"));
        selectedSpotKey = GetSpotKey(spot);
        ResetManualMapView();

        if (spot != null)
        {
            float distance = GetDistanceToSpot(spot);
            if (distance >= 0f && distance <= spot.OpenArRadiusMeters)
            {
                Debug.Log("[MapScreenController] Marker tap opening AR directly. spot=" + spot.spotName + ", distance=" + distance + ", radius=" + spot.OpenArRadiusMeters);
                OpenArForSpot(spot, distance);
                return;
            }
        }

        RefreshNow();
    }

    private void OnOpenArPressed()
    {
        TourismSpot selectedSpot = FindSelectedSpot();
        if (selectedSpot == null)
        {
            Debug.Log("[MapScreenController] Open AR ignored because no spot is selected.");
            return;
        }

        float distance = GetDistanceToSpot(selectedSpot);
        if (distance < 0f || distance > selectedSpot.OpenArRadiusMeters)
        {
            Debug.Log("[MapScreenController] Open AR blocked. spot=" + selectedSpot.spotName + ", distance=" + distance + ", radius=" + selectedSpot.OpenArRadiusMeters);
            return;
        }

        OpenArForSpot(selectedSpot, distance);
    }

    private void OnNavigatePressed()
    {
        TourismSpot selectedSpot = FindSelectedSpot();
        if (selectedSpot == null)
        {
            Debug.Log("[MapScreenController] Navigate ignored because no spot is selected.");
            return;
        }

        Debug.Log("[MapScreenController] Navigate pressed for " + selectedSpot.spotName);
        MapNavigationService.OpenDirections(selectedSpot, locationTracker);
    }

    private void OnRecenterPressed()
    {
        Debug.Log("[MapScreenController] Recenter pressed.");
        ResetManualMapView();
        RefreshNow();
    }

    private TourismSpot FindSelectedSpot()
    {
        if (tourismSpotManager == null || tourismSpotManager.tourismSpots == null)
        {
            return null;
        }

        if (string.IsNullOrEmpty(selectedSpotKey))
        {
            return null;
        }

        for (int i = 0; i < tourismSpotManager.tourismSpots.Count; i++)
        {
            TourismSpot spot = tourismSpotManager.tourismSpots[i];
            if (spot != null && GetSpotKey(spot) == selectedSpotKey)
            {
                return spot;
            }
        }

        return null;
    }

    private static void OpenArForSpot(TourismSpot spot, float distance)
    {
        Debug.Log("[MapScreenController] Open AR accepted. spot=" + spot.spotName + ", distance=" + distance + ", radius=" + spot.OpenArRadiusMeters);

        GameSession.SetSpotData(
            spot.spotId,
            spot.spotName,
            spot.description,
            spot.rewardName,
            spot.rewardDescription,
            spot.rewardImageUrl,
            spot.rewardPreviewPrefabKey,
            spot.latitude,
            spot.longitude,
            spot.radiusMeters,
            spot.modelPrefabKey);

        Debug.Log("[MapScreenController] Loading ARScene from map.");
        SceneManager.LoadScene("ARScene");
    }

    private Vector2 GetMapCenter(TourismSpot selectedSpot)
    {
        if (hasManualMapCenter)
        {
            return manualMapCenter;
        }

        if (locationTracker != null && locationTracker.IsLocationReady)
        {
            if (selectedSpot != null)
            {
                float distanceToSpot = GetDistanceToSpot(selectedSpot);
                if (distanceToSpot > 8f)
                {
                    float blend = Mathf.Clamp01(distanceToSpot / 180f) * 0.5f;
                    float longitude = Mathf.Lerp(
                        (float)locationTracker.Longitude,
                        (float)selectedSpot.longitude,
                        blend);
                    float latitude = Mathf.Lerp(
                        (float)locationTracker.Latitude,
                        (float)selectedSpot.latitude,
                        blend);
                    return new Vector2(longitude, latitude);
                }
            }

            return new Vector2((float)locationTracker.Longitude, (float)locationTracker.Latitude);
        }

        if (selectedSpot != null)
        {
            return new Vector2((float)selectedSpot.longitude, (float)selectedSpot.latitude);
        }

        if (tourismSpotManager != null && tourismSpotManager.NearestSpot != null)
        {
            return new Vector2((float)tourismSpotManager.NearestSpot.longitude, (float)tourismSpotManager.NearestSpot.latitude);
        }

        return new Vector2(106.9177f, 47.9184f);
    }

    private float GetMapZoom(TourismSpot selectedSpot, Vector2 currentCenter)
    {
        if (hasManualMapZoom)
        {
            return manualMapZoom;
        }

        if (selectedSpot == null || locationTracker == null || !locationTracker.IsLocationReady || mapViewportRect == null)
        {
            return MapboxMapConfig.DefaultZoom;
        }

        float distance = GetDistanceToSpot(selectedSpot);
        if (distance <= 0.001f)
        {
            return MapboxMapConfig.DefaultZoom;
        }

        Vector2 viewportSize = mapViewportRect.rect.size;
        if (viewportSize.x <= 0f || viewportSize.y <= 0f)
        {
            return MapboxMapConfig.DefaultZoom;
        }

        float visibleMeters = Mathf.Max(distance * 2.15f, selectedSpot.OpenArRadiusMeters * 3.2f, 120f);
        float targetPixels = Mathf.Min(viewportSize.x, viewportSize.y) * 0.42f;
        float calculatedZoom = GeoCoordinateUtils.CalculateZoomForVisibleMeters(currentCenter.y, visibleMeters, targetPixels);

        if (calculatedZoom <= 0f)
        {
            return MapboxMapConfig.DefaultZoom;
        }

        return Mathf.Clamp(calculatedZoom, 13.1f, MapboxMapConfig.DefaultZoom);
    }

    private bool HandleMapGestures()
    {
        bool changed = false;

#if UNITY_EDITOR || UNITY_STANDALONE
        changed |= HandleMouseGestures();
#endif

        changed |= HandleTouchGestures();
        return changed;
    }

    private bool HandleMouseGestures()
    {
        bool changed = false;
        Vector2 mousePosition = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            isMouseDraggingMap = IsMapGestureAllowed(mousePosition);
            lastMousePosition = mousePosition;
        }

        if (Input.GetMouseButton(0) && isMouseDraggingMap)
        {
            Vector2 delta = mousePosition - lastMousePosition;
            if (delta.sqrMagnitude > 0.0001f)
            {
                changed |= ApplyPanDelta(delta);
                lastMousePosition = mousePosition;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            isMouseDraggingMap = false;
        }

        float scroll = Input.mouseScrollDelta.y;
        if (Mathf.Abs(scroll) > 0.001f && IsMapGestureAllowed(mousePosition))
        {
            changed |= ApplyZoomDelta(scroll * 0.35f);
        }

        return changed;
    }

    private bool HandleTouchGestures()
    {
        if (Input.touchCount <= 0)
        {
            activePanFingerId = -1;
            lastPinchDistance = -1f;
            return false;
        }

        if (Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);

            if (touch.phase == TouchPhase.Began)
            {
                if (IsMapGestureAllowed(touch.position))
                {
                    activePanFingerId = touch.fingerId;
                }
            }

            if (touch.fingerId == activePanFingerId &&
                (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary))
            {
                lastPinchDistance = -1f;
                return ApplyPanDelta(touch.deltaPosition);
            }

            if (touch.fingerId == activePanFingerId &&
                (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled))
            {
                activePanFingerId = -1;
            }

            lastPinchDistance = -1f;
            return false;
        }

        activePanFingerId = -1;

        Touch firstTouch = Input.GetTouch(0);
        Touch secondTouch = Input.GetTouch(1);
        if (!IsMapGestureAllowed(firstTouch.position) || !IsMapGestureAllowed(secondTouch.position))
        {
            lastPinchDistance = -1f;
            return false;
        }

        Vector2 firstPosition = firstTouch.position;
        Vector2 secondPosition = secondTouch.position;
        Vector2 midpoint = (firstPosition + secondPosition) * 0.5f;
        float currentDistance = Vector2.Distance(firstPosition, secondPosition);

        bool changed = false;
        if (lastPinchDistance > 0.001f)
        {
            float pinchRatio = currentDistance / lastPinchDistance;
            float zoomDelta = Mathf.Log(Mathf.Max(0.01f, pinchRatio), 2f) * 1.2f;
            if (Mathf.Abs(zoomDelta) > 0.001f)
            {
                changed |= ApplyZoomDelta(zoomDelta);
            }

            Vector2 midpointDelta = midpoint - lastPinchMidpoint;
            if (midpointDelta.sqrMagnitude > 0.0001f)
            {
                changed |= ApplyPanDelta(midpointDelta);
            }
        }

        lastPinchDistance = currentDistance;
        lastPinchMidpoint = midpoint;
        return changed;
    }

    private bool ApplyPanDelta(Vector2 screenDelta)
    {
        if (mapViewportRect == null || screenDelta.sqrMagnitude <= 0.0001f)
        {
            return false;
        }

        Vector2 currentCenter = GetMapCenter(FindSelectedSpot());
        float currentZoom = GetMapZoom(FindSelectedSpot(), currentCenter);
        Vector2 nextCenter = GeoCoordinateUtils.PanCenterByScreenDelta(
            currentCenter.y,
            currentCenter.x,
            currentZoom,
            screenDelta);

        manualMapCenter = nextCenter;
        hasManualMapCenter = true;
        lastManualGestureTime = Time.unscaledTime;

        if (!hasManualMapZoom)
        {
            manualMapZoom = currentZoom;
            hasManualMapZoom = true;
        }

        return true;
    }

    private bool ApplyZoomDelta(float zoomDelta)
    {
        if (Mathf.Abs(zoomDelta) <= 0.0001f)
        {
            return false;
        }

        Vector2 currentCenter = GetMapCenter(FindSelectedSpot());
        float currentZoom = GetMapZoom(FindSelectedSpot(), currentCenter);
        float nextZoom = Mathf.Clamp(currentZoom + zoomDelta, MinInteractiveZoom, MaxInteractiveZoom);

        if (Mathf.Abs(nextZoom - currentZoom) <= 0.0001f)
        {
            return false;
        }

        manualMapCenter = currentCenter;
        hasManualMapCenter = true;
        manualMapZoom = nextZoom;
        hasManualMapZoom = true;
        lastManualGestureTime = Time.unscaledTime;
        return true;
    }

    private void ResetManualMapView()
    {
        hasManualMapCenter = false;
        hasManualMapZoom = false;
        manualMapZoom = MapboxMapConfig.DefaultZoom;
        lastManualGestureTime = -999f;
    }

    private void UpdateRecenterButtonState()
    {
        if (recenterButton == null)
        {
            return;
        }

        recenterButton.gameObject.SetActive(hasManualMapCenter || hasManualMapZoom);
    }

    private bool IsMapGestureAllowed(Vector2 screenPosition)
    {
        if (mapViewportRect == null)
        {
            return false;
        }

        if (!RectTransformUtility.RectangleContainsScreenPoint(mapViewportRect, screenPosition, null))
        {
            return false;
        }

        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            return true;
        }

        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = screenPosition
        };

        uiRaycastResults.Clear();
        eventSystem.RaycastAll(pointerData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            GameObject hitObject = uiRaycastResults[i].gameObject;
            if (hitObject == null)
            {
                continue;
            }

            Transform hitTransform = hitObject.transform;
            return hitTransform.IsChildOf(mapViewportRect);
        }

        return true;
    }

    private static Vector2Int GetSnapshotRequestSize(Vector2 viewportSize)
    {
        float width = Mathf.Max(1f, viewportSize.x);
        float height = Mathf.Max(1f, viewportSize.y);
        float scale = Mathf.Min(
            MapboxMapConfig.SnapshotMaxSize / width,
            MapboxMapConfig.SnapshotMaxSize / height);

        if (scale > 1f)
        {
            scale = 1f;
        }

        int requestWidth = Mathf.Clamp(
            Mathf.RoundToInt(width * scale),
            MapboxMapConfig.SnapshotMinSize,
            MapboxMapConfig.SnapshotMaxSize);
        int requestHeight = Mathf.Clamp(
            Mathf.RoundToInt(height * scale),
            MapboxMapConfig.SnapshotMinSize,
            MapboxMapConfig.SnapshotMaxSize);

        return new Vector2Int(requestWidth, requestHeight);
    }

    private float GetDistanceToSpot(TourismSpot spot)
    {
        if (spot == null || locationTracker == null || !locationTracker.IsLocationReady)
        {
            return -1f;
        }

        return GeoCoordinateUtils.HaversineMeters(
            locationTracker.Latitude,
            locationTracker.Longitude,
            spot.latitude,
            spot.longitude);
    }

    private void ShowSelectedSpotOverlay(Vector2 selectedSpotPosition, float radiusPixels)
    {
        if (selectedSpotRangeImage != null)
        {
            RectTransform rangeRect = selectedSpotRangeImage.rectTransform;
            rangeRect.anchoredPosition = selectedSpotPosition;
            rangeRect.sizeDelta = new Vector2(radiusPixels * 2f, radiusPixels * 2f);
            selectedSpotRangeImage.gameObject.SetActive(true);
            selectedSpotRangeImage.transform.SetAsFirstSibling();
        }

        if (routeLineImage == null)
        {
            return;
        }

        bool canShowRoute = locationTracker != null && locationTracker.IsLocationReady && hasUserMarkerAnchoredPosition;
        routeLineImage.gameObject.SetActive(canShowRoute);

        if (!canShowRoute)
        {
            return;
        }

        RectTransform lineRect = routeLineImage.rectTransform;
        Vector2 from = userMarkerAnchoredPosition;
        Vector2 to = selectedSpotPosition;
        Vector2 direction = to - from;
        float length = direction.magnitude;

        if (length <= 0.001f)
        {
            routeLineImage.gameObject.SetActive(false);
            return;
        }

        lineRect.anchoredPosition = (from + to) * 0.5f;
        lineRect.sizeDelta = new Vector2(8f, length);
        lineRect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f);
        routeLineImage.transform.SetAsFirstSibling();
    }

    private void HideSelectedSpotOverlay()
    {
        if (selectedSpotRangeImage != null)
        {
            selectedSpotRangeImage.gameObject.SetActive(false);
        }

        if (routeLineImage != null)
        {
            routeLineImage.gameObject.SetActive(false);
        }
    }

    private static string GetSpotKey(TourismSpot spot)
    {
        if (spot == null)
        {
            return string.Empty;
        }

        return string.IsNullOrEmpty(spot.spotId) ? spot.spotName : spot.spotId;
    }

    private static bool IsSameSpot(TourismSpot left, TourismSpot right)
    {
        return !string.IsNullOrEmpty(GetSpotKey(left)) && GetSpotKey(left) == GetSpotKey(right);
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static GameObject CreatePanel(
        string objectName,
        RectTransform parent,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color,
        TextAnchor anchor)
    {
        GameObject panelObject = CreateUiObject(objectName, parent);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.sizeDelta = sizeDelta;

        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                panelRect.anchorMin = new Vector2(0f, 1f);
                panelRect.anchorMax = new Vector2(0f, 1f);
                panelRect.pivot = new Vector2(0f, 1f);
                panelRect.anchoredPosition = anchoredPosition;
                break;
            default:
                panelRect.anchorMin = new Vector2(0f, 0f);
                panelRect.anchorMax = new Vector2(0f, 0f);
                panelRect.pivot = new Vector2(0f, 0f);
                panelRect.anchoredPosition = anchoredPosition;
                break;
        }

        Image panelImage = panelObject.AddComponent<Image>();
        panelImage.sprite = GetDefaultSprite();
        panelImage.color = color;

        return panelObject;
    }

    private static RectTransform CreateChip(
        string objectName,
        RectTransform parent,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color)
    {
        GameObject chipObject = CreateUiObject(objectName, parent);
        RectTransform chipRect = chipObject.GetComponent<RectTransform>();
        chipRect.anchorMin = new Vector2(0f, 0f);
        chipRect.anchorMax = new Vector2(0f, 0f);
        chipRect.pivot = new Vector2(0f, 0f);
        chipRect.anchoredPosition = anchoredPosition;
        chipRect.sizeDelta = sizeDelta;

        Image chipImage = chipObject.AddComponent<Image>();
        chipImage.sprite = GetDefaultSprite();
        chipImage.color = color;
        ApplyOutline(chipImage, new Color(1f, 1f, 1f, 0.5f), new Vector2(1f, -1f));

        return chipRect;
    }

    private static Button CreateButton(
        string objectName,
        RectTransform parent,
        string buttonText,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = sizeDelta;

        Image buttonImage = buttonObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyButtonChrome(buttonImage, color);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);

        TMP_Text text = CreateText(
            "Label",
            buttonRect,
            buttonText,
            22,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(text.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        RuntimeGameUiTheme.StyleButtonLabel(text);
        text.transform.SetAsLastSibling();

        return button;
    }

    private static void ApplyShadow(Graphic graphic, Color color, Vector2 effectDistance)
    {
        if (graphic == null)
        {
            return;
        }

        Shadow shadow = graphic.gameObject.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = graphic.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = color;
        shadow.effectDistance = effectDistance;
        shadow.useGraphicAlpha = true;
    }

    private static void ApplyOutline(Graphic graphic, Color color, Vector2 effectDistance)
    {
        if (graphic == null)
        {
            return;
        }

        Outline outline = graphic.gameObject.GetComponent<Outline>();
        if (outline == null)
        {
            outline = graphic.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = color;
        outline.effectDistance = effectDistance;
        outline.useGraphicAlpha = true;
    }

    private static TMP_Text CreateText(
        string objectName,
        RectTransform parent,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject textObject = CreateUiObject(objectName, parent);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = fontStyle;
        text.color = color;
        text.alignment = alignment;

        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
        }

        return text;
    }

    private static void StretchRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
    {
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = offsetMin;
        rectTransform.offsetMax = offsetMax;
    }

    private static Sprite GetDefaultSprite()
    {
        if (defaultSprite == null)
        {
            defaultSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f));
        }

        return defaultSprite;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite == null)
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.name = "MapRangeCircle";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.45f;
            float edgeWidth = size * 0.05f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float fillAlpha = distance <= radius ? 0.35f : 0f;
                    float ringAlpha = Mathf.Abs(distance - radius) <= edgeWidth ? 1f : 0f;
                    float alpha = Mathf.Max(fillAlpha, ringAlpha);
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            circleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
        }

        return circleSprite;
    }

    private static Texture2D GetGridTexture()
    {
        if (gridTexture == null)
        {
            const int size = 64;
            gridTexture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            gridTexture.name = "MapGridTexture";
            gridTexture.filterMode = FilterMode.Bilinear;
            gridTexture.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool majorLine = x == 0 || y == 0;
                    bool minorLine = x % 16 == 0 || y % 16 == 0;
                    float alpha = majorLine ? 0.22f : minorLine ? 0.08f : 0f;
                    gridTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            gridTexture.Apply();
        }

        return gridTexture;
    }

    private void OnDestroy()
    {
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
        }

        if (currentSnapshotTexture != null)
        {
            Destroy(currentSnapshotTexture);
        }

        CleanupUserAvatarPreview();
    }
}
