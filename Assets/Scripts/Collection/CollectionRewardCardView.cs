using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CollectionRewardCardView : MonoBehaviour
{
    private static Sprite defaultSprite;

    private RectTransform rectTransform;
    private Button button;
    private Image backgroundImage;
    private RawImage thumbnailImage;
    private Image thumbnailPlaceholder;
    private TMP_Text placeholderText;
    private TMP_Text nameText;
    private TMP_Text descriptionText;
    private TMP_Text acquiredDateText;

    private CollectedRewardItem item;
    private Action<CollectedRewardItem> onSelected;
    private string currentImageUrl = string.Empty;
    private Coroutine thumbnailRoutine;

    public void Initialize()
    {
        rectTransform = gameObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0.5f, 1f);
        rectTransform.sizeDelta = new Vector2(0f, 142f);

        backgroundImage = gameObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyCardChrome(
            backgroundImage,
            new Color(0.98f, 0.99f, 0.98f, 0.96f),
            new Color(0.15f, 0.60f, 0.52f, 1f));

        button = gameObject.AddComponent<Button>();
        button.targetGraphic = backgroundImage;
        button.onClick.AddListener(HandleClicked);

        RectTransform thumbnailFrame = CreateFrame("ThumbnailFrame", new Vector2(12f, -12f), new Vector2(104f, 104f));
        Image frameImage = thumbnailFrame.gameObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyPanelChrome(frameImage, new Color(0.88f, 0.92f, 0.97f, 1f));

        GameObject thumbnailObject = CreateUiObject("Thumbnail", thumbnailFrame);
        RectTransform thumbnailRect = thumbnailObject.GetComponent<RectTransform>();
        StretchRect(thumbnailRect, new Vector2(6f, 6f), new Vector2(-6f, -6f));

        thumbnailImage = thumbnailObject.AddComponent<RawImage>();
        thumbnailImage.color = Color.white;
        thumbnailImage.gameObject.SetActive(false);

        GameObject placeholderObject = CreateUiObject("Placeholder", thumbnailFrame);
        RectTransform placeholderRect = placeholderObject.GetComponent<RectTransform>();
        StretchRect(placeholderRect, new Vector2(6f, 6f), new Vector2(-6f, -6f));

        thumbnailPlaceholder = placeholderObject.AddComponent<Image>();
        thumbnailPlaceholder.sprite = GetDefaultSprite();
        thumbnailPlaceholder.color = new Color(0.21f, 0.44f, 0.79f, 0.9f);

        placeholderText = CreateText(
            "PlaceholderText",
            placeholderRect,
            "AR",
            24f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(placeholderText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f));
        RuntimeGameUiTheme.StyleButtonLabel(placeholderText);

        nameText = CreateText(
            "NameText",
            rectTransform,
            "Reward Name",
            24f,
            FontStyles.Bold,
            new Color(0.08f, 0.12f, 0.18f, 1f),
            TextAlignmentOptions.TopLeft);
        ConfigureTextRect(nameText.rectTransform, new Vector2(128f, -14f), new Vector2(-16f, -12f), 40f);
        RuntimeGameUiTheme.StyleTitleText(nameText, new Color(0.08f, 0.12f, 0.18f, 1f));

        descriptionText = CreateText(
            "DescriptionText",
            rectTransform,
            "Reward description",
            18f,
            FontStyles.Normal,
            new Color(0.28f, 0.33f, 0.39f, 1f),
            TextAlignmentOptions.TopLeft);
        ConfigureTextRect(descriptionText.rectTransform, new Vector2(128f, -56f), new Vector2(-16f, -46f), 44f);

        acquiredDateText = CreateText(
            "AcquiredDateText",
            rectTransform,
            "Acquired: -",
            16f,
            FontStyles.Bold,
            new Color(0.07f, 0.43f, 0.59f, 1f),
            TextAlignmentOptions.BottomLeft);
        acquiredDateText.rectTransform.anchorMin = new Vector2(0f, 0f);
        acquiredDateText.rectTransform.anchorMax = new Vector2(1f, 0f);
        acquiredDateText.rectTransform.pivot = new Vector2(0f, 0f);
        acquiredDateText.rectTransform.offsetMin = new Vector2(128f, 12f);
        acquiredDateText.rectTransform.offsetMax = new Vector2(-16f, 34f);
        RuntimeGameUiTheme.StyleAccentText(acquiredDateText, new Color(0.07f, 0.43f, 0.59f, 1f));
    }

    public void Bind(
        CollectedRewardItem value,
        Action<CollectedRewardItem> selectionHandler,
        RemoteTextureCache textureCache,
        MonoBehaviour coroutineHost)
    {
        item = value;
        onSelected = selectionHandler;
        currentImageUrl = value != null ? value.rewardImageUrl : string.Empty;

        nameText.text = value != null ? value.rewardName : "Unknown Reward";
        descriptionText.text = value != null ? value.GetShortDescription() : "No description available yet.";
        acquiredDateText.text = value != null ? "Acquired: " + value.claimedAtDisplay : "Acquired: -";
        gameObject.name = value != null && !string.IsNullOrEmpty(value.rewardName)
            ? "RewardCard_" + value.rewardName
            : "RewardCard";

        SetThumbnail(null, value != null ? value.rewardName : string.Empty);

        if (thumbnailRoutine != null)
        {
            coroutineHost.StopCoroutine(thumbnailRoutine);
            thumbnailRoutine = null;
        }

        if (value == null || string.IsNullOrEmpty(value.rewardImageUrl) || textureCache == null || coroutineHost == null)
        {
            return;
        }

        thumbnailRoutine = coroutineHost.StartCoroutine(LoadThumbnail(textureCache, value.rewardImageUrl));
    }

    private IEnumerator LoadThumbnail(RemoteTextureCache textureCache, string imageUrl)
    {
        Texture2D loadedTexture = null;
        string errorMessage = null;

        yield return textureCache.LoadTexture(
            imageUrl,
            texture => loadedTexture = texture,
            error => errorMessage = error);

        thumbnailRoutine = null;

        if (currentImageUrl != imageUrl)
        {
            yield break;
        }

        if (loadedTexture != null)
        {
            SetThumbnail(loadedTexture, item != null ? item.rewardName : string.Empty);
            yield break;
        }

        if (!string.IsNullOrEmpty(errorMessage))
        {
            Debug.LogWarning("Reward thumbnail failed: " + errorMessage);
        }
    }

    private void SetThumbnail(Texture texture, string rewardName)
    {
        bool hasTexture = texture != null;
        thumbnailImage.texture = texture;
        thumbnailImage.gameObject.SetActive(hasTexture);
        thumbnailPlaceholder.gameObject.SetActive(!hasTexture);

        string placeholderLabel = !string.IsNullOrEmpty(rewardName)
            ? rewardName.Substring(0, 1).ToUpperInvariant()
            : "R";
        placeholderText.text = placeholderLabel;
    }

    private void HandleClicked()
    {
        onSelected?.Invoke(item);
    }

    private RectTransform CreateFrame(string objectName, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject frameObject = CreateUiObject(objectName, rectTransform);
        RectTransform frameRect = frameObject.GetComponent<RectTransform>();
        frameRect.anchorMin = new Vector2(0f, 1f);
        frameRect.anchorMax = new Vector2(0f, 1f);
        frameRect.pivot = new Vector2(0f, 1f);
        frameRect.anchoredPosition = offsetMin;
        frameRect.sizeDelta = offsetMax;
        return frameRect;
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

    private static void ConfigureTextRect(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax, float height)
    {
        rectTransform.anchorMin = new Vector2(0f, 1f);
        rectTransform.anchorMax = new Vector2(1f, 1f);
        rectTransform.pivot = new Vector2(0f, 1f);
        rectTransform.offsetMin = new Vector2(offsetMin.x, -height + offsetMin.y);
        rectTransform.offsetMax = new Vector2(offsetMax.x, offsetMax.y);
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
