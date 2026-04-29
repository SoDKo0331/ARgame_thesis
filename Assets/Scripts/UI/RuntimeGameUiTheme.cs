using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class RuntimeGameUiTheme
{
    private static Sprite defaultSprite;
    private static Sprite pillSprite;
    private static Sprite roundedRectSprite;

    public static void ApplyButtonChrome(Image image, Color bodyColor)
    {
        if (image == null) return;

        image.sprite = GetPillSprite();
        image.type = Image.Type.Sliced;
        image.color = bodyColor; // vibrant solid color

        ApplyShadow(image, new Color(0f, 0f, 0f, 0.15f), new Vector2(0f, -4f));

        // Subtle top highlight for 3D pill look
        Image topHighlight = EnsureLayer(image.transform, "TopHighlight");
        topHighlight.sprite = GetPillSprite();
        topHighlight.type = Image.Type.Sliced;
        Stretch(topHighlight.rectTransform, new Vector2(4f, -4f), new Vector2(-4f, -4f)); // inset slightly
        topHighlight.color = new Color(1f, 1f, 1f, 0.15f);
        topHighlight.raycastTarget = false;

        // Subtle bottom shade
        Image bottomShade = EnsureLayer(image.transform, "BottomShade");
        bottomShade.sprite = GetPillSprite();
        bottomShade.type = Image.Type.Sliced;
        bottomShade.rectTransform.anchorMin = new Vector2(0f, 0f);
        bottomShade.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        bottomShade.rectTransform.offsetMin = new Vector2(0f, 0f);
        bottomShade.rectTransform.offsetMax = new Vector2(0f, 0f);
        bottomShade.color = new Color(0f, 0f, 0f, 0.1f);
        bottomShade.raycastTarget = false;
    }

    public static void ApplyPanelChrome(Image image, Color fillColor)
    {
        if (image == null) return;

        image.sprite = GetRoundedRectSprite();
        image.type = Image.Type.Sliced;
        image.color = fillColor;

        // Clean soft shadow instead of neon glow
        ApplyShadow(image, new Color(0.05f, 0.10f, 0.15f, 0.12f), new Vector2(0f, -6f));
        ApplyOutline(image, new Color(0.9f, 0.9f, 0.9f, 1f), new Vector2(1f, -1f));

        // Ensure clean slate, hide legacy neon bands
        HideLayer(image.transform, "TopBand");
        HideLayer(image.transform, "BottomBand");
        HideLayer(image.transform, "PanelGlow");
    }

    public static void ApplyCardChrome(Image image, Color fillColor, Color accentColor)
    {
        if (image == null) return;
        ApplyPanelChrome(image, fillColor);

        Image sideAccent = EnsureLayer(image.transform, "SideAccent");
        sideAccent.sprite = GetDefaultSprite();
        sideAccent.rectTransform.anchorMin = new Vector2(0f, 0f);
        sideAccent.rectTransform.anchorMax = new Vector2(0f, 1f);
        sideAccent.rectTransform.pivot = new Vector2(0f, 0.5f);
        sideAccent.rectTransform.offsetMin = new Vector2(16f, 16f);
        sideAccent.rectTransform.offsetMax = new Vector2(24f, -16f); // Thicker, cleaner accent line
        sideAccent.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.9f); // Solid bright color
        sideAccent.raycastTarget = false;
    }

    public static void StyleButtonLabel(TMP_Text text)
    {
        if (text == null) return;

        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 1f; // less spaced out, bolder
        text.color = new Color(1f, 1f, 1f, 0.95f);

        ApplyShadow(text, new Color(0.05f, 0.15f, 0.10f, 0.20f), new Vector2(0f, -1.5f));
        
        // Remove outline if it exists
        var outline = text.GetComponent<Outline>();
        if (outline != null) Object.Destroy(outline);
    }

    public static void StyleTitleText(TMP_Text text, Color color)
    {
        if (text == null) return;

        text.color = new Color(color.r, color.g, color.b, 0.95f);
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 0f;
        ApplyShadow(text, new Color(0f, 0f, 0f, 0.08f), new Vector2(0f, -1.5f));
    }

    public static void StyleAccentText(TMP_Text text, Color color)
    {
        if (text == null) return;

        text.color = new Color(color.r, color.g, color.b, 1f);
        text.fontStyle = FontStyles.Bold;
        text.characterSpacing = 0.5f;
        ApplyShadow(text, new Color(0f, 0f, 0f, 0.05f), new Vector2(0f, -1f));
    }

    private static void HideLayer(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        if (existing != null) existing.gameObject.SetActive(false);
    }

    private static Image EnsureLayer(Transform parent, string objectName)
    {
        Transform existing = parent.Find(objectName);
        GameObject layerObject;

        if (existing != null)
        {
            layerObject = existing.gameObject;
            layerObject.SetActive(true);
        }
        else
        {
            layerObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
            layerObject.transform.SetParent(parent, false);
            layerObject.transform.SetAsFirstSibling();
        }

        Image image = layerObject.GetComponent<Image>();
        image.sprite = GetDefaultSprite();
        return image;
    }

    private static void ApplyShadow(Graphic graphic, Color color, Vector2 effectDistance)
    {
        Shadow shadow = graphic.GetComponent<Shadow>();
        if (shadow == null) shadow = graphic.gameObject.AddComponent<Shadow>();

        shadow.effectColor = color;
        shadow.effectDistance = effectDistance;
        shadow.useGraphicAlpha = true;
    }

    private static void ApplyOutline(Graphic graphic, Color color, Vector2 effectDistance)
    {
        Outline outline = graphic.GetComponent<Outline>();
        if (outline == null) outline = graphic.gameObject.AddComponent<Outline>();

        outline.effectColor = color;
        outline.effectDistance = effectDistance;
        outline.useGraphicAlpha = true;
    }

    private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
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
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f));
        }
        return defaultSprite;
    }

    private static Sprite GetPillSprite()
    {
        if (pillSprite == null) pillSprite = CreateRoundedRectSprite("UI_Pill", 132, 132, 64);
        return pillSprite;
    }

    private static Sprite GetRoundedRectSprite()
    {
        if (roundedRectSprite == null) roundedRectSprite = CreateRoundedRectSprite("UI_RoundedRect", 68, 68, 32);
        return roundedRectSprite;
    }

    private static Sprite CreateRoundedRectSprite(string name, int width, int height, int radius)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.name = name;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = 0, dy = 0;

                if (x < radius) dx = radius - x;
                else if (x >= width - radius) dx = x - (width - radius) + 1;

                if (y < radius) dy = radius - y;
                else if (y >= height - radius) dy = y - (height - radius) + 1;

                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - dist + 1f);

                if (dx == 0 && dy == 0) alpha = 1f;

                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();

        // Safe 9-slice borders, leaves a small center pixel
        Vector4 border = new Vector4(radius, radius, radius, radius);
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }
}
