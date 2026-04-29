using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackendSettingsController : MonoBehaviour
{
    private static Sprite defaultSprite;

    private RectTransform rootRect;
    private RectTransform safeAreaRect;
    private Button openButton;
    private GameObject overlayObject;
    private Image stateBadgeImage;
    private TMP_Text stateBadgeText;
    private TMP_Text activeUrlText;
    private TMP_Text defaultUrlText;
    private TMP_Text backendStateText;
    private TMP_Text feedbackText;
    private TMP_InputField urlInputField;
    private string lastObservedStatusMessage = string.Empty;

    public void Initialize()
    {
        BuildUi();
        RefreshInfo();
    }

    private void Update()
    {
        UpdateOpenButtonState();

        if (overlayObject != null && overlayObject.activeSelf)
        {
            UpdateBackendStateText();
        }
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

        openButton = CreateButton(
            "ServerSettingsButton",
            safeAreaRect,
            "Server",
            new Vector2(16f, -18f),
            new Vector2(150f, 52f),
            new Color(0.15f, 0.24f, 0.36f, 0.94f),
            OpenPanel);

        RectTransform openRect = openButton.GetComponent<RectTransform>();
        openRect.anchorMin = new Vector2(0f, 1f);
        openRect.anchorMax = new Vector2(0f, 1f);
        openRect.pivot = new Vector2(0f, 1f);

        overlayObject = CreateUiObject("BackendSettingsOverlay", rootRect);
        RectTransform overlayRect = overlayObject.GetComponent<RectTransform>();
        StretchRect(overlayRect, Vector2.zero, Vector2.zero);
        overlayObject.SetActive(false);

        Image overlayImage = overlayObject.AddComponent<Image>();
        overlayImage.sprite = GetDefaultSprite();
        overlayImage.color = new Color(0.04f, 0.07f, 0.11f, 0.82f);

        GameObject overlaySafeAreaObject = CreateUiObject("OverlaySafeArea", overlayRect);
        RectTransform overlaySafeAreaRect = overlaySafeAreaObject.GetComponent<RectTransform>();
        RuntimeSafeAreaFitter overlaySafeAreaFitter = overlaySafeAreaObject.AddComponent<RuntimeSafeAreaFitter>();
        overlaySafeAreaFitter.Configure(new Vector2(16f, 16f), new Vector2(16f, 16f));

        GameObject panelObject = CreateUiObject("BackendSettingsPanel", overlaySafeAreaRect);
        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.10f, 0.18f);
        panelRect.anchorMax = new Vector2(0.90f, 0.82f);
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        RuntimeKeyboardAvoider keyboardAvoider = panelObject.AddComponent<RuntimeKeyboardAvoider>();
        keyboardAvoider.Configure(24f);

        Image panelImage = panelObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyPanelChrome(panelImage, new Color(0.98f, 0.99f, 0.99f, 0.99f));

        TMP_Text titleText = CreateText(
            "TitleText",
            panelRect,
            "Backend Server",
            30f,
            FontStyles.Bold,
            new Color(0.08f, 0.12f, 0.18f, 1f),
            TextAlignmentOptions.Left);
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.pivot = new Vector2(0f, 1f);
        titleText.rectTransform.offsetMin = new Vector2(24f, -58f);
        titleText.rectTransform.offsetMax = new Vector2(-128f, -18f);
        RuntimeGameUiTheme.StyleTitleText(titleText, new Color(0.08f, 0.12f, 0.18f, 1f));

        TMP_Text subtitleText = CreateText(
            "SubtitleText",
            panelRect,
            "Use this when your Mac's LAN IP changes during device testing.",
            18f,
            FontStyles.Normal,
            new Color(0.30f, 0.35f, 0.40f, 1f),
            TextAlignmentOptions.Left);
        subtitleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        subtitleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        subtitleText.rectTransform.pivot = new Vector2(0f, 1f);
        subtitleText.rectTransform.offsetMin = new Vector2(24f, -96f);
        subtitleText.rectTransform.offsetMax = new Vector2(-24f, -58f);

        Button closeButton = CreateButton(
            "CloseButton",
            panelRect,
            "Close",
            new Vector2(-18f, -18f),
            new Vector2(108f, 42f),
            new Color(0.57f, 0.18f, 0.19f, 0.95f),
            ClosePanel);
        RectTransform closeRect = closeButton.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);

        activeUrlText = CreateText(
            "ActiveUrlText",
            panelRect,
            string.Empty,
            20f,
            FontStyles.Bold,
            new Color(0.08f, 0.42f, 0.58f, 1f),
            TextAlignmentOptions.Left);
        activeUrlText.enableWordWrapping = false;
        activeUrlText.overflowMode = TextOverflowModes.Ellipsis;
        activeUrlText.rectTransform.anchorMin = new Vector2(0f, 1f);
        activeUrlText.rectTransform.anchorMax = new Vector2(1f, 1f);
        activeUrlText.rectTransform.pivot = new Vector2(0f, 1f);
        activeUrlText.rectTransform.offsetMin = new Vector2(24f, -146f);
        activeUrlText.rectTransform.offsetMax = new Vector2(-24f, -112f);

        defaultUrlText = CreateText(
            "DefaultUrlText",
            panelRect,
            string.Empty,
            17f,
            FontStyles.Normal,
            new Color(0.22f, 0.27f, 0.33f, 1f),
            TextAlignmentOptions.Left);
        defaultUrlText.enableWordWrapping = false;
        defaultUrlText.overflowMode = TextOverflowModes.Ellipsis;
        defaultUrlText.rectTransform.anchorMin = new Vector2(0f, 1f);
        defaultUrlText.rectTransform.anchorMax = new Vector2(1f, 1f);
        defaultUrlText.rectTransform.pivot = new Vector2(0f, 1f);
        defaultUrlText.rectTransform.offsetMin = new Vector2(24f, -182f);
        defaultUrlText.rectTransform.offsetMax = new Vector2(-24f, -152f);

        GameObject stateBadgeObject = CreateUiObject("StateBadge", panelRect);
        RectTransform stateBadgeRect = stateBadgeObject.GetComponent<RectTransform>();
        stateBadgeRect.anchorMin = new Vector2(0f, 1f);
        stateBadgeRect.anchorMax = new Vector2(0f, 1f);
        stateBadgeRect.pivot = new Vector2(0f, 1f);
        stateBadgeRect.anchoredPosition = new Vector2(24f, -210f);
        stateBadgeRect.sizeDelta = new Vector2(220f, 34f);

        stateBadgeImage = stateBadgeObject.AddComponent<Image>();
        stateBadgeImage.sprite = GetDefaultSprite();
        stateBadgeImage.color = new Color(0.62f, 0.66f, 0.70f, 0.95f);
        ApplyShadow(stateBadgeImage, new Color(0f, 0f, 0f, 0.10f), new Vector2(0f, -3f));

        stateBadgeText = CreateText(
            "StateBadgeText",
            stateBadgeRect,
            "Checking...",
            17f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(stateBadgeText.rectTransform, new Vector2(8f, 4f), new Vector2(-8f, -4f));
        RuntimeGameUiTheme.StyleButtonLabel(stateBadgeText);

        TMP_Text inputLabelText = CreateText(
            "InputLabelText",
            panelRect,
            "Custom backend URL",
            18f,
            FontStyles.Bold,
            new Color(0.10f, 0.15f, 0.20f, 1f),
            TextAlignmentOptions.Left);
        inputLabelText.rectTransform.anchorMin = new Vector2(0f, 1f);
        inputLabelText.rectTransform.anchorMax = new Vector2(1f, 1f);
        inputLabelText.rectTransform.pivot = new Vector2(0f, 1f);
        inputLabelText.rectTransform.offsetMin = new Vector2(24f, -258f);
        inputLabelText.rectTransform.offsetMax = new Vector2(-24f, -224f);

        urlInputField = CreateInputField(panelRect);
        RectTransform inputRect = urlInputField.GetComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0f, 1f);
        inputRect.anchorMax = new Vector2(1f, 1f);
        inputRect.pivot = new Vector2(0.5f, 1f);
        inputRect.offsetMin = new Vector2(24f, -320f);
        inputRect.offsetMax = new Vector2(-24f, -264f);

        Button saveButton = CreateButton(
            "SaveButton",
            panelRect,
            "Save & Reconnect",
            new Vector2(24f, 96f),
            new Vector2(210f, 50f),
            new Color(0.11f, 0.55f, 0.40f, 0.96f),
            SaveAndReconnect);
        RectTransform saveRect = saveButton.GetComponent<RectTransform>();
        saveRect.anchorMin = new Vector2(0f, 0f);
        saveRect.anchorMax = new Vector2(0f, 0f);
        saveRect.pivot = new Vector2(0f, 0f);

        Button resetButton = CreateButton(
            "ResetButton",
            panelRect,
            "Use Default",
            new Vector2(248f, 96f),
            new Vector2(170f, 50f),
            new Color(0.32f, 0.40f, 0.50f, 0.96f),
            ResetToDefault);
        RectTransform resetRect = resetButton.GetComponent<RectTransform>();
        resetRect.anchorMin = new Vector2(0f, 0f);
        resetRect.anchorMax = new Vector2(0f, 0f);
        resetRect.pivot = new Vector2(0f, 0f);

        backendStateText = CreateText(
            "BackendStateText",
            panelRect,
            string.Empty,
            17f,
            FontStyles.Bold,
            new Color(0.08f, 0.42f, 0.58f, 1f),
            TextAlignmentOptions.Left);
        backendStateText.rectTransform.anchorMin = new Vector2(0f, 0f);
        backendStateText.rectTransform.anchorMax = new Vector2(1f, 0f);
        backendStateText.rectTransform.pivot = new Vector2(0f, 0f);
        backendStateText.rectTransform.offsetMin = new Vector2(24f, 70f);
        backendStateText.rectTransform.offsetMax = new Vector2(-24f, 132f);

        feedbackText = CreateText(
            "FeedbackText",
            panelRect,
            "Save a new URL when your backend machine IP changes.",
            17f,
            FontStyles.Normal,
            new Color(0.26f, 0.31f, 0.37f, 1f),
            TextAlignmentOptions.TopLeft);
        feedbackText.rectTransform.anchorMin = new Vector2(0f, 0f);
        feedbackText.rectTransform.anchorMax = new Vector2(1f, 0f);
        feedbackText.rectTransform.pivot = new Vector2(0f, 0f);
        feedbackText.rectTransform.offsetMin = new Vector2(24f, 18f);
        feedbackText.rectTransform.offsetMax = new Vector2(-24f, 64f);
    }

    private void OpenPanel()
    {
        overlayObject.SetActive(true);
        overlayObject.transform.SetAsLastSibling();
        RefreshInfo();
    }

    private void ClosePanel()
    {
        overlayObject.SetActive(false);
    }

    private void SaveAndReconnect()
    {
        string rawValue = urlInputField != null ? urlInputField.text : string.Empty;
        if (!TryNormalizeHttpUrl(rawValue, out string normalizedUrl, out string errorMessage))
        {
            SetFeedback(errorMessage, new Color(0.70f, 0.18f, 0.18f, 1f));
            return;
        }

        ApiConfig.SetBaseUrlOverride(normalizedUrl);
        RefreshInfo();

        if (BackendBootstrap.Instance != null)
        {
            BackendBootstrap.Instance.RefreshSession();
            SetFeedback("Saved. Reconnecting backend with the new URL...", new Color(0.10f, 0.42f, 0.58f, 1f));
            return;
        }

        SetFeedback("Saved. Open MainScene to reconnect the backend.", new Color(0.10f, 0.42f, 0.58f, 1f));
    }

    private void ResetToDefault()
    {
        ApiConfig.ClearBaseUrlOverride();
        RefreshInfo();

        if (BackendBootstrap.Instance != null)
        {
            BackendBootstrap.Instance.RefreshSession();
            SetFeedback("Default server restored. Reconnecting backend...", new Color(0.10f, 0.42f, 0.58f, 1f));
            return;
        }

        SetFeedback("Default server restored.", new Color(0.10f, 0.42f, 0.58f, 1f));
    }

    private void RefreshInfo()
    {
        if (urlInputField != null)
        {
            urlInputField.text = ApiConfig.BaseUrl;
        }

        if (activeUrlText != null)
        {
            string sourceLabel = ApiConfig.HasBaseUrlOverride ? "custom override" : "current default";
            activeUrlText.text = "Active URL: " + ApiConfig.BaseUrl + " (" + sourceLabel + ")";
        }

        if (defaultUrlText != null)
        {
            defaultUrlText.text = "Default for this build: " + ApiConfig.DefaultBaseUrl;
        }

        UpdateBackendStateText();
        UpdateOpenButtonState();

        if (BackendBootstrap.Instance == null)
        {
            SetFeedback("Save a new URL when your backend machine IP changes.", new Color(0.26f, 0.31f, 0.37f, 1f));
        }
    }

    private void UpdateBackendStateText()
    {
        if (backendStateText == null)
        {
            return;
        }

        if (BackendBootstrap.Instance == null)
        {
            if (stateBadgeText != null)
            {
                stateBadgeText.text = "Not Ready";
            }

            if (stateBadgeImage != null)
            {
                stateBadgeImage.color = new Color(0.51f, 0.57f, 0.64f, 0.95f);
            }

            backendStateText.text = "Status: backend bootstrap not ready yet.";
            return;
        }

        BackendBootstrap bootstrap = BackendBootstrap.Instance;
        Color stateColor = GetStateColor(bootstrap.ConnectionState);

        if (stateBadgeText != null)
        {
            stateBadgeText.text = bootstrap.ConnectionTitle;
        }

        if (stateBadgeImage != null)
        {
            stateBadgeImage.color = stateColor;
        }

        string detail = string.IsNullOrEmpty(bootstrap.ConnectionDetail)
            ? "Waiting for backend status."
            : bootstrap.ConnectionDetail;
        string sync = string.IsNullOrEmpty(bootstrap.LastSuccessfulSyncTime)
            ? "Last sync: not yet"
            : "Last sync: " + bootstrap.LastSuccessfulSyncTime;
        string source = string.IsNullOrEmpty(bootstrap.LastSuccessfulBaseUrl)
            ? "Connected URL: -"
            : "Connected URL: " + bootstrap.LastSuccessfulBaseUrl;

        backendStateText.text = detail + "\n" + sync + "\n" + source;

        string newStatusMessage = bootstrap.LastStatusMessage;
        if (newStatusMessage != lastObservedStatusMessage)
        {
            lastObservedStatusMessage = newStatusMessage;

            if (bootstrap.ConnectionState == BackendBootstrap.BackendConnectionState.Online)
            {
                SetFeedback("Connected successfully. Backend is live and tourism spots are synced.", stateColor);
            }
            else if (bootstrap.ConnectionState == BackendBootstrap.BackendConnectionState.Degraded)
            {
                SetFeedback("Backend partially connected. App is using local spot fallback.", stateColor);
            }
            else if (bootstrap.ConnectionState == BackendBootstrap.BackendConnectionState.Offline)
            {
                string message = string.IsNullOrEmpty(bootstrap.LastErrorMessage)
                    ? "Backend is offline. Using fallback mode."
                    : "Backend is offline. " + bootstrap.LastErrorMessage;
                SetFeedback(message, stateColor);
            }
            else if (bootstrap.ConnectionState == BackendBootstrap.BackendConnectionState.Connecting)
            {
                SetFeedback(detail, stateColor);
            }
        }
    }

    private void UpdateOpenButtonState()
    {
        if (openButton == null)
        {
            return;
        }

        Image buttonImage = openButton.targetGraphic as Image;
        if (buttonImage == null)
        {
            return;
        }

        if (BackendBootstrap.Instance == null)
        {
            RuntimeGameUiTheme.ApplyButtonChrome(buttonImage, new Color(0.15f, 0.24f, 0.36f, 0.94f));
            return;
        }

        RuntimeGameUiTheme.ApplyButtonChrome(buttonImage, GetStateColor(BackendBootstrap.Instance.ConnectionState));
    }

    private static Color GetStateColor(BackendBootstrap.BackendConnectionState state)
    {
        switch (state)
        {
            case BackendBootstrap.BackendConnectionState.Online:
                return new Color(0.12f, 0.56f, 0.38f, 0.96f);
            case BackendBootstrap.BackendConnectionState.Degraded:
                return new Color(0.84f, 0.56f, 0.16f, 0.96f);
            case BackendBootstrap.BackendConnectionState.Offline:
                return new Color(0.70f, 0.22f, 0.24f, 0.96f);
            case BackendBootstrap.BackendConnectionState.Connecting:
                return new Color(0.16f, 0.46f, 0.72f, 0.96f);
            default:
                return new Color(0.32f, 0.40f, 0.50f, 0.96f);
        }
    }

    private void SetFeedback(string message, Color color)
    {
        if (feedbackText == null)
        {
            return;
        }

        feedbackText.text = message;
        feedbackText.color = color;
    }

    private static bool TryNormalizeHttpUrl(string rawValue, out string normalizedUrl, out string errorMessage)
    {
        normalizedUrl = string.Empty;
        errorMessage = string.Empty;

        string trimmedValue = rawValue == null ? string.Empty : rawValue.Trim();
        if (string.IsNullOrEmpty(trimmedValue))
        {
            errorMessage = "Enter a full backend URL, for example http://192.168.1.23:4000";
            return false;
        }

        if (!Uri.TryCreate(trimmedValue, UriKind.Absolute, out Uri parsedUri))
        {
            errorMessage = "That URL is not valid.";
            return false;
        }

        if (parsedUri.Scheme != Uri.UriSchemeHttp && parsedUri.Scheme != Uri.UriSchemeHttps)
        {
            errorMessage = "Use http:// or https:// in the backend URL.";
            return false;
        }

        if (string.IsNullOrEmpty(parsedUri.Host))
        {
            errorMessage = "The backend URL is missing a host.";
            return false;
        }

        normalizedUrl = parsedUri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
        return true;
    }

    private static TMP_InputField CreateInputField(RectTransform parent)
    {
        GameObject fieldObject = CreateUiObject("UrlInputField", parent);
        Image backgroundImage = fieldObject.AddComponent<Image>();
        backgroundImage.sprite = GetDefaultSprite();
        backgroundImage.color = Color.white;
        ApplyOutline(backgroundImage, new Color(0.18f, 0.28f, 0.36f, 0.12f), new Vector2(1f, -1f));

        TMP_InputField inputField = fieldObject.AddComponent<TMP_InputField>();
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.contentType = TMP_InputField.ContentType.Standard;

        GameObject textAreaObject = CreateUiObject("TextArea", fieldObject.transform);
        RectTransform textAreaRect = textAreaObject.GetComponent<RectTransform>();
        StretchRect(textAreaRect, new Vector2(16f, 10f), new Vector2(-16f, -10f));

        GameObject textObject = CreateUiObject("Text", textAreaRect);
        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = 22f;
        text.color = new Color(0.10f, 0.13f, 0.17f, 1f);
        text.alignment = TextAlignmentOptions.Left;
        StretchRect(text.rectTransform, Vector2.zero, Vector2.zero);

        GameObject placeholderObject = CreateUiObject("Placeholder", textAreaRect);
        TextMeshProUGUI placeholder = placeholderObject.AddComponent<TextMeshProUGUI>();
        placeholder.text = "http://192.168.1.23:4000";
        placeholder.fontSize = 22f;
        placeholder.color = new Color(0.55f, 0.60f, 0.66f, 0.85f);
        placeholder.alignment = TextAlignmentOptions.Left;
        StretchRect(placeholder.rectTransform, Vector2.zero, Vector2.zero);

        if (TMP_Settings.defaultFontAsset != null)
        {
            text.font = TMP_Settings.defaultFontAsset;
            placeholder.font = TMP_Settings.defaultFontAsset;
        }

        inputField.textViewport = textAreaRect;
        inputField.textComponent = text;
        inputField.placeholder = placeholder;

        return inputField;
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
}
