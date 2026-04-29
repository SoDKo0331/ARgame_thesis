using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RewardPanelController : MonoBehaviour
{
    public TMP_Text titleText;
    public TMP_Text descriptionText;
    public TMP_Text statusText;
    public RawImage rewardImage;
    public Image rewardPlaceholder;
    public TMP_Text rewardPlaceholderText;

    private readonly RemoteTextureCache textureCache = new RemoteTextureCache();
    private Coroutine claimRoutine;
    private Coroutine claimWatchRoutine;
    private Coroutine imageLoadRoutine;
    private string currentImageUrl = string.Empty;

    private void Awake()
    {
        Debug.Log("[RewardPanelController] Awake");
        ResolveMissingReferences();
        ApplyVisualTheme();
    }

    private void OnEnable()
    {
        Debug.Log("[RewardPanelController] OnEnable. reward=" + GameSession.rewardName + ", claimRequested=" + GameSession.rewardClaimRequested);
        ResolveMissingReferences();
        ApplyVisualTheme();
        Refresh();

        if (claimWatchRoutine != null)
        {
            StopCoroutine(claimWatchRoutine);
            claimWatchRoutine = null;
        }

        if (claimRoutine != null)
        {
            StopCoroutine(claimRoutine);
        }

        if (BackendBootstrap.Instance != null && BackendBootstrap.Instance.IsClaimInProgress)
        {
            ShowClaimStatus(string.IsNullOrEmpty(GameSession.backendStatusMessage)
                ? "Collection-д нэмж байна..."
                : GameSession.backendStatusMessage);
            claimWatchRoutine = StartCoroutine(WatchBackgroundClaim());
        }
        else if (GameSession.rewardClaimRequested)
        {
            ShowClaimStatus(!string.IsNullOrEmpty(GameSession.backendStatusMessage)
                ? GameSession.backendStatusMessage
                : (GameSession.alreadyClaimed
                    ? "Энэ шагнал аль хэдийн collection-д байна."
                    : "Collection-д нэмэгдлээ!"));
        }
        else
        {
            claimRoutine = StartCoroutine(ClaimRewardFromBackend());
        }
    }

    public void Refresh()
    {
        Debug.Log("[RewardPanelController] Refresh. reward=" + GameSession.rewardName + ", alreadyClaimed=" + GameSession.alreadyClaimed);
        if (titleText != null)
            titleText.text = "Шагнал: " + GameSession.rewardName;

        if (descriptionText != null)
            descriptionText.text = GameSession.rewardDescription;

        RefreshRewardVisual();
    }

    private IEnumerator ClaimRewardFromBackend()
    {
        if (BackendBootstrap.Instance == null)
        {
            Debug.Log("[RewardPanelController] Claim skipped because BackendBootstrap is missing.");
            SetStatus("Локал reward мэдээлэл харуулж байна.");
            yield break;
        }

        if (!BackendBootstrap.Instance.HasBootstrappedSession ||
            string.IsNullOrEmpty(GameSession.selectedSpotId) ||
            string.IsNullOrEmpty(GameSession.userId))
        {
            Debug.Log("[RewardPanelController] Claim skipped because session is not ready. userId=" + GameSession.userId + ", spotId=" + GameSession.selectedSpotId);
            SetStatus("Локал reward мэдээлэл харуулж байна.");
            yield break;
        }

        GameSession.rewardClaimRequested = true;
        Debug.Log("[RewardPanelController] Starting backend claim from reward panel for spotId=" + GameSession.selectedSpotId);
        SetStatus("Collection-д нэмж байна...");

        ClaimRewardResponseDto response = null;
        ApiClientError apiError = null;

        yield return StartCoroutine(BackendBootstrap.Instance.ClaimSelectedSpot(
            value => response = value,
            error => apiError = error));

        if (response != null)
        {
            Debug.Log("[RewardPanelController] Backend claim completed. alreadyClaimed=" + response.alreadyClaimed);
            Refresh();
            SetStatus(response.alreadyClaimed ? "Энэ шагнал аль хэдийн collection-д байна." : "Collection-д нэмэгдлээ!");
            claimRoutine = null;
            yield break;
        }

        if (apiError != null)
        {
            GameSession.rewardClaimRequested = false;
            SetStatus("Интернетгүй тул локал reward мэдээлэл харуулж байна.");
            Debug.LogWarning("[RewardPanelController] Reward claim failed: " + apiError.message);
        }

        claimRoutine = null;
    }

    private IEnumerator WatchBackgroundClaim()
    {
        Debug.Log("[RewardPanelController] Watching background claim...");
        while (BackendBootstrap.Instance != null && BackendBootstrap.Instance.IsClaimInProgress)
        {
            if (!string.IsNullOrEmpty(GameSession.backendStatusMessage))
            {
                SetStatus(GameSession.backendStatusMessage);
            }

            yield return null;
        }

        Refresh();

        if (GameSession.rewardClaimRequested)
        {
            SetStatus(GameSession.alreadyClaimed
                ? "Энэ шагнал аль хэдийн collection-д байна."
                : "Collection-д нэмэгдлээ!");
        }

        Debug.Log("[RewardPanelController] Background claim watch completed.");
        claimWatchRoutine = null;
    }

    private void OnDisable()
    {
        if (claimRoutine != null)
        {
            StopCoroutine(claimRoutine);
            claimRoutine = null;
        }

        if (imageLoadRoutine != null)
        {
            StopCoroutine(imageLoadRoutine);
            imageLoadRoutine = null;
        }

        if (claimWatchRoutine != null)
        {
            StopCoroutine(claimWatchRoutine);
            claimWatchRoutine = null;
        }
    }

    private void SetStatus(string message)
    {
        GameSession.backendStatusMessage = message;

        if (statusText != null)
        {
            statusText.text = message;
        }
    }

    public void ShowClaimStatus(string message)
    {
        Debug.Log("[RewardPanelController] ShowClaimStatus => " + message);
        SetStatus(message);
    }

    public void ClosePanel()
    {
        Debug.Log("[RewardPanelController] ClosePanel pressed.");
        gameObject.SetActive(false);
    }

    private void ResolveMissingReferences()
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        if (titleText == null)
        {
            titleText = FindTextByName(texts, "TitleText");
        }

        if (descriptionText == null)
        {
            descriptionText = FindTextByName(texts, "DescriptionText");
        }

        if (statusText == null)
        {
            statusText = FindTextByName(texts, "StatusText");
        }

        if (titleText == null && texts.Length > 0)
        {
            titleText = texts[0];
        }

        if (descriptionText == null && texts.Length > 1)
        {
            descriptionText = texts[1];
        }
    }

    private void ApplyVisualTheme()
    {
        BuildRuntimeLayout();

        if (titleText != null)
        {
            RuntimeGameUiTheme.StyleTitleText(titleText, new Color(1f, 0.96f, 0.82f, 1f));
        }

        if (descriptionText != null)
        {
            descriptionText.color = new Color(0.14f, 0.18f, 0.23f, 0.86f);
            descriptionText.characterSpacing = 1.1f;
        }

        if (statusText != null)
        {
            RuntimeGameUiTheme.StyleAccentText(statusText, new Color(0.62f, 0.92f, 0.76f, 1f));
        }

        Button[] buttons = GetComponentsInChildren<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Image buttonImage = buttons[i].targetGraphic as Image;
            if (buttonImage != null)
            {
                Color baseColor = buttonImage.color.a > 0.01f
                    ? buttonImage.color
                    : new Color(0.14f, 0.62f, 0.46f, 0.96f);
                RuntimeGameUiTheme.ApplyButtonChrome(buttonImage, baseColor);
            }

            TMP_Text label = buttons[i].GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                RuntimeGameUiTheme.StyleButtonLabel(label);
                label.transform.SetAsLastSibling();
            }
        }
    }

    private void RefreshRewardVisual()
    {
        SetRewardVisual(null, GameSession.rewardName);

        if (imageLoadRoutine != null)
        {
            StopCoroutine(imageLoadRoutine);
            imageLoadRoutine = null;
        }

        currentImageUrl = GameSession.rewardImageUrl;
        if (!string.IsNullOrEmpty(currentImageUrl))
        {
            imageLoadRoutine = StartCoroutine(LoadRewardImage(currentImageUrl));
        }
    }

    private IEnumerator LoadRewardImage(string imageUrl)
    {
        Texture2D loadedTexture = null;
        string errorMessage = null;

        yield return textureCache.LoadTexture(
            imageUrl,
            texture => loadedTexture = texture,
            error => errorMessage = error);

        imageLoadRoutine = null;

        if (currentImageUrl != imageUrl)
        {
            yield break;
        }

        if (loadedTexture != null)
        {
            SetRewardVisual(loadedTexture, GameSession.rewardName);
            yield break;
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            Debug.LogWarning("Reward image failed: " + errorMessage);
        }
    }

    private void SetRewardVisual(Texture texture, string rewardName)
    {
        bool hasTexture = texture != null;

        if (rewardImage != null)
        {
            rewardImage.texture = texture;
            rewardImage.gameObject.SetActive(hasTexture);
        }

        if (rewardPlaceholder != null)
        {
            rewardPlaceholder.gameObject.SetActive(!hasTexture);
        }

        if (rewardPlaceholderText != null)
        {
            rewardPlaceholderText.text = !string.IsNullOrEmpty(rewardName)
                ? rewardName.Substring(0, 1).ToUpperInvariant()
                : "R";
        }
    }

    private void BuildRuntimeLayout()
    {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect == null)
        {
            return;
        }

        Image overlayImage = GetComponent<Image>();
        if (overlayImage != null)
        {
            overlayImage.color = new Color(0.03f, 0.07f, 0.10f, 0.78f);
        }

        RectTransform cardRect = FindOrCreateRect("RewardCard", rootRect);
        cardRect.anchorMin = new Vector2(0.08f, 0.18f);
        cardRect.anchorMax = new Vector2(0.92f, 0.82f);
        cardRect.offsetMin = Vector2.zero;
        cardRect.offsetMax = Vector2.zero;

        Image cardImage = cardRect.GetComponent<Image>();
        if (cardImage == null)
        {
            cardImage = cardRect.gameObject.AddComponent<Image>();
        }

        RuntimeGameUiTheme.ApplyPanelChrome(cardImage, new Color(0.98f, 0.99f, 0.98f, 0.98f));

        RectTransform imageFrameRect = FindOrCreateRect("RewardImageFrame", cardRect);
        imageFrameRect.anchorMin = new Vector2(0.5f, 1f);
        imageFrameRect.anchorMax = new Vector2(0.5f, 1f);
        imageFrameRect.pivot = new Vector2(0.5f, 1f);
        imageFrameRect.anchoredPosition = new Vector2(0f, -26f);
        imageFrameRect.sizeDelta = new Vector2(220f, 220f);

        Image imageFrameImage = imageFrameRect.GetComponent<Image>();
        if (imageFrameImage == null)
        {
            imageFrameImage = imageFrameRect.gameObject.AddComponent<Image>();
        }

        RuntimeGameUiTheme.ApplyPanelChrome(imageFrameImage, new Color(0.88f, 0.92f, 0.97f, 1f));

        rewardImage = FindOrCreateRawImage("RewardImage", imageFrameRect);
        StretchRect(rewardImage.rectTransform, new Vector2(12f, 12f), new Vector2(-12f, -12f));

        rewardPlaceholder = FindOrCreateImage("RewardPlaceholder", imageFrameRect);
        StretchRect(rewardPlaceholder.rectTransform, new Vector2(12f, 12f), new Vector2(-12f, -12f));
        RuntimeGameUiTheme.ApplyButtonChrome(rewardPlaceholder, new Color(0.18f, 0.70f, 0.54f, 0.96f));

        rewardPlaceholderText = FindOrCreateText(
            "RewardPlaceholderText",
            rewardPlaceholder.rectTransform,
            "R",
            52f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(rewardPlaceholderText.rectTransform, Vector2.zero, Vector2.zero);
        RuntimeGameUiTheme.StyleButtonLabel(rewardPlaceholderText);

        titleText = EnsureTextParentAndName(titleText, cardRect, "TitleText");
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.offsetMin = new Vector2(28f, -294f);
        titleText.rectTransform.offsetMax = new Vector2(-28f, -238f);
        titleText.alignment = TextAlignmentOptions.Center;

        descriptionText = EnsureTextParentAndName(descriptionText, cardRect, "DescriptionText");
        descriptionText.rectTransform.anchorMin = new Vector2(0f, 1f);
        descriptionText.rectTransform.anchorMax = new Vector2(1f, 1f);
        descriptionText.rectTransform.pivot = new Vector2(0.5f, 1f);
        descriptionText.rectTransform.offsetMin = new Vector2(32f, -390f);
        descriptionText.rectTransform.offsetMax = new Vector2(-32f, -300f);
        descriptionText.alignment = TextAlignmentOptions.Center;
        descriptionText.enableWordWrapping = true;

        statusText = EnsureTextParentAndName(statusText, cardRect, "StatusText");
        statusText.rectTransform.anchorMin = new Vector2(0f, 0f);
        statusText.rectTransform.anchorMax = new Vector2(1f, 0f);
        statusText.rectTransform.pivot = new Vector2(0.5f, 0f);
        statusText.rectTransform.offsetMin = new Vector2(28f, 86f);
        statusText.rectTransform.offsetMax = new Vector2(-28f, 126f);
        statusText.alignment = TextAlignmentOptions.Center;

        Button[] buttons = GetComponentsInChildren<Button>(true);
        if (buttons.Length > 0)
        {
            RectTransform buttonRect = buttons[0].GetComponent<RectTransform>();
            buttonRect.SetParent(cardRect, false);
            buttonRect.anchorMin = new Vector2(0.5f, 0f);
            buttonRect.anchorMax = new Vector2(0.5f, 0f);
            buttonRect.pivot = new Vector2(0.5f, 0f);
            buttonRect.anchoredPosition = new Vector2(0f, 24f);
            buttonRect.sizeDelta = new Vector2(240f, 58f);
        }
    }

    private static RectTransform FindOrCreateRect(string objectName, RectTransform parent)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null)
        {
            return existing as RectTransform;
        }

        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<RectTransform>();
    }

    private static Image FindOrCreateImage(string objectName, RectTransform parent)
    {
        RectTransform rect = FindOrCreateRect(objectName, parent);
        Image image = rect.GetComponent<Image>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<Image>();
        }

        return image;
    }

    private static RawImage FindOrCreateRawImage(string objectName, RectTransform parent)
    {
        RectTransform rect = FindOrCreateRect(objectName, parent);
        RawImage image = rect.GetComponent<RawImage>();
        if (image == null)
        {
            image = rect.gameObject.AddComponent<RawImage>();
        }

        image.color = Color.white;
        return image;
    }

    private static TMP_Text FindOrCreateText(
        string objectName,
        RectTransform parent,
        string value,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment)
    {
        Transform existing = parent.Find(objectName);
        TextMeshProUGUI text = existing != null
            ? existing.GetComponent<TextMeshProUGUI>()
            : null;

        if (text == null)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            text = textObject.AddComponent<TextMeshProUGUI>();
        }

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

    private static TMP_Text EnsureTextParentAndName(TMP_Text text, RectTransform parent, string objectName)
    {
        if (text == null)
        {
            text = FindOrCreateText(
                objectName,
                parent,
                string.Empty,
                24f,
                FontStyles.Normal,
                Color.white,
                TextAlignmentOptions.Center);
        }
        else
        {
            text.gameObject.name = objectName;
            text.rectTransform.SetParent(parent, false);
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

    private static TMP_Text FindTextByName(TMP_Text[] texts, string objectName)
    {
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null && texts[i].gameObject.name == objectName)
            {
                return texts[i];
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        if (imageLoadRoutine != null)
        {
            StopCoroutine(imageLoadRoutine);
            imageLoadRoutine = null;
        }
    }
}
