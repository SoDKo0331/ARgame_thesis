using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StartupLoadingScreenController : MonoBehaviour
{
    private const float MinVisibleSeconds = 2.2f;
    private const float FallbackFinishSeconds = 6.5f;

    private RectTransform rootRect;
    private RectTransform safeAreaRect;
    private CanvasGroup canvasGroup;
    private TMP_Text percentText;
    private TMP_Text detailText;
    private RectTransform progressFillRect;
    private float displayedProgress;
    private float startTime;

    private void Awake()
    {
        BuildUi();
    }

    private void Start()
    {
        startTime = Time.unscaledTime;
        StartCoroutine(RunLoadingSequence());
    }

    private IEnumerator RunLoadingSequence()
    {
        while (!ShouldFinish())
        {
            UpdateProgressVisuals(GetTargetProgress(), GetDetailMessage());
            yield return null;
        }

        while (displayedProgress < 0.999f)
        {
            UpdateProgressVisuals(1f, "Adventure is ready");
            yield return null;
        }

        yield return new WaitForSecondsRealtime(0.22f);

        float fadeDuration = 0.35f;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - t;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    private bool ShouldFinish()
    {
        if (Time.unscaledTime - startTime < MinVisibleSeconds)
        {
            return false;
        }

        if (Time.unscaledTime - startTime >= FallbackFinishSeconds)
        {
            return true;
        }

        bool backendReady = BackendBootstrap.Instance != null &&
            BackendBootstrap.Instance.ConnectionState != BackendBootstrap.BackendConnectionState.Connecting &&
            !BackendBootstrap.Instance.IsBusy;

        bool mapReady = FindObjectOfType<MapScreenController>() != null;
        return backendReady && mapReady;
    }

    private float GetTargetProgress()
    {
        float elapsed = Time.unscaledTime - startTime;
        float baseline = Mathf.Lerp(0.05f, 0.48f, Mathf.Clamp01(elapsed / MinVisibleSeconds));
        float target = baseline;

        if (BackendBootstrap.Instance != null)
        {
            switch (BackendBootstrap.Instance.ConnectionState)
            {
                case BackendBootstrap.BackendConnectionState.Connecting:
                    target = Mathf.Max(target, 0.62f);
                    break;
                case BackendBootstrap.BackendConnectionState.Online:
                    target = Mathf.Max(target, 0.86f);
                    break;
                case BackendBootstrap.BackendConnectionState.Degraded:
                case BackendBootstrap.BackendConnectionState.Offline:
                    target = Mathf.Max(target, 0.82f);
                    break;
            }
        }

        if (FindObjectOfType<MapScreenController>() != null)
        {
            target = Mathf.Max(target, 0.94f);
        }

        if (elapsed >= FallbackFinishSeconds)
        {
            target = 1f;
        }

        return Mathf.Clamp01(target);
    }

    private string GetDetailMessage()
    {
        if (BackendBootstrap.Instance != null && !string.IsNullOrEmpty(BackendBootstrap.Instance.ConnectionDetail))
        {
            return BackendBootstrap.Instance.ConnectionDetail;
        }

        return "Preparing nomad adventure systems...";
    }

    private void UpdateProgressVisuals(float targetProgress, string detail)
    {
        displayedProgress = Mathf.MoveTowards(displayedProgress, targetProgress, Time.unscaledDeltaTime * 0.55f);

        if (progressFillRect != null)
        {
            progressFillRect.anchorMax = new Vector2(displayedProgress, 1f);
        }

        if (percentText != null)
        {
            percentText.text = Mathf.RoundToInt(displayedProgress * 100f).ToString("00") + "%";
        }

        if (detailText != null)
        {
            detailText.text = detail;
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
        rootRect.SetAsLastSibling();

        canvasGroup = gameObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        GameObject backgroundObject = CreateUiObject("Background", rootRect);
        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        StretchRect(backgroundRect, Vector2.zero, Vector2.zero);

        Image dimImage = backgroundObject.AddComponent<Image>();
        dimImage.color = new Color(0.04f, 0.08f, 0.09f, 1f);

        Texture2D splashTexture = Resources.Load<Texture2D>("loadingscreen");
        if (splashTexture != null)
        {
            GameObject splashObject = CreateUiObject("SplashImage", backgroundRect);
            RectTransform splashRect = splashObject.GetComponent<RectTransform>();
            StretchRect(splashRect, Vector2.zero, Vector2.zero);

            RawImage splashImage = splashObject.AddComponent<RawImage>();
            splashImage.texture = splashTexture;
            splashImage.color = Color.white;

            AspectRatioFitter aspectFitter = splashObject.AddComponent<AspectRatioFitter>();
            aspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            aspectFitter.aspectRatio = splashTexture.width > 0 && splashTexture.height > 0
                ? (float)splashTexture.width / splashTexture.height
                : 0.5625f;
        }

        GameObject topFadeObject = CreateUiObject("TopFade", backgroundRect);
        RectTransform topFadeRect = topFadeObject.GetComponent<RectTransform>();
        topFadeRect.anchorMin = new Vector2(0f, 1f);
        topFadeRect.anchorMax = new Vector2(1f, 1f);
        topFadeRect.pivot = new Vector2(0.5f, 1f);
        topFadeRect.offsetMin = new Vector2(0f, -360f);
        topFadeRect.offsetMax = new Vector2(0f, 0f);
        Image topFadeImage = topFadeObject.AddComponent<Image>();
        topFadeImage.color = new Color(0.04f, 0.10f, 0.10f, 0.42f);

        GameObject safeAreaObject = CreateUiObject("SafeArea", rootRect);
        safeAreaRect = safeAreaObject.GetComponent<RectTransform>();
        RuntimeSafeAreaFitter safeAreaFitter = safeAreaObject.AddComponent<RuntimeSafeAreaFitter>();
        safeAreaFitter.Configure(new Vector2(32f, 32f), new Vector2(32f, 32f));

        GameObject headerObject = CreateUiObject("LoadingHeader", safeAreaRect);
        RectTransform headerRect = headerObject.GetComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0f, 1f);
        headerRect.anchorMax = new Vector2(1f, 1f);
        headerRect.pivot = new Vector2(0.5f, 1f);
        headerRect.offsetMin = new Vector2(24f, -260f);
        headerRect.offsetMax = new Vector2(-24f, -28f);

        TMP_Text loadingText = CreateText(
            "LoadingText",
            headerRect,
            "LOADING",
            34f,
            FontStyles.Bold,
            new Color(0.98f, 0.99f, 0.98f, 1f),
            TextAlignmentOptions.TopLeft);
        loadingText.characterSpacing = 18f;
        loadingText.rectTransform.anchorMin = new Vector2(0f, 1f);
        loadingText.rectTransform.anchorMax = new Vector2(0.7f, 1f);
        loadingText.rectTransform.pivot = new Vector2(0f, 1f);
        loadingText.rectTransform.offsetMin = new Vector2(0f, -52f);
        loadingText.rectTransform.offsetMax = new Vector2(0f, 0f);

        percentText = CreateText(
            "PercentText",
            headerRect,
            "00%",
            30f,
            FontStyles.Bold,
            new Color(0.82f, 0.97f, 0.90f, 1f),
            TextAlignmentOptions.TopRight);
        percentText.characterSpacing = 10f;
        percentText.rectTransform.anchorMin = new Vector2(0.55f, 1f);
        percentText.rectTransform.anchorMax = new Vector2(1f, 1f);
        percentText.rectTransform.pivot = new Vector2(1f, 1f);
        percentText.rectTransform.offsetMin = new Vector2(0f, -52f);
        percentText.rectTransform.offsetMax = new Vector2(0f, 0f);

        GameObject progressTrackObject = CreateUiObject("ProgressTrack", headerRect);
        RectTransform progressTrackRect = progressTrackObject.GetComponent<RectTransform>();
        progressTrackRect.anchorMin = new Vector2(0f, 1f);
        progressTrackRect.anchorMax = new Vector2(1f, 1f);
        progressTrackRect.pivot = new Vector2(0.5f, 1f);
        progressTrackRect.offsetMin = new Vector2(0f, -112f);
        progressTrackRect.offsetMax = new Vector2(0f, -96f);

        Image progressTrackImage = progressTrackObject.AddComponent<Image>();
        progressTrackImage.color = new Color(1f, 1f, 1f, 0.24f);

        GameObject progressFillObject = CreateUiObject("ProgressFill", progressTrackRect);
        progressFillRect = progressFillObject.GetComponent<RectTransform>();
        progressFillRect.anchorMin = new Vector2(0f, 0f);
        progressFillRect.anchorMax = new Vector2(0f, 1f);
        progressFillRect.pivot = new Vector2(0f, 0.5f);
        progressFillRect.offsetMin = Vector2.zero;
        progressFillRect.offsetMax = Vector2.zero;

        Image progressFillImage = progressFillObject.AddComponent<Image>();
        progressFillImage.color = new Color(0.58f, 0.92f, 0.78f, 1f);
        ApplyShadow(progressFillImage, new Color(0.58f, 0.92f, 0.78f, 0.45f), new Vector2(0f, 0f));

        detailText = CreateText(
            "DetailText",
            headerRect,
            "Preparing nomad adventure systems...",
            20f,
            FontStyles.Normal,
            new Color(0.94f, 0.98f, 0.96f, 0.92f),
            TextAlignmentOptions.TopLeft);
        detailText.rectTransform.anchorMin = new Vector2(0f, 1f);
        detailText.rectTransform.anchorMax = new Vector2(1f, 1f);
        detailText.rectTransform.pivot = new Vector2(0f, 1f);
        detailText.rectTransform.offsetMin = new Vector2(0f, -184f);
        detailText.rectTransform.offsetMax = new Vector2(0f, -128f);
    }

    private static GameObject CreateUiObject(string objectName, Transform parent)
    {
        GameObject gameObject = new GameObject(objectName, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        return gameObject;
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

    private static void ApplyShadow(Graphic graphic, Color effectColor, Vector2 distance)
    {
        if (graphic == null)
        {
            return;
        }

        Shadow shadow = graphic.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = graphic.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = effectColor;
        shadow.effectDistance = distance;
    }
}
