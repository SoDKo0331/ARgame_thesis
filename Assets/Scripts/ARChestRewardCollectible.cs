using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider))]
public class ARChestRewardCollectible : MonoBehaviour
{
    public bool verboseLogging = true;

    private Camera targetCamera;
    private Action onCollected;
    private Transform visualRoot;
    private Transform labelRoot;
    private Transform showcaseRoot;
    private CanvasGroup canvasGroup;
    private BoxCollider boxCollider;
    private Vector3 basePosition;
    private float spawnTime;
    private bool isCollected;
    private bool usesModelVisual;

    public void Initialize(string rewardName, string previewPrefabKey, Camera camera, Action collectedCallback)
    {
        targetCamera = camera;
        onCollected = collectedCallback;
        basePosition = transform.position;
        spawnTime = Time.time;
        LogInfo("Initialize called. Reward = " + rewardName + ", preview key = " + previewPrefabKey);

        BuildVisuals(
            string.IsNullOrEmpty(rewardName) ? "Reward" : rewardName,
            previewPrefabKey);
    }

    private void Update()
    {
        if (visualRoot == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        float elapsed = Time.time - spawnTime;
        float appear = Mathf.Clamp01(elapsed / 0.28f);
        float easedAppear = 1f - Mathf.Pow(1f - appear, 3f);

        transform.position = basePosition + Vector3.up * (0.06f + Mathf.Sin(elapsed * 2.2f) * 0.035f);
        visualRoot.localScale = Vector3.one * Mathf.Lerp(0.25f, 1f, easedAppear);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = easedAppear;
        }

        if (usesModelVisual)
        {
            if (showcaseRoot != null)
            {
                showcaseRoot.Rotate(0f, Time.deltaTime * 26f, 0f, Space.Self);
            }

            if (labelRoot != null && targetCamera != null)
            {
                Vector3 lookDirection = targetCamera.transform.position - labelRoot.position;
                if (lookDirection.sqrMagnitude > 0.0001f)
                {
                    labelRoot.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                }
            }
        }
        else if (targetCamera != null)
        {
            Vector3 lookDirection = targetCamera.transform.position - transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
            }
        }

#if UNITY_EDITOR
        if (Input.GetMouseButtonDown(0))
        {
            TryCollect(Input.mousePosition);
        }
#endif

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
        {
            TryCollect(Input.GetTouch(0).position);
        }
    }

    private void TryCollect(Vector2 screenPosition)
    {
        if (isCollected)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        Ray ray = targetCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 12f))
        {
            if (hit.collider == boxCollider || hit.collider.transform.IsChildOf(transform))
            {
                LogInfo("Collectible tap detected on " + hit.collider.name);
                Collect();
            }
        }
    }

    private void Collect()
    {
        if (isCollected)
        {
            return;
        }

        isCollected = true;

        if (boxCollider != null)
        {
            boxCollider.enabled = false;
        }

        LogInfo("Collectible collected. Starting collect animation.");
        onCollected?.Invoke();
        StartCoroutine(CollectSequence());
    }

    private IEnumerator CollectSequence()
    {
        float duration = 0.35f;
        float elapsed = 0f;
        Vector3 startScale = visualRoot != null ? visualRoot.localScale : Vector3.one;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (visualRoot != null)
            {
                visualRoot.localScale = startScale * (1f + Mathf.Sin(t * Mathf.PI) * 0.5f);
                visualRoot.Rotate(0f, Time.deltaTime * 900f * (1f - t), 0f, Space.Self);
            }

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f - t;
            }
            
            if (labelRoot != null)
            {
                labelRoot.localScale = Vector3.Lerp(Vector3.one * 0.002f, Vector3.zero, t);
            }

            transform.position += Vector3.up * (Time.deltaTime * 0.5f);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void BuildVisuals(string rewardName, string previewPrefabKey)
    {
        boxCollider = GetComponent<BoxCollider>();

        GameObject rewardPrefab = RewardPreviewModelResolver.ResolvePrefab(
            previewPrefabKey,
            string.Empty,
            rewardName);

        if (rewardPrefab != null)
        {
            LogInfo("3D reward prefab resolved: " + rewardPrefab.name);
            BuildModelVisuals(rewardName, rewardPrefab);
            return;
        }

        LogInfo("No reward prefab found. Falling back to 2D collectible card.");

        boxCollider.center = new Vector3(0f, 0.18f, 0f);
        boxCollider.size = new Vector3(0.42f, 0.52f, 0.18f);

        GameObject canvasObject = new GameObject("CollectibleCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        canvasObject.transform.SetParent(transform, false);
        visualRoot = canvasObject.transform;
        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.worldCamera = targetCamera;
        canvas.sortingOrder = 40;

        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(360f, 420f);
        visualRoot.localScale = Vector3.one * 0.00135f;

        Image cardImage = canvasObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyPanelChrome(cardImage, new Color(0.98f, 0.99f, 0.98f, 0.97f));

        GameObject badgeObject = CreateUiObject("Badge", canvasRect);
        RectTransform badgeRect = badgeObject.GetComponent<RectTransform>();
        badgeRect.anchorMin = new Vector2(0.5f, 1f);
        badgeRect.anchorMax = new Vector2(0.5f, 1f);
        badgeRect.pivot = new Vector2(0.5f, 1f);
        badgeRect.anchoredPosition = new Vector2(0f, -18f);
        badgeRect.sizeDelta = new Vector2(132f, 132f);

        Image badgeImage = badgeObject.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyButtonChrome(badgeImage, new Color(0.18f, 0.70f, 0.54f, 0.96f));

        TMP_Text badgeText = CreateText(
            "BadgeText",
            badgeRect,
            rewardName.Substring(0, 1).ToUpperInvariant(),
            54f,
            FontStyles.Bold,
            Color.white,
            TextAlignmentOptions.Center);
        StretchRect(badgeText.rectTransform, Vector2.zero, Vector2.zero);
        RuntimeGameUiTheme.StyleButtonLabel(badgeText);

        TMP_Text titleText = CreateText(
            "TitleText",
            canvasRect,
            rewardName,
            28f,
            FontStyles.Bold,
            new Color(0.10f, 0.14f, 0.19f, 1f),
            TextAlignmentOptions.Center);
        titleText.enableWordWrapping = true;
        titleText.rectTransform.anchorMin = new Vector2(0f, 1f);
        titleText.rectTransform.anchorMax = new Vector2(1f, 1f);
        titleText.rectTransform.pivot = new Vector2(0.5f, 1f);
        titleText.rectTransform.offsetMin = new Vector2(28f, -230f);
        titleText.rectTransform.offsetMax = new Vector2(-28f, -150f);
        RuntimeGameUiTheme.StyleTitleText(titleText, new Color(0.10f, 0.14f, 0.19f, 1f));

        TMP_Text hintText = CreateText(
            "HintText",
            canvasRect,
            "TAP TO ADD TO COLLECTION",
            18f,
            FontStyles.Bold,
            new Color(0.16f, 0.48f, 0.36f, 1f),
            TextAlignmentOptions.Center);
        hintText.rectTransform.anchorMin = new Vector2(0f, 0f);
        hintText.rectTransform.anchorMax = new Vector2(1f, 0f);
        hintText.rectTransform.pivot = new Vector2(0.5f, 0f);
        hintText.rectTransform.offsetMin = new Vector2(20f, 28f);
        hintText.rectTransform.offsetMax = new Vector2(-20f, 62f);
        RuntimeGameUiTheme.StyleAccentText(hintText, new Color(0.16f, 0.48f, 0.36f, 1f));
    }

    private void BuildModelVisuals(string rewardName, GameObject rewardPrefab)
    {
        usesModelVisual = true;

        GameObject visualRootObject = new GameObject("CollectibleModelRoot");
        visualRootObject.transform.SetParent(transform, false);
        visualRoot = visualRootObject.transform;

        GameObject showcaseObject = new GameObject("Showcase");
        showcaseObject.transform.SetParent(visualRoot, false);
        showcaseRoot = showcaseObject.transform;

        GameObject instance = Instantiate(rewardPrefab, showcaseRoot);
        instance.name = "RewardModel";
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one;

        Bounds modelBounds = CalculateLocalBounds(showcaseRoot);
        float targetHeight = 0.38f;
        if (modelBounds.size.y > 0.001f)
        {
            float uniformScale = Mathf.Clamp(targetHeight / modelBounds.size.y, 0.05f, 0.88f);
            showcaseRoot.localScale = Vector3.one * uniformScale;
            modelBounds = CalculateLocalBounds(showcaseRoot);
        }
        else
        {
            showcaseRoot.localScale = Vector3.one * 0.24f;
            modelBounds = CalculateLocalBounds(showcaseRoot);
        }

        Vector3 centeredOffset = -modelBounds.center + new Vector3(0f, modelBounds.extents.y, 0f);
        instance.transform.localPosition += centeredOffset;
        modelBounds = CalculateLocalBounds(showcaseRoot);

        boxCollider.center = modelBounds.center;
        boxCollider.size = Vector3.Max(modelBounds.size + new Vector3(0.12f, 0.12f, 0.12f), new Vector3(0.3f, 0.3f, 0.3f));

        CreateCollectibleLabel(rewardName, modelBounds);
        CreateProceduralVFX(modelBounds);
        LogInfo("3D collectible built. Collider size = " + boxCollider.size + ", center = " + boxCollider.center);
    }

    private void CreateProceduralVFX(Bounds bounds)
    {
        // Magic glowing ring on floor
        GameObject ringObj = new GameObject("MagicRing");
        ringObj.transform.SetParent(visualRoot, false);
        ringObj.transform.localPosition = new Vector3(0f, -0.02f, 0f);
        ringObj.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        
        LineRenderer ring = ringObj.AddComponent<LineRenderer>();
        ring.useWorldSpace = false;
        ring.loop = true;
        ring.positionCount = 36;
        float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) * 1.6f + 0.18f;
        for (int i = 0; i < 36; i++)
        {
            float angle = i * Mathf.PI * 2f / 36f;
            ring.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f));
        }
        ring.startWidth = 0.025f;
        ring.endWidth = 0.025f;
        
        Material glowMat = new Material(Shader.Find("Mobile/Particles/Additive"));
        ring.material = glowMat;
        ring.startColor = new Color(0.18f, 0.88f, 0.66f, 0.7f);
        ring.endColor = new Color(0.1f, 0.6f, 0.9f, 0.7f);

        // God Ray Pillar
        GameObject rayObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rayObj.name = "GodRayPillar";
        rayObj.transform.SetParent(visualRoot, false);
        Destroy(rayObj.GetComponent<Collider>());
        rayObj.transform.localScale = new Vector3(radius * 2f, 8f, radius * 2f);
        rayObj.transform.localPosition = new Vector3(0f, 8f, 0f);
        
        Material rayMat = new Material(Shader.Find("Mobile/Particles/Additive"));
        rayMat.color = new Color(0.1f, 0.8f, 0.95f, 0.12f);
        rayObj.GetComponent<Renderer>().material = rayMat;
    }

    private void CreateCollectibleLabel(string rewardName, Bounds modelBounds)
    {
        GameObject canvasObj = new GameObject("CollectibleLabelCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        canvasObj.transform.SetParent(transform, false);
        labelRoot = canvasObj.transform;
        labelRoot.localPosition = new Vector3(0f, modelBounds.max.y + 0.18f, 0f);
        labelRoot.localScale = Vector3.one * 0.002f;

        Canvas canvas = canvasObj.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = canvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(300f, 90f);

        Image bg = canvasObj.AddComponent<Image>();
        RuntimeGameUiTheme.ApplyPanelChrome(bg, new Color(0.02f, 0.05f, 0.1f, 0.80f));
        
        var outline = canvasObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = new Color(0.18f, 0.68f, 0.88f, 0.5f);
        outline.effectDistance = new Vector2(1f, -1f);

        GameObject titleObj = new GameObject("TitleText", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(canvasRect, false);
        TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = rewardName;
        titleText.fontSize = 28f;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(0.95f, 0.98f, 1f, 1f);
        titleText.fontStyle = FontStyles.Bold;
        
        var shadow = titleObj.AddComponent<UnityEngine.UI.Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.8f);
        shadow.effectDistance = new Vector2(0f, -2f);

        RectTransform titleRect = titleObj.GetComponent<RectTransform>();
        StretchRect(titleRect, new Vector2(10f, 24f), new Vector2(-10f, -6f));

        GameObject hintObj = new GameObject("HintText", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintObj.transform.SetParent(canvasRect, false);
        TextMeshProUGUI hintText = hintObj.GetComponent<TextMeshProUGUI>();
        hintText.text = "TAP TO COLLECT";
        hintText.fontSize = 15f;
        hintText.alignment = TextAlignmentOptions.Center;
        hintText.color = new Color(0.5f, 0.9f, 0.7f, 1f);
        hintText.fontStyle = FontStyles.Bold;

        RectTransform hintRect = hintObj.GetComponent<RectTransform>();
        StretchRect(hintRect, new Vector2(10f, 6f), new Vector2(-10f, -54f));
    }

    private static Bounds CalculateLocalBounds(Transform root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            return new Bounds(Vector3.zero, new Vector3(0.25f, 0.25f, 0.25f));
        }

        Bounds bounds = TransformWorldBoundsToLocal(root, renderers[0].bounds);
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(TransformWorldBoundsToLocal(root, renderers[i].bounds));
        }

        return bounds;
    }

    private static Bounds TransformWorldBoundsToLocal(Transform root, Bounds worldBounds)
    {
        Vector3 center = root.InverseTransformPoint(worldBounds.center);
        Vector3 extents = worldBounds.extents;

        Vector3[] corners =
        {
            center + root.InverseTransformVector(new Vector3( extents.x,  extents.y,  extents.z)),
            center + root.InverseTransformVector(new Vector3( extents.x,  extents.y, -extents.z)),
            center + root.InverseTransformVector(new Vector3( extents.x, -extents.y,  extents.z)),
            center + root.InverseTransformVector(new Vector3( extents.x, -extents.y, -extents.z)),
            center + root.InverseTransformVector(new Vector3(-extents.x,  extents.y,  extents.z)),
            center + root.InverseTransformVector(new Vector3(-extents.x,  extents.y, -extents.z)),
            center + root.InverseTransformVector(new Vector3(-extents.x, -extents.y,  extents.z)),
            center + root.InverseTransformVector(new Vector3(-extents.x, -extents.y, -extents.z))
        };

        Bounds localBounds = new Bounds(corners[0], Vector3.zero);
        for (int i = 1; i < corners.Length; i++)
        {
            localBounds.Encapsulate(corners[i]);
        }

        return localBounds;
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

    private void LogInfo(string message)
    {
        if (!verboseLogging)
        {
            return;
        }

        Debug.Log("[ARChestRewardCollectible] " + message, this);
    }
}
