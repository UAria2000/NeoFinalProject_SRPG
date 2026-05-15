using UnityEngine;

public abstract class HoverPopupUIBase : MonoBehaviour
{
    [Header("Hover Popup")]
    [SerializeField] protected Vector2 screenOffset = new Vector2(120f, -50f);
    [SerializeField] protected bool keepInsideCanvas = true;
    [SerializeField] protected Vector2 canvasEdgePadding = new Vector2(24f, 24f);

    protected void ShowRootAt(GameObject rootObject, Vector2 pointerScreenPosition)
    {
        if (rootObject == null)
            return;

        rootObject.SetActive(true);

        RectTransform targetRect = rootObject.transform as RectTransform;
        if (targetRect == null)
            return;

        // 툴팁은 항상 다른 전투 UI 위에 보여야 한다.
        targetRect.SetAsLastSibling();

        Vector2 screenPosition = pointerScreenPosition + screenOffset;

        Canvas canvas = targetRect.GetComponentInParent<Canvas>();
        Canvas rootCanvas = canvas != null ? canvas.rootCanvas : null;
        RectTransform canvasRect = rootCanvas != null ? rootCanvas.transform as RectTransform : null;

        if (rootCanvas == null || canvasRect == null)
        {
            targetRect.position = screenPosition;
            return;
        }

        Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, eventCamera, out Vector2 canvasLocalPoint))
        {
            targetRect.position = screenPosition;
            return;
        }

        if (keepInsideCanvas)
            canvasLocalPoint = ClampToCanvas(canvasLocalPoint, canvasRect, targetRect);

        targetRect.position = canvasRect.TransformPoint(canvasLocalPoint);
    }

    protected void HideRoot(GameObject rootObject)
    {
        if (rootObject != null)
            rootObject.SetActive(false);
    }

    private Vector2 ClampToCanvas(Vector2 canvasLocalPosition, RectTransform canvasRect, RectTransform targetRect)
    {
        if (canvasRect == null || targetRect == null)
            return canvasLocalPosition;

        Rect canvasBounds = canvasRect.rect;
        Vector2 tooltipSize = GetTargetSizeInCanvasUnits(targetRect, canvasRect);
        Vector2 pivot = targetRect.pivot;

        float minX = canvasBounds.xMin + canvasEdgePadding.x + tooltipSize.x * pivot.x;
        float maxX = canvasBounds.xMax - canvasEdgePadding.x - tooltipSize.x * (1f - pivot.x);
        float minY = canvasBounds.yMin + canvasEdgePadding.y + tooltipSize.y * pivot.y;
        float maxY = canvasBounds.yMax - canvasEdgePadding.y - tooltipSize.y * (1f - pivot.y);

        if (minX <= maxX)
            canvasLocalPosition.x = Mathf.Clamp(canvasLocalPosition.x, minX, maxX);

        if (minY <= maxY)
            canvasLocalPosition.y = Mathf.Clamp(canvasLocalPosition.y, minY, maxY);

        return canvasLocalPosition;
    }

    private Vector2 GetTargetSizeInCanvasUnits(RectTransform targetRect, RectTransform canvasRect)
    {
        Vector2 size = targetRect.rect.size;

        if (canvasRect == null)
            return size;

        Vector3 canvasScale = canvasRect.lossyScale;
        Vector3 targetScale = targetRect.lossyScale;

        float scaleX = Mathf.Approximately(canvasScale.x, 0f) ? 1f : targetScale.x / canvasScale.x;
        float scaleY = Mathf.Approximately(canvasScale.y, 0f) ? 1f : targetScale.y / canvasScale.y;

        size.x *= Mathf.Abs(scaleX);
        size.y *= Mathf.Abs(scaleY);
        return size;
    }
}
