using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StorageItemTooltipUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private RectTransform tooltipRect;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text categoryText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text equippedStateText;

    [Header("Offsets")]
    [SerializeField] private Vector2 rightSideOffset = new Vector2(24f, 24f);
    [SerializeField] private Vector2 leftSideOffset = new Vector2(-24f, 24f);

    [Header("Visibility")]
    [SerializeField] private bool keepInsideCanvas = true;
    [SerializeField] private Vector2 canvasEdgePadding = new Vector2(24f, 24f);
    [SerializeField] private bool disableTooltipRaycasts = true;

    private Canvas parentCanvas;
    private Canvas rootCanvas;
    private CanvasGroup canvasGroup;
    private bool visible;
    private bool initialized;
    private int currentColumnIndex;

    private void Awake()
    {
        EnsureInitialized();

        // Show()가 비활성 오브젝트에 외부 호출된 뒤 SetActive(true)로 Awake가 늦게 실행되는 경우가 있다.
        // 그 상황에서 Awake가 다시 꺼버리면 하이어라키에는 켜졌다가도 실제 패널이 보이지 않는다.
        if (!visible)
            HideImmediate();
    }

    private void OnDisable()
    {
        visible = false;
    }

    private void Update()
    {
        if (!visible)
            return;

        FollowMouse();
    }

    public void Show(ItemDefinition item, bool isAssigned, int columnIndexInRow)
    {
        EnsureInitialized();

        if (item == null)
        {
            Hide();
            return;
        }

        currentColumnIndex = Mathf.Clamp(columnIndexInRow, 0, 9);

        if (iconImage != null)
        {
            iconImage.gameObject.SetActive(item.icon != null);
            iconImage.sprite = item.icon;
        }

        if (itemNameText != null)
            itemNameText.text = item.itemName;

        if (categoryText != null)
            categoryText.text = GetCategoryLabel(item);

        if (descriptionText != null)
            descriptionText.text = item.description;

        if (equippedStateText != null)
            equippedStateText.text = isAssigned ? "장착/세팅 중" : string.Empty;

        visible = true;
        SetTooltipActive(true);

        transform.SetAsLastSibling();
        if (tooltipRect != null)
            tooltipRect.SetAsLastSibling();

        ApplyCanvasGroupVisible(true);
        ApplyGraphicRaycastState();
        FollowMouse();
    }

    public void Hide()
    {
        visible = false;
        ApplyCanvasGroupVisible(false);
        SetTooltipActive(false);
    }

    private void HideImmediate()
    {
        visible = false;
        ApplyCanvasGroupVisible(false);
        SetTooltipActive(false);
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        if (tooltipRect == null)
            tooltipRect = transform as RectTransform;

        parentCanvas = GetComponentInParent<Canvas>(true);
        rootCanvas = parentCanvas != null ? parentCanvas.rootCanvas : null;

        if (tooltipRect != null)
            canvasGroup = tooltipRect.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        initialized = true;
    }

    private void SetTooltipActive(bool active)
    {
        if (active)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (tooltipRect != null && tooltipRect.gameObject != gameObject && !tooltipRect.gameObject.activeSelf)
                tooltipRect.gameObject.SetActive(true);
        }
        else
        {
            if (tooltipRect != null && tooltipRect.gameObject != gameObject && tooltipRect.gameObject.activeSelf)
                tooltipRect.gameObject.SetActive(false);

            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }
    }

    private void ApplyCanvasGroupVisible(bool isVisible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = isVisible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ApplyGraphicRaycastState()
    {
        if (!disableTooltipRaycasts)
            return;

        RectTransform target = GetTargetRect();
        if (target == null)
            return;

        Graphic[] graphics = target.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }

    private RectTransform GetTargetRect()
    {
        if (tooltipRect != null)
            return tooltipRect;

        return transform as RectTransform;
    }

    private void FollowMouse()
    {
        EnsureInitialized();

        RectTransform targetRect = GetTargetRect();
        if (targetRect == null)
            return;

        if (Mouse.current == null)
            return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector2 offset = currentColumnIndex <= 4 ? rightSideOffset : leftSideOffset;

        Canvas canvas = rootCanvas != null ? rootCanvas : parentCanvas;
        if (canvas == null)
        {
            targetRect.position = mouseScreenPosition + offset;
            return;
        }

        RectTransform canvasRect = canvas.transform as RectTransform;
        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;

        if (canvasRect == null)
        {
            targetRect.position = mouseScreenPosition + offset;
            return;
        }

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                mouseScreenPosition,
                eventCamera,
                out Vector2 canvasLocalPoint))
        {
            return;
        }

        Vector2 canvasLocalPosition = canvasLocalPoint + offset;
        if (keepInsideCanvas)
            canvasLocalPosition = ClampToCanvas(canvasLocalPosition, canvasRect, targetRect);

        // 중요:
        // 이전 버전은 canvas 기준 localPoint를 tooltipRect.anchoredPosition에 바로 넣었다.
        // tooltipRect가 TooltipUIRoot의 자식 Panel이면 좌표계가 달라져서 패널이 화면 밖으로 밀릴 수 있다.
        // canvas local 좌표를 world 좌표로 변환해서 targetRect.position에 넣으면
        // tooltipRect가 루트든 자식 Panel이든 같은 방식으로 화면 안에 배치된다.
        Vector3 worldPosition = canvasRect.TransformPoint(canvasLocalPosition);
        targetRect.position = worldPosition;
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

    private string GetCategoryLabel(ItemDefinition item)
    {
        if (item == null)
            return string.Empty;

        switch (item.mainUICategory)
        {
            case MainUIItemCategory.Equipment:
                return "장비";
            case MainUIItemCategory.Consumable:
                return "소모품";
            case MainUIItemCategory.Other:
                return "기타";
            default:
                return string.Empty;
        }
    }
}
