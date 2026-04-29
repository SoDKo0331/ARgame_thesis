using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CollectionScreenController : MonoBehaviour
{
    private enum CollectionViewState
    {
        Hidden,
        Loading,
        Content,
        Empty,
        Retry
    }

    private static Sprite defaultSprite;

    private readonly List<CollectedRewardItem> cachedRewards = new List<CollectedRewardItem>();
    private readonly List<CollectionRewardCardView> cardViews = new List<CollectionRewardCardView>();
    private readonly RemoteTextureCache textureCache = new RemoteTextureCache();

    private RewardApiService rewardApiService;

    private RectTransform rootRect;
    private RectTransform safeAreaRect;
    private Button openCollectionButton;
    private GameObject overlayObject;
    private TMP_Text headerBadgeText;
    private TMP_Text statusText;
    private TMP_Text emptyStateText;
    private GameObject statePanelObject;
    private TMP_Text stateTitleText;
    private Button retryButton;
    private ScrollRect collectionScrollRect;
    private RectTransform contentRect;

    private GameObject detailOverlayObject;
    private TMP_Text detailTitleText;
    private RawImage detailThumbnailImage;
    private Image detailThumbnailPlaceholder;
    private TMP_Text detailPlaceholderText;
    private TMP_Text detailDescriptionText;
    private TMP_Text detailAcquiredDateText;
    private TMP_Text detailSpotNameText;
    private Button detailOpenArButton;

    private CollectedRewardItem selectedItem;
    private Coroutine loadRoutine;
    private Coroutine detailImageRoutine;
    private string currentDetailImageUrl = string.Empty;
    private CollectionViewState currentViewState = CollectionViewState.Hidden;

    public void Initialize()
    {
        rewardApiService = new RewardApiService(new ApiClient());
        BuildUi();
    }

    private void BuildUi()
    {
        rootRect = gameObject.GetComponent<RectTransform>();
        if (rootRect == null)
        {
            rootRect = gameObject.AddComponent<RectTransform>();
        }

        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject safeAreaObject = CreateUiObject("SafeArea", rootRect);
        safeAreaRect = safeAreaObject.GetComponent<RectTransform>();
        RuntimeSafeAreaFitter safeAreaFitter = safeAreaObject.AddComponent<RuntimeSafeAreaFitter>();
        safeAreaFitter.Configure(new Vector2(16f, 16f), new Vector2(16f, 16f));

        openCollectionButton = CreateButton(
            "OpenCollectionButton",
            safeAreaRect,
            "Collection",
            new Vector2(-16f, -18f),
            new Vector2(174f, 54f),
            new Color(0.09f, 0.22f, 0.36f, 0.92f),
            OpenCollectionPanel);

        RectTransform collectionButtonRect = openCollectionButton.GetComponent<RectTransform>();
        collectionButtonRect.anchorMin = new Vector2(1f, 1f);
        collectionButtonRect.anchorMax = new Vector2(1f, 1f);
        collectionButtonRect.pivot = new Vector2(1f, 1f);

        overlayObject = CreateUiObject("CollectionOverlay", rootRect);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        StretchRect(overlayRect, Vector2.zero, Vector2.zero);
        overlayObject.SetActive(false);

        Image overlayImage = overlayObject.AddComponent<Image>();
        overlayImage.sprite = GetDefaultSprite();
        overlayImage.color = new Color(0.04f, 0.07f, 0.11f, 0.84f);

        GameObject overlaySafeAreaObject = CreateUiObject("OverlaySafeArea", overlayRect);
        RectTransform overlaySafeAreaRect = overlaySafeAreaObject.GetComponent<RectTransform>();
        RuntimeSafeAreaFitter overlaySafeAreaFitter = overlaySafeAreaObject.AddComponent<RuntimeSafeAreaFitter>();
        overlaySafeAreaFitter.Configure(new Vector2(16f, 16f), new Vector2(16f, 16f));

        GameObject panelObject = CreateUiObject("CollectionPanel", overlaySafeAreaRect);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.06f, 0.08f);
        panelRect.anchorMax = new Vector2(0.94f, 0.92f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        Image panelImage = panelObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyPanelChrome(panelImage, new Color(0.97f, 0.98f, 0.98f, 0.98f));

        TMP_Text headerText = CreateText(
            "HeaderText",
            panelRect,
            "Collection",
            30f,
            FontStyles.Bold,
            new Color(0.08f, 0.12f, 0.18f, 1f),
            TextAlignmentOptions.Left);
        headerText.rectTransform.anchorMin = new Vector2(0f, 1f);
        headerText.rectTransform.anchorMax = new Vector2(1f, 1f);
        headerText.rectTransform.pivot = new Vector2(0f, 1f);
        headerText.rectTransform.offsetMin = new Vector2(22f, -54f);
        headerText.rectTransform.offsetMax = new Vector2(-130f, -12f);
        RuntimeGameUiTheme.StyleTitleText(headerText, new Color(0.08f, 0.12f, 0.18f, 1f));

        GameObject headerBadgeObject = CreateUiObject("HeaderBadge", panelRect);
        RectTransform headerBadgeRect = headerBadgeObject.GetComponent<RectTransform>();
        headerBadgeRect.anchorMin = new Vector2(1f, 1f);
        headerBadgeRect.anchorMax = new Vector2(1f, 1f);
        headerBadgeRect.pivot = new Vector2(1f, 1f);
        headerBadgeRect.anchoredPosition = new Vector2(-138f, -16f);
        headerBadgeRect.sizeDelta = new Vector2(130f, 36f);

        Image headerBadgeImage = headerBadgeObject.AddComponent<Image>();
        headerBadgeImage.sprite = GetDefaultSprite();
        headerBadgeImage.color = new Color(0.10f, 0.55f, 0.59f, 0.94f);

        headerBadgeText = CreateText(
            "HeaderBadgeText",
            headerBadgeRect,
            "0 rewards",
            17f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(headerBadgeText.rectTransform, new Vector2(6f, 6f), new Vector2(-6f, -6f));
        RuntimeGameUiTheme.StyleButtonLabel(headerBadgeText);

        Button closeButton = CreateButton(
            "CloseButton",
            panelRect,
            "Close",
            new Vector2(-18f, -18f),
            new Vector2(110f, 44f),
            new Color(0.57f, 0.18f, 0.19f, 0.95f),
            CloseCollectionPanel);
        RectTransform closeButtonRect = closeButton.GetComponent<RectTransform>();
        closeButtonRect.anchorMin = new Vector2(1f, 1f);
        closeButtonRect.anchorMax = new Vector2(1f, 1f);
        closeButtonRect.pivot = new Vector2(1f, 1f);

        statusText = CreateText(
            "StatusText",
            panelRect,
            "Ready",
            18f,
            FontStyles.Normal,
            new Color(0.17f, 0.31f, 0.45f, 1f),
            TextAlignmentOptions.Left);
        statusText.rectTransform.anchorMin = new Vector2(0f, 1f);
        statusText.rectTransform.anchorMax = new Vector2(1f, 1f);
        statusText.rectTransform.pivot = new Vector2(0f, 1f);
        statusText.rectTransform.offsetMin = new Vector2(22f, -88f);
        statusText.rectTransform.offsetMax = new Vector2(-22f, -58f);

        retryButton = CreateButton(
            "RetryButton",
            panelRect,
            "Retry",
            new Vector2(22f, 90f),
            new Vector2(120f, 46f),
            new Color(0.12f, 0.46f, 0.67f, 0.95f),
            RetryFetch);
        RectTransform retryRect = retryButton.GetComponent<RectTransform>();
        retryRect.anchorMin = new Vector2(0f, 0f);
        retryRect.anchorMax = new Vector2(0f, 0f);
        retryRect.pivot = new Vector2(0f, 0f);
        retryButton.gameObject.SetActive(false);

        statePanelObject = CreateUiObject("StatePanel", panelRect);
        RectTransform statePanelRect = statePanelObject.GetComponent<RectTransform>();
        statePanelRect.anchorMin = new Vector2(0.5f, 0.5f);
        statePanelRect.anchorMax = new Vector2(0.5f, 0.5f);
        statePanelRect.pivot = new Vector2(0.5f, 0.5f);
        statePanelRect.anchoredPosition = new Vector2(0f, -18f);
        statePanelRect.sizeDelta = new Vector2(460f, 220f);
        statePanelObject.SetActive(false);

        Image statePanelImage = statePanelObject.AddComponent<Image>();
        statePanelImage.sprite = GetDefaultSprite();
        statePanelImage.color = new Color(0.93f, 0.96f, 0.98f, 1f);
        ApplyShadow(statePanelImage, new Color(0f, 0f, 0f, 0.12f), new Vector2(0f, -6f));
        RuntimeGameUiTheme.ApplyPanelChrome(statePanelImage, new Color(0.93f, 0.96f, 0.98f, 1f));

        stateTitleText = CreateText(
            "StateTitleText",
            statePanelRect,
            "Collection",
            28f,
            FontStyles.Bold,
            new Color(0.08f, 0.12f, 0.18f, 1f),
            TextAlignmentOptions.Center);
        stateTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        stateTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        stateTitleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        stateTitleText.rectTransform.offsetMin = new Vector2(18f, -58f);
        stateTitleText.rectTransform.offsetMax = new Vector2(-18f, -18f);
        RuntimeGameUiTheme.StyleTitleText(stateTitleText, new Color(0.08f, 0.12f, 0.18f, 1f));

        emptyStateText = CreateText(
            "StateBodyText",
            statePanelRect,
            "No collected rewards yet.",
            21f,
            FontStyles.Normal,
            new Color(0.26f, 0.31f, 0.37f, 1f),
            TextAlignmentOptions.Center);
        emptyStateText.rectTransform.anchorMin = new Vector2(0f, 0f);
        emptyStateText.rectTransform.anchorMax = new Vector2(1f, 1f);
        emptyStateText.rectTransform.offsetMin = new Vector2(28f, 30f);
        emptyStateText.rectTransform.offsetMax = new Vector2(-28f, -72f);

        GameObject scrollObject = CreateUiObject("CollectionScrollView", panelRect);
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        scrollRect.anchorMin = new Vector2(0f, 0f);
        scrollRect.anchorMax = new Vector2(1f, 1f);
        scrollRect.offsetMin = new Vector2(20f, 18f);
        scrollRect.offsetMax = new Vector2(-20f, -102f);

        Image scrollBackground = scrollObject.AddComponent<Image>();
        scrollBackground.sprite = GetDefaultSprite();
        scrollBackground.color = new Color(0.93f, 0.96f, 0.97f, 1f);
        Mask scrollMask = scrollObject.AddComponent<Mask>();
        scrollMask.showMaskGraphic = true;

        collectionScrollRect = scrollObject.AddComponent<ScrollRect>();
        collectionScrollRect.horizontal = false;
        collectionScrollRect.movementType = ScrollRect.MovementType.Clamped;
        collectionScrollRect.scrollSensitivity = 24f;

        GameObject viewportObject = CreateUiObject("Viewport", scrollRect);
        RectTransform viewportRect = viewportObject.GetComponent<RectTransform>();
        StretchRect(viewportRect, Vector2.zero, Vector2.zero);

        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(1f, 1f, 1f, 0.01f);
        Mask viewportMask = viewportObject.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        GameObject contentObject = CreateUiObject("Content", viewportRect);
        contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = new Vector2(8f, 0f);
        contentRect.offsetMax = new Vector2(-8f, 0f);
        contentRect.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup layoutGroup = contentObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.spacing = 12f;
        layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        layoutGroup.childControlWidth = true;
        layoutGroup.childControlHeight = false;
        layoutGroup.childForceExpandWidth = true;
        layoutGroup.childForceExpandHeight = false;

        ContentSizeFitter sizeFitter = contentObject.AddComponent<ContentSizeFitter>();
        sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        collectionScrollRect.viewport = viewportRect;
        collectionScrollRect.content = contentRect;

        BuildDetailOverlay(overlayRect);
    }

    private void BuildDetailOverlay(RectTransform overlayRect)
    {
        detailOverlayObject = CreateUiObject("DetailOverlay", overlayRect);
        RectTransform detailOverlayRect = detailOverlayObject.GetComponent<RectTransform>();
        StretchRect(detailOverlayRect, Vector2.zero, Vector2.zero);
        detailOverlayObject.SetActive(false);

        Image backdropImage = detailOverlayObject.AddComponent<Image>();
        backdropImage.sprite = GetDefaultSprite();
        backdropImage.color = new Color(0.03f, 0.06f, 0.10f, 0.82f);

        GameObject detailPanel = CreateUiObject("DetailPanel", detailOverlayRect);
        RectTransform detailPanelRect = detailPanel.GetComponent<RectTransform>();
        detailPanelRect.anchorMin = new Vector2(0.11f, 0.14f);
        detailPanelRect.anchorMax = new Vector2(0.89f, 0.86f);
        detailPanelRect.offsetMin = Vector2.zero;
        detailPanelRect.offsetMax = Vector2.zero;

        Image detailPanelImage = detailPanel.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyPanelChrome(detailPanelImage, new Color(0.98f, 0.99f, 0.98f, 1f));

        detailTitleText = CreateText(
            "DetailTitleText",
            detailPanelRect,
            "Reward",
            30f,
            FontStyles.Bold,
            new Color(0.08f, 0.12f, 0.18f, 1f),
            TextAlignmentOptions.Left);
        detailTitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        detailTitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        detailTitleText.rectTransform.pivot = new Vector2(0f, 1f);
        detailTitleText.rectTransform.offsetMin = new Vector2(22f, -58f);
        detailTitleText.rectTransform.offsetMax = new Vector2(-128f, -12f);
        RuntimeGameUiTheme.StyleTitleText(detailTitleText, new Color(0.08f, 0.12f, 0.18f, 1f));

        Button detailCloseButton = CreateButton(
            "DetailCloseButton",
            detailPanelRect,
            "Close",
            new Vector2(-18f, -18f),
            new Vector2(108f, 42f),
            new Color(0.57f, 0.18f, 0.19f, 0.95f),
            CloseDetailPanel);
        RectTransform detailCloseRect = detailCloseButton.GetComponent<RectTransform>();
        detailCloseRect.anchorMin = new Vector2(1f, 1f);
        detailCloseRect.anchorMax = new Vector2(1f, 1f);
        detailCloseRect.pivot = new Vector2(1f, 1f);

        GameObject previewFrame = CreateUiObject("PreviewFrame", detailPanelRect);
        RectTransform previewFrameRect = previewFrame.GetComponent<RectTransform>();
        previewFrameRect.anchorMin = new Vector2(0f, 1f);
        previewFrameRect.anchorMax = new Vector2(0f, 1f);
        previewFrameRect.pivot = new Vector2(0f, 1f);
        previewFrameRect.anchoredPosition = new Vector2(22f, -86f);
        previewFrameRect.sizeDelta = new Vector2(220f, 220f);

        Image previewFrameImage = previewFrame.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyPanelChrome(previewFrameImage, new Color(0.88f, 0.92f, 0.97f, 1f));

        GameObject detailThumbnailObject = CreateUiObject("DetailThumbnail", previewFrameRect);
        RectTransform detailThumbnailRect = detailThumbnailObject.GetComponent<RectTransform>();
        StretchRect(detailThumbnailRect, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        detailThumbnailImage = detailThumbnailObject.AddComponent<RawImage>();
        detailThumbnailImage.color = Color.white;
        detailThumbnailImage.gameObject.SetActive(false);

        GameObject detailPlaceholderObject = CreateUiObject("DetailPlaceholder", previewFrameRect);
        RectTransform detailPlaceholderRect = detailPlaceholderObject.GetComponent<RectTransform>();
        StretchRect(detailPlaceholderRect, new Vector2(10f, 10f), new Vector2(-10f, -10f));
        detailThumbnailPlaceholder = detailPlaceholderObject.AddComponent<Image>();
        detailThumbnailPlaceholder.sprite = GetDefaultSprite();
        detailThumbnailPlaceholder.color = new Color(0.22f, 0.45f, 0.80f, 0.92f);

        detailPlaceholderText = CreateText(
            "DetailPlaceholderText",
            detailPlaceholderRect,
            "R",
            44f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(detailPlaceholderText.rectTransform, Vector2.zero, Vector2.zero);
        RuntimeGameUiTheme.StyleButtonLabel(detailPlaceholderText);

        detailDescriptionText = CreateText(
            "DetailDescriptionText",
            detailPanelRect,
            "Description",
            19f,
            FontStyles.Normal,
            new Color(0.23f, 0.29f, 0.35f, 1f),
            TextAlignmentOptions.TopLeft);
        detailDescriptionText.rectTransform.anchorMin = new Vector2(0f, 1f);
        detailDescriptionText.rectTransform.anchorMax = new Vector2(1f, 1f);
        detailDescriptionText.rectTransform.pivot = new Vector2(0f, 1f);
        detailDescriptionText.rectTransform.offsetMin = new Vector2(260f, -220f);
        detailDescriptionText.rectTransform.offsetMax = new Vector2(-22f, -88f);

        detailAcquiredDateText = CreateText(
            "DetailAcquiredDateText",
            detailPanelRect,
            "Acquired: -",
            18f,
            FontStyles.Bold,
            new Color(0.07f, 0.43f, 0.59f, 1f),
            TextAlignmentOptions.Left);
        detailAcquiredDateText.rectTransform.anchorMin = new Vector2(0f, 1f);
        detailAcquiredDateText.rectTransform.anchorMax = new Vector2(1f, 1f);
        detailAcquiredDateText.rectTransform.pivot = new Vector2(0f, 1f);
        detailAcquiredDateText.rectTransform.offsetMin = new Vector2(260f, -262f);
        detailAcquiredDateText.rectTransform.offsetMax = new Vector2(-22f, -234f);
        RuntimeGameUiTheme.StyleAccentText(detailAcquiredDateText, new Color(0.07f, 0.43f, 0.59f, 1f));

        detailSpotNameText = CreateText(
            "DetailSpotNameText",
            detailPanelRect,
            "Collected at: -",
            18f,
            FontStyles.Normal,
            new Color(0.18f, 0.24f, 0.31f, 1f),
            TextAlignmentOptions.Left);
        detailSpotNameText.rectTransform.anchorMin = new Vector2(0f, 1f);
        detailSpotNameText.rectTransform.anchorMax = new Vector2(1f, 1f);
        detailSpotNameText.rectTransform.pivot = new Vector2(0f, 1f);
        detailSpotNameText.rectTransform.offsetMin = new Vector2(260f, -298f);
        detailSpotNameText.rectTransform.offsetMax = new Vector2(-22f, -270f);

        detailOpenArButton = CreateButton(
            "DetailOpenArButton",
            detailPanelRect,
            "Open AR Preview",
            new Vector2(-22f, 20f),
            new Vector2(220f, 54f),
            new Color(0.13f, 0.61f, 0.46f, 1f),
            OpenSelectedRewardPreview);
        RectTransform detailOpenRect = detailOpenArButton.GetComponent<RectTransform>();
        detailOpenRect.anchorMin = new Vector2(1f, 0f);
        detailOpenRect.anchorMax = new Vector2(1f, 0f);
        detailOpenRect.pivot = new Vector2(1f, 0f);
    }

    private void OpenCollectionPanel()
    {
        transform.SetAsLastSibling();
        overlayObject.SetActive(true);
        detailOverlayObject.SetActive(false);
        selectedItem = null;

        if (cachedRewards.Count > 0)
        {
            RefreshCards();
            ShowContentState("Refreshing collection...");
        }
        else
        {
            ShowLoadingState("Loading your collected rewards...", false);
        }

        RetryFetch();
    }

    private void CloseCollectionPanel()
    {
        detailOverlayObject.SetActive(false);
        overlayObject.SetActive(false);
    }

    private void RetryFetch()
    {
        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
        }

        loadRoutine = StartCoroutine(FetchCollection());
    }

    private IEnumerator FetchCollection()
    {
        ShowLoadingState("Checking your collection...", cachedRewards.Count > 0);

        if (string.IsNullOrEmpty(GameSession.userId))
        {
            ShowRetryState("Collection is not ready yet. Guest login is still loading.");
            loadRoutine = null;
            yield break;
        }

        UserRewardsResponseDto response = null;
        ApiClientError apiError = null;

        yield return rewardApiService.GetUserRewards(
            GameSession.userId,
            value => response = value,
            error => apiError = error);

        loadRoutine = null;

        if (response != null && response.rewards != null)
        {
            cachedRewards.Clear();

            for (int i = 0; i < response.rewards.Count; i++)
            {
                CollectedRewardItem rewardItem = CollectedRewardItem.FromClaim(response.rewards[i]);
                if (rewardItem != null)
                {
                    cachedRewards.Add(rewardItem);
                }
            }

            cachedRewards.Sort((left, right) => string.CompareOrdinal(
                right != null ? right.claimedAtRaw : string.Empty,
                left != null ? left.claimedAtRaw : string.Empty));

            RefreshCards();

            if (cachedRewards.Count == 0)
            {
                ShowEmptyState("No collected rewards yet. Open AR near a tourism spot to earn your first reward.");
            }
            else
            {
                ShowContentState("Collected rewards: " + cachedRewards.Count);
            }

            yield break;
        }

        if (cachedRewards.Count > 0)
        {
            RefreshCards();
            ShowContentState("Could not refresh. Showing last synced collection.");
            retryButton.gameObject.SetActive(true);
            yield break;
        }

        ShowRetryState(apiError != null ? apiError.message : "Failed to load collection.");
    }

    private void RefreshCards()
    {
        EnsureCardCount(cachedRewards.Count);

        for (int i = 0; i < cardViews.Count; i++)
        {
            if (i >= cachedRewards.Count)
            {
                cardViews[i].gameObject.SetActive(false);
                continue;
            }

            cardViews[i].gameObject.SetActive(true);
            cardViews[i].Bind(cachedRewards[i], OpenDetailPanel, textureCache, this);
        }

        emptyStateText.gameObject.SetActive(cachedRewards.Count == 0);
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        Canvas.ForceUpdateCanvases();
        collectionScrollRect.verticalNormalizedPosition = 1f;
    }

    private void EnsureCardCount(int requiredCount)
    {
        while (cardViews.Count < requiredCount)
        {
            GameObject cardObject = CreateUiObject("RewardCard", contentRect);
            CollectionRewardCardView cardView = cardObject.AddComponent<CollectionRewardCardView>();
            cardView.Initialize();
            cardViews.Add(cardView);
        }
    }

    private void OpenDetailPanel(CollectedRewardItem item)
    {
        selectedItem = item;
        detailOverlayObject.SetActive(true);

        detailTitleText.text = item != null ? item.rewardName : "Reward";
        detailDescriptionText.text = item != null && !string.IsNullOrEmpty(item.rewardDescription)
            ? item.rewardDescription
            : "No description available yet.";
        detailAcquiredDateText.text = item != null ? "Acquired: " + item.claimedAtDisplay : "Acquired: -";
        detailSpotNameText.text = item != null && !string.IsNullOrEmpty(item.tourismSpotName)
            ? "Collected at: " + item.tourismSpotName
            : "Collected at: Unknown spot";

        detailOpenArButton.interactable = item != null;
        SetDetailThumbnail(null, item != null ? item.rewardName : string.Empty);

        if (detailImageRoutine != null)
        {
            StopCoroutine(detailImageRoutine);
            detailImageRoutine = null;
        }

        currentDetailImageUrl = item != null ? item.rewardImageUrl : string.Empty;

        if (item != null && !string.IsNullOrEmpty(item.rewardImageUrl))
        {
            detailImageRoutine = StartCoroutine(LoadDetailThumbnail(item.rewardImageUrl));
        }
    }

    private IEnumerator LoadDetailThumbnail(string imageUrl)
    {
        Texture2D loadedTexture = null;
        string errorMessage = null;

        yield return textureCache.LoadTexture(
            imageUrl,
            texture => loadedTexture = texture,
            error => errorMessage = error);

        detailImageRoutine = null;

        if (currentDetailImageUrl != imageUrl)
        {
            yield break;
        }

        if (loadedTexture != null)
        {
            SetDetailThumbnail(loadedTexture, selectedItem != null ? selectedItem.rewardName : string.Empty);
            yield break;
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            Debug.LogWarning("Detail thumbnail failed: " + errorMessage);
        }
    }

    private void SetDetailThumbnail(Texture texture, string rewardName)
    {
        bool hasTexture = texture != null;
        detailThumbnailImage.texture = texture;
        detailThumbnailImage.gameObject.SetActive(hasTexture);
        detailThumbnailPlaceholder.gameObject.SetActive(!hasTexture);

        string placeholderLabel = !string.IsNullOrEmpty(rewardName)
            ? rewardName.Substring(0, 1).ToUpperInvariant()
            : "R";
        detailPlaceholderText.text = placeholderLabel;
    }

    private void CloseDetailPanel()
    {
        detailOverlayObject.SetActive(false);
    }

    private void OpenSelectedRewardPreview()
    {
        if (selectedItem == null)
        {
            Debug.Log("[CollectionScreenController] OpenSelectedRewardPreview ignored because no reward is selected.");
            return;
        }

        Debug.Log("[CollectionScreenController] Opening AR preview for reward=" + selectedItem.rewardName + ", rewardId=" + selectedItem.rewardId);
        GameSession.SetCollectionPreviewData(
            selectedItem.rewardId,
            selectedItem.rewardName,
            selectedItem.rewardDescription,
            selectedItem.rewardImageUrl,
            selectedItem.previewPrefabKey,
            selectedItem.claimedAtRaw,
            selectedItem.tourismSpotName);

        Debug.Log("[CollectionScreenController] Loading ARScene in collection preview mode.");
        SceneManager.LoadScene("ARScene");
    }

    private void ShowLoadingState(string message, bool keepContentVisible)
    {
        currentViewState = CollectionViewState.Loading;
        SetStatus(message);
        retryButton.gameObject.SetActive(false);
        UpdateHeaderBadge();

        if (keepContentVisible && cachedRewards.Count > 0)
        {
            statePanelObject.SetActive(false);
            collectionScrollRect.gameObject.SetActive(true);
            return;
        }

        statePanelObject.SetActive(true);
        stateTitleText.text = "Loading Collection";
        emptyStateText.text = message;
        collectionScrollRect.gameObject.SetActive(false);
    }

    private void ShowContentState(string message)
    {
        currentViewState = CollectionViewState.Content;
        SetStatus(message);
        UpdateHeaderBadge();
        retryButton.gameObject.SetActive(false);
        statePanelObject.SetActive(false);
        collectionScrollRect.gameObject.SetActive(true);
    }

    private void ShowRetryState(string message)
    {
        currentViewState = CollectionViewState.Retry;
        SetStatus(message);
        UpdateHeaderBadge();
        retryButton.gameObject.SetActive(true);
        statePanelObject.SetActive(true);
        stateTitleText.text = "Couldn't Load Collection";
        emptyStateText.text = "We couldn't reach the collection service right now.\n\n" + message;
        collectionScrollRect.gameObject.SetActive(false);
    }

    private void ShowEmptyState(string message)
    {
        currentViewState = CollectionViewState.Empty;
        SetStatus("No collected rewards yet.");
        UpdateHeaderBadge();
        statePanelObject.SetActive(true);
        stateTitleText.text = "Collection is Empty";
        emptyStateText.text = message;
        collectionScrollRect.gameObject.SetActive(false);
        retryButton.gameObject.SetActive(true);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    private void UpdateHeaderBadge()
    {
        if (headerBadgeText == null)
        {
            return;
        }

        int rewardCount = cachedRewards.Count;
        headerBadgeText.text = rewardCount == 1 ? "1 reward" : rewardCount + " rewards";
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
        buttonRect.sizeDelta = sizeDelta;
        buttonRect.anchoredPosition = anchoredPosition;

        Image buttonImage = buttonObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyButtonChrome(buttonImage, color);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);

        TMP_Text label = CreateText(
            "Label",
            buttonRect,
            buttonText,
            20f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(label.rectTransform, new Vector2(8f, 8f), new Vector2(-8f, -8f));
        RuntimeGameUiTheme.StyleButtonLabel(label);
        label.transform.SetAsLastSibling();

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

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
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

    private void OnDestroy()
    {
        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
        }

        if (detailImageRoutine != null)
        {
            StopCoroutine(detailImageRoutine);
        }
    }
}
