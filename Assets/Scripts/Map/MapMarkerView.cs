using System;
using UnityEngine;
using UnityEngine.UI;

public class MapMarkerView : MonoBehaviour
{
    private static Sprite hitTargetSprite;
    private static Sprite circleSprite;

    private RectTransform rectTransform;
    private Button button;
    private Image buttonImage;
    private Image haloImage;
    private Image ringImage;
    private Image dotImage;
    private Image coreImage;

    private TourismSpot spot;
    private Action<TourismSpot> onClicked;

    public void Initialize()
    {
        rectTransform = gameObject.GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            rectTransform = gameObject.AddComponent<RectTransform>();
        }

        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.sizeDelta = new Vector2(40f, 40f);

        buttonImage = gameObject.AddComponent<Image>();
        buttonImage.sprite = GetHitTargetSprite();
        buttonImage.color = new Color(1f, 1f, 1f, 0.01f);

        button = gameObject.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(HandleClick);

        haloImage = CreateCircle("Halo", new Vector2(0f, 0f), 64f, new Color(1f, 1f, 1f, 0f));
        // Soft drop shadow under the marker
        ringImage = CreateCircle("Ring", new Vector2(0f, -4f), 44f, new Color(0f, 0f, 0f, 0.20f));
        // Main dot
        dotImage = CreateCircle("Dot", new Vector2(0f, 0f), 44f, new Color(0.14f, 0.76f, 0.88f, 1f));
        // Inner white highlight/core
        coreImage = CreateCircle("Core", new Vector2(0f, 0f), 12f, new Color(1f, 1f, 1f, 0.8f));
    }

    public void Bind(TourismSpot value, Action<TourismSpot> clickHandler)
    {
        spot = value;
        onClicked = clickHandler;
        gameObject.name = string.IsNullOrEmpty(value.spotName) ? "SpotMarker" : "SpotMarker_" + value.spotName;
    }

    public void SetAnchoredPosition(Vector2 anchoredPosition)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = anchoredPosition;
        }
    }

    public void SetVisualState(bool isNearby, bool isSelected, bool canOpenAr)
    {
        if (rectTransform == null)
        {
            return;
        }

        float size = isSelected ? 64f : (isNearby ? 56f : 48f);
        rectTransform.sizeDelta = new Vector2(size, size);

        if (haloImage != null)
        {
            // A glowing aura for selected/nearby
            haloImage.rectTransform.sizeDelta = new Vector2(size + 24f, size + 24f);
            haloImage.color = isSelected
                ? new Color(0.98f, 0.82f, 0.22f, 0.4f)
                : isNearby
                    ? new Color(0.32f, 0.86f, 0.58f, 0.3f)
                    : new Color(0f, 0f, 0f, 0f);
        }

        if (ringImage != null)
        {
            // Shadow always stays slightly larger underneath
            ringImage.rectTransform.sizeDelta = new Vector2(size, size);
        }

        if (dotImage != null)
        {
            dotImage.rectTransform.sizeDelta = new Vector2(size * 0.9f, size * 0.9f);
            dotImage.color = isSelected
                ? new Color(0.98f, 0.82f, 0.22f, 1f) // Bright Yellow/Gold for selected
                : canOpenAr
                    ? new Color(0.32f, 0.86f, 0.58f, 1f) // Poké Green for actionable
                    : new Color(0.18f, 0.68f, 0.90f, 1f); // Bright Blue for default
        }

        if (coreImage != null)
        {
            // Just a tiny elegant highlight
            coreImage.rectTransform.sizeDelta = new Vector2(size * 0.25f, size * 0.25f);
            coreImage.color = new Color(1f, 1f, 1f, 0.85f);
        }
    }

    private void HandleClick()
    {
        onClicked?.Invoke(spot);
    }

    private Image CreateCircle(string objectName, Vector2 anchoredPosition, float size, Color color)
    {
        GameObject child = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        child.transform.SetParent(transform, false);

        RectTransform childRect = child.GetComponent<RectTransform>();
        childRect.anchorMin = new Vector2(0.5f, 0.5f);
        childRect.anchorMax = new Vector2(0.5f, 0.5f);
        childRect.pivot = new Vector2(0.5f, 0.5f);
        childRect.anchoredPosition = anchoredPosition;
        childRect.sizeDelta = new Vector2(size, size);

        Image childImage = child.GetComponent<Image>();
        childImage.sprite = GetCircleSprite();
        childImage.color = color;
        childImage.raycastTarget = false;

        return childImage;
    }

    private static Sprite GetHitTargetSprite()
    {
        if (hitTargetSprite == null)
        {
            hitTargetSprite = Sprite.Create(
                Texture2D.whiteTexture,
                new Rect(0f, 0f, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                new Vector2(0.5f, 0.5f));
        }

        return hitTargetSprite;
    }

    private static Sprite GetCircleSprite()
    {
        if (circleSprite == null)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.ARGB32, false);
            texture.name = "MapMarkerCircle";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.45f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = distance <= radius ? 1f : 0f;
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
}
