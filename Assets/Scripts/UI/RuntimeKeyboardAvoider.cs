using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class RuntimeKeyboardAvoider : MonoBehaviour
{
    [SerializeField] private float extraPadding = 24f;
    [SerializeField] private float fallbackScreenRatio = 0.32f;

    private RectTransform rectTransform;
    private Vector2 baseOffsetMin;
    private Vector2 baseOffsetMax;
    private bool hasCachedOffsets;

    public void Configure(float bottomPadding)
    {
        extraPadding = bottomPadding;
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        CacheBaseOffsets();
    }

    private void OnEnable()
    {
        CacheBaseOffsets();
        ApplyKeyboardOffset(0f);
    }

    private void LateUpdate()
    {
        float keyboardHeight = GetKeyboardHeight();
        ApplyKeyboardOffset(keyboardHeight);
    }

    private void CacheBaseOffsets()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }

        baseOffsetMin = rectTransform.offsetMin;
        baseOffsetMax = rectTransform.offsetMax;
        hasCachedOffsets = true;
    }

    private void ApplyKeyboardOffset(float keyboardHeight)
    {
        if (!hasCachedOffsets || rectTransform == null)
        {
            return;
        }

        float shift = keyboardHeight > 0f ? keyboardHeight + extraPadding : 0f;
        rectTransform.offsetMin = baseOffsetMin + new Vector2(0f, shift);
        rectTransform.offsetMax = baseOffsetMax + new Vector2(0f, shift);
    }

    private float GetKeyboardHeight()
    {
        if (!Application.isMobilePlatform)
        {
            return 0f;
        }

        Rect keyboardArea = TouchScreenKeyboard.area;
        if (keyboardArea.height > 0f)
        {
            return keyboardArea.height;
        }

        if (TouchScreenKeyboard.visible)
        {
            return Screen.height * fallbackScreenRatio;
        }

        return 0f;
    }
}
