using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CollectedRewardPreviewController : MonoBehaviour
{
    private static Sprite defaultSprite;

    public static CollectedRewardPreviewController Instance { get; private set; }

    private readonly RemoteTextureCache textureCache = new RemoteTextureCache();

    private Camera previewCamera;
    private Transform previewRoot;
    private RawImage previewImage;
    private Image previewPlaceholder;
    private TMP_Text placeholderText;
    private TMP_Text titleText;
    private TMP_Text descriptionText;
    private TMP_Text acquiredDateText;
    private TMP_Text spotNameText;
    private Canvas screenOverlayCanvas;
    private Coroutine attachRoutine;
    private Coroutine imageRoutine;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureExists()
    {
        if (Instance != null)
        {
            return;
        }

        GameObject bootstrapObject = new GameObject("CollectedRewardPreviewController");
        bootstrapObject.AddComponent<CollectedRewardPreviewController>();
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
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("[CollectedRewardPreviewController] Scene loaded => " + scene.name + ", previewMode=" + GameSession.isCollectionPreviewMode);
        if (scene.name == "ARScene")
        {
            RefreshPreviewIfNeeded();
            return;
        }

        CleanupPreview();
    }

    public void RefreshPreviewIfNeeded()
    {
        if (attachRoutine != null)
        {
            StopCoroutine(attachRoutine);
            attachRoutine = null;
        }

        if (SceneManager.GetActiveScene().name != "ARScene")
        {
            CleanupPreview();
            return;
        }

        attachRoutine = StartCoroutine(AttachPreviewIfNeeded());
    }

    private IEnumerator AttachPreviewIfNeeded()
    {
        yield return null;
        attachRoutine = null;

        if (SceneManager.GetActiveScene().name != "ARScene" || !GameSession.isCollectionPreviewMode)
        {
            Debug.Log("[CollectedRewardPreviewController] AttachPreviewIfNeeded skipped. Scene=" + SceneManager.GetActiveScene().name + ", previewMode=" + GameSession.isCollectionPreviewMode);
            CleanupPreview();
            yield break;
        }

        previewCamera = ResolvePreviewCamera();
        if (previewCamera == null)
        {
            Debug.LogWarning("[CollectedRewardPreviewController] No preview camera found.");
            yield break;
        }

        Debug.Log("[CollectedRewardPreviewController] Attaching collection preview UI and disabling chest spawners.");
        DisableChestSpawners();
        BuildPreviewUi();
        UpdatePreviewText();
        StartLoadingImage();
    }

    private void Update()
    {
        if (!GameSession.isCollectionPreviewMode || previewRoot == null)
        {
            return;
        }

        if (previewCamera == null)
        {
            previewCamera = ResolvePreviewCamera();
            if (previewCamera == null)
            {
                return;
            }
        }

        Vector3 targetPosition =
            previewCamera.transform.position +
            previewCamera.transform.forward * 1.35f +
            previewCamera.transform.up * -0.08f;

        previewRoot.position = Vector3.Lerp(previewRoot.position, targetPosition, Time.deltaTime * 8f);

        Vector3 lookDirection = previewCamera.transform.position - previewRoot.position;
        if (lookDirection.sqrMagnitude > 0.0001f)
        {
            previewRoot.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
        }
    }

    private void BuildPreviewUi()
    {
        if (previewRoot != null)
        {
            return;
        }

        GameObject previewObject = new GameObject("CollectionPreviewRoot");
        previewRoot = previewObject.transform;

        Canvas worldCanvas = previewObject.AddComponent<Canvas>();
        worldCanvas.renderMode = RenderMode.WorldSpace;
        worldCanvas.worldCamera = previewCamera;
        worldCanvas.sortingOrder = 20;
        previewObject.AddComponent<GraphicRaycaster>();

        RectTransform canvasRect = worldCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(700f, 980f);
        previewRoot.localScale = Vector3.one * 0.0017f;

        Image backgroundImage = previewObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyPanelChrome(backgroundImage, new Color(0.98f, 0.99f, 0.98f, 0.96f));

        TMP_Text headerText = CreateText(
            "Header",
            canvasRect,
            "Collection Preview",
            46f,
            FontStyles.Bold,
            new Color(0.08f, 0.12f, 0.18f, 1f),
            TextAlignmentOptions.Center);
        headerText.rectTransform.anchorMin = new Vector2(0f, 1f);
        headerText.rectTransform.anchorMax = new Vector2(1f, 1f);
        headerText.rectTransform.pivot = new Vector2(0.5f, 1f);
        headerText.rectTransform.offsetMin = new Vector2(40f, -92f);
        headerText.rectTransform.offsetMax = new Vector2(-40f, -24f);
        RuntimeGameUiTheme.StyleTitleText(headerText, new Color(0.08f, 0.12f, 0.18f, 1f));

        GameObject imageFrame = CreateUiObject("ImageFrame", canvasRect);
        RectTransform imageFrameRect = imageFrame.GetComponent<RectTransform>();
        imageFrameRect.anchorMin = new Vector2(0.5f, 1f);
        imageFrameRect.anchorMax = new Vector2(0.5f, 1f);
        imageFrameRect.pivot = new Vector2(0.5f, 1f);
        imageFrameRect.anchoredPosition = new Vector2(0f, -132f);
        imageFrameRect.sizeDelta = new Vector2(310f, 310f);

        Image imageFrameImage = imageFrame.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyPanelChrome(imageFrameImage, new Color(0.88f, 0.92f, 0.97f, 1f));

        GameObject thumbnailObject = CreateUiObject("Thumbnail", imageFrameRect);
        RectTransform thumbnailRect = thumbnailObject.GetComponent<RectTransform>();
        StretchRect(thumbnailRect, new Vector2(16f, 16f), new Vector2(-16f, -16f));
        previewImage = thumbnailObject.AddComponent<RawImage>();
        previewImage.color = Color.white;
        previewImage.gameObject.SetActive(false);

        GameObject placeholderObject = CreateUiObject("ThumbnailPlaceholder", imageFrameRect);
        RectTransform placeholderRect = placeholderObject.GetComponent<RectTransform>();
        StretchRect(placeholderRect, new Vector2(16f, 16f), new Vector2(-16f, -16f));
        previewPlaceholder = placeholderObject.AddComponent<Image>();
        previewPlaceholder.sprite = GetDefaultSprite();
        previewPlaceholder.color = new Color(0.22f, 0.45f, 0.80f, 0.92f);

        placeholderText = CreateText(
            "PlaceholderText",
            placeholderRect,
            "R",
            72f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(placeholderText.rectTransform, Vector2.zero, Vector2.zero);
        RuntimeGameUiTheme.StyleButtonLabel(placeholderText);

        titleText = CreateText(
            "TitleText",
            canvasRect,
            "Reward",
            42f,
            FontStyles.Bold,
            new Color(0.08f, 0.12f, 0.18f, 1f),
            TextAlignmentOptions.Center);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.offsetMin = new Vector2(40f, -492f);
        titleText.rectTransform.offsetMax = new Vector2(-40f, -430f);
        RuntimeGameUiTheme.StyleTitleText(titleText, new Color(0.08f, 0.12f, 0.18f, 1f));

        descriptionText = CreateText(
            "DescriptionText",
            canvasRect,
            "Description",
            30f,
            FontStyles.Normal,
            new Color(0.25f, 0.30f, 0.36f, 1f),
            TextAlignmentOptions.TopLeft);
        descriptionText.rectTransform.anchorMin = new Vector2(0f, 1f);
        descriptionText.rectTransform.anchorMax = new Vector2(1f, 1f);
        descriptionText.rectTransform.pivot = new Vector2(0f, 1f);
        descriptionText.rectTransform.offsetMin = new Vector2(52f, -720f);
        descriptionText.rectTransform.offsetMax = new Vector2(-52f, -520f);

        acquiredDateText = CreateText(
            "AcquiredDateText",
            canvasRect,
            "Acquired: -",
            28f,
            FontStyles.Bold,
            new Color(0.07f, 0.43f, 0.59f, 1f),
            TextAlignmentOptions.Left);
        acquiredDateText.rectTransform.anchorMin = new Vector2(0f, 0f);
        acquiredDateText.rectTransform.anchorMax = new Vector2(1f, 0f);
        acquiredDateText.rectTransform.pivot = new Vector2(0f, 0f);
        acquiredDateText.rectTransform.offsetMin = new Vector2(52f, 112f);
        acquiredDateText.rectTransform.offsetMax = new Vector2(-52f, 152f);
        RuntimeGameUiTheme.StyleAccentText(acquiredDateText, new Color(0.07f, 0.43f, 0.59f, 1f));

        spotNameText = CreateText(
            "SpotNameText",
            canvasRect,
            "Collected at: -",
            26f,
            FontStyles.Normal,
            new Color(0.18f, 0.24f, 0.31f, 1f),
            TextAlignmentOptions.Left);
        spotNameText.rectTransform.anchorMin = new Vector2(0f, 0f);
        spotNameText.rectTransform.anchorMax = new Vector2(1f, 0f);
        spotNameText.rectTransform.pivot = new Vector2(0f, 0f);
        spotNameText.rectTransform.offsetMin = new Vector2(52f, 66f);
        spotNameText.rectTransform.offsetMax = new Vector2(-52f, 100f);

        BuildScreenOverlay();
    }

    private void UpdatePreviewText()
    {
        if (titleText == null)
        {
            return;
        }

        titleText.text = string.IsNullOrEmpty(GameSession.previewRewardName)
            ? "Collected Reward"
            : GameSession.previewRewardName;

        descriptionText.text = string.IsNullOrEmpty(GameSession.previewRewardDescription)
            ? "No description available yet."
            : GameSession.previewRewardDescription;

        acquiredDateText.text = "Acquired: " + CollectedRewardItem.FormatClaimedAt(GameSession.previewClaimedAtRaw);
        spotNameText.text = string.IsNullOrEmpty(GameSession.previewSpotName)
            ? "Collected at: Unknown spot"
            : "Collected at: " + GameSession.previewSpotName;

        SetPreviewTexture(null, GameSession.previewRewardName);
    }

    private void StartLoadingImage()
    {
        if (imageRoutine != null)
        {
            StopCoroutine(imageRoutine);
            imageRoutine = null;
        }

        if (string.IsNullOrEmpty(GameSession.previewRewardImageUrl))
        {
            Debug.Log("[CollectedRewardPreviewController] No preview image URL. Using placeholder.");
            return;
        }

        Debug.Log("[CollectedRewardPreviewController] Loading preview image => " + GameSession.previewRewardImageUrl);
        imageRoutine = StartCoroutine(LoadPreviewImage(GameSession.previewRewardImageUrl));
    }

    private IEnumerator LoadPreviewImage(string imageUrl)
    {
        Texture2D loadedTexture = null;
        string errorMessage = null;

        yield return textureCache.LoadTexture(
            imageUrl,
            texture => loadedTexture = texture,
            error => errorMessage = error);

        imageRoutine = null;

        if (!GameSession.isCollectionPreviewMode || GameSession.previewRewardImageUrl != imageUrl)
        {
            yield break;
        }

        if (loadedTexture != null)
        {
            SetPreviewTexture(loadedTexture, GameSession.previewRewardName);
            yield break;
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            Debug.LogWarning("AR preview image failed: " + errorMessage);
        }
    }

    private void SetPreviewTexture(Texture texture, string rewardName)
    {
        if (previewImage == null || previewPlaceholder == null)
        {
            return;
        }

        bool hasTexture = texture != null;
        previewImage.texture = texture;
        previewImage.gameObject.SetActive(hasTexture);
        previewPlaceholder.gameObject.SetActive(!hasTexture);
        placeholderText.text = !string.IsNullOrEmpty(rewardName)
            ? rewardName.Substring(0, 1).ToUpperInvariant()
            : "R";
    }

    private void DisableChestSpawners()
    {
        ARChestSpawner[] chestSpawners = FindObjectsOfType<ARChestSpawner>();
        Debug.Log("[CollectedRewardPreviewController] DisableChestSpawners => count=" + chestSpawners.Length);
        for (int i = 0; i < chestSpawners.Length; i++)
        {
            chestSpawners[i].StopSpawning();
            chestSpawners[i].enabled = false;
        }
    }

    private void BuildScreenOverlay()
    {
        if (screenOverlayCanvas != null)
        {
            return;
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        GameObject overlayObject = new GameObject("CollectionPreviewOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        screenOverlayCanvas = overlayObject.GetComponent<Canvas>();
        screenOverlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenOverlayCanvas.sortingOrder = 200;

        CanvasScaler scaler = overlayObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        StretchRect(overlayRect, Vector2.zero, Vector2.zero);

        GameObject badgeObject = CreateUiObject("PreviewBadge", overlayRect);
        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 1f);
        badgeRect.anchorMax = new Vector2(0.5f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.anchoredPosition = new Vector2(0f, -28f);
        badgeRect.sizeDelta = new Vector2(300f, 52f);

        Image badgeImage = badgeObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyButtonChrome(badgeImage, new Color(0.07f, 0.12f, 0.18f, 0.82f));

        TMP_Text badgeText = CreateText(
            "BadgeText",
            badgeRect,
            "Collection Preview",
            24f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(badgeText.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        RuntimeGameUiTheme.StyleButtonLabel(badgeText);

        GameObject hintObject = CreateUiObject("GestureHint", overlayRect);
        RectTransform hintRect = hintObject.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 1f);
        hintRect.anchorMax = new Vector2(0.5f, 1f);
        hintRect.pivot = new Vector2(0.5f, 1f);
        hintRect.anchoredPosition = new Vector2(0f, -90f);
        hintRect.sizeDelta = new Vector2(420f, 42f);

        TMP_Text hintText = CreateText(
            "HintText",
            hintRect,
            "Drag to rotate. Pinch to scale.",
            20f,
            FontStyles.Normal,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(hintText.rectTransform, new Vector2(8f, 6f), new Vector2(-8f, -6f));

        CreateOverlayButton(
            "BackButton",
            overlayRect,
            "Back",
            new Vector2(24f, -28f),
            new Vector2(120f, 48f),
            new Color(0.57f, 0.18f, 0.19f, 0.95f),
            BackToMainScene);
    }

    private void BackToMainScene()
    {
        Debug.Log("[CollectedRewardPreviewController] BackToMainScene pressed.");
        GameSession.ClearCollectionPreviewData();
        SceneManager.LoadScene("MainScene");
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
    }

    private static Button CreateOverlayButton(
        string objectName,
        RectTransform parent,
        string label,
        Vector2 anchoredPosition,
        Vector2 sizeDelta,
        Color color,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = CreateUiObject(objectName, parent);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0f, 1f);
        buttonRect.anchorMax = new Vector2(0f, 1f);
        buttonRect.pivot = new Vector2(0f, 1f);
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
            label,
            22f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(text.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        RuntimeGameUiTheme.StyleButtonLabel(text);
        text.transform.SetAsLastSibling();

        return button;
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

    private void CleanupPreview()
    {
        if (attachRoutine != null)
        {
            StopCoroutine(attachRoutine);
            attachRoutine = null;
        }

        if (imageRoutine != null)
        {
            StopCoroutine(imageRoutine);
            imageRoutine = null;
        }

        if (previewRoot != null)
        {
            Destroy(previewRoot.gameObject);
            previewRoot = null;
        }

        if (screenOverlayCanvas != null)
        {
            Destroy(screenOverlayCanvas.gameObject);
            screenOverlayCanvas = null;
        }
    }

    private void OnDestroy()
    {
        CleanupPreview();
    }

    private static Camera ResolvePreviewCamera()
    {
        if (NomadARRuntimePermissionGate.Instance?.PrimaryArCamera != null)
        {
            return NomadARRuntimePermissionGate.Instance.PrimaryArCamera;
        }

        return Camera.main != null ? Camera.main : FindObjectOfType<Camera>();
    }
}
