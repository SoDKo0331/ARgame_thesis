using UnityEngine;
using UnityEngine.EventSystems;

public class CollectedRewardPreviewInteraction : MonoBehaviour
{
    public float rotationSpeed = 0.2f;
    public float pinchScaleSpeed = 0.002f;
    public float minScale = 0.3f;
    public float maxScale = 2.5f;

    private Transform targetRoot;
    private float baseScale = 1f;

    public void Initialize(Transform previewRootTransform)
    {
        targetRoot = previewRootTransform;
        if (targetRoot != null)
        {
            baseScale = targetRoot.localScale.x;
        }
    }

    private void Update()
    {
        if (targetRoot == null)
        {
            return;
        }

#if UNITY_EDITOR
        HandleMouseRotation();
#endif

        HandleTouchRotation();
        HandleTouchScaling();
    }

    private void HandleTouchRotation()
    {
        if (Input.touchCount != 1)
        {
            return;
        }

        Touch touch = Input.GetTouch(0);
        if (touch.phase != TouchPhase.Moved)
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(touch.fingerId))
        {
            return;
        }

        float rotationDelta = -touch.deltaPosition.x * rotationSpeed;
        targetRoot.Rotate(Vector3.up, rotationDelta, Space.World);
    }

    private void HandleTouchScaling()
    {
        if (Input.touchCount < 2)
        {
            return;
        }

        Touch touch0 = Input.GetTouch(0);
        Touch touch1 = Input.GetTouch(1);

        if (EventSystem.current != null &&
            (EventSystem.current.IsPointerOverGameObject(touch0.fingerId) ||
             EventSystem.current.IsPointerOverGameObject(touch1.fingerId)))
        {
            return;
        }

        Vector2 previousTouch0 = touch0.position - touch0.deltaPosition;
        Vector2 previousTouch1 = touch1.position - touch1.deltaPosition;

        float previousDistance = Vector2.Distance(previousTouch0, previousTouch1);
        float currentDistance = Vector2.Distance(touch0.position, touch1.position);
        float pinchDelta = currentDistance - previousDistance;

        float nextScale = Mathf.Clamp(targetRoot.localScale.x + pinchDelta * pinchScaleSpeed, baseScale * minScale, baseScale * maxScale);
        targetRoot.localScale = Vector3.one * nextScale;
    }

#if UNITY_EDITOR
    private void HandleMouseRotation()
    {
        if (!Input.GetMouseButton(0))
        {
            return;
        }

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        float mouseDelta = Input.GetAxis("Mouse X");
        if (Mathf.Abs(mouseDelta) <= Mathf.Epsilon)
        {
            return;
        }

        targetRoot.Rotate(Vector3.up, -mouseDelta * 12f, Space.World);
    }
#endif
}
