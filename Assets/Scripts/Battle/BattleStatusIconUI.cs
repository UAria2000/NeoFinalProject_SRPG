using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 체력바 위에 표시되는 보호막/전투 기믹/상태이상/스탯 변화 아이콘 1칸.
/// 숫자는 보호막 수치, 상태이상 스택, 또는 스탯 변화 지속 턴을 표시한다.
/// 스탯 변화 아이콘만 Up/Down 화살표를 함께 표시한다.
///
/// 툴팁은 아이콘 바 전체가 아니라 실제 IconImage에 붙은 BattleStatusIconHoverTarget에서만 뜬다.
/// </summary>
[DisallowMultipleComponent]
public class BattleStatusIconUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Image upDownImage;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private GameObject countRoot;

    [Header("Hover")]
    [Tooltip("비워두면 Icon Image를 호버 영역으로 사용한다. 아이콘 이미지와 다른 영역을 쓰고 싶을 때만 별도 Image를 연결한다.")]
    [SerializeField] private Image hoverRaycastImage;

    private RectTransform rectTransform;
    private BattleStatusIconHoverTarget hoverTarget;
    private BattleStatusTooltipUI tooltipUI;
    private string tooltipTitle;
    private string tooltipBody;
    private bool pointerInside;

    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            return rectTransform;
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        AutoWireIfNeeded();
        ConfigureHoverRaycast();
    }

    private void OnDisable()
    {
        pointerInside = false;
        tooltipUI?.Hide();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoWireIfNeeded();
        ConfigureHoverRaycast();
    }
#endif

    public void Set(Sprite icon, int count, bool showCount)
    {
        Set(icon, count, showCount, null, false);
    }

    public void Set(Sprite icon, int count, bool showCount, Sprite arrowIcon, bool showArrow)
    {
        AutoWireIfNeeded();

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
            iconImage.preserveAspect = true;
        }

        bool arrowActive = showArrow && arrowIcon != null;
        if (upDownImage != null)
        {
            upDownImage.gameObject.SetActive(arrowActive);
            upDownImage.sprite = arrowActive ? arrowIcon : null;
            upDownImage.enabled = arrowActive;
            upDownImage.preserveAspect = true;
        }

        bool countActive = showCount && countText != null;
        if (countRoot != null)
            countRoot.SetActive(countActive);
        if (countText != null)
        {
            countText.gameObject.SetActive(countActive);
            countText.text = count.ToString();
        }

        gameObject.SetActive(icon != null);
        ConfigureHoverRaycast();
    }

    public void SetTooltip(BattleStatusTooltipUI tooltip, string title, string body)
    {
        tooltipUI = tooltip;
        tooltipTitle = title ?? string.Empty;
        tooltipBody = body ?? string.Empty;
        ConfigureHoverRaycast();
    }

    public void Clear()
    {
        pointerInside = false;
        tooltipUI?.Hide();
        tooltipTitle = string.Empty;
        tooltipBody = string.Empty;
        tooltipUI = null;

        if (iconImage != null)
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        if (upDownImage != null)
        {
            upDownImage.sprite = null;
            upDownImage.enabled = false;
            upDownImage.gameObject.SetActive(false);
        }

        if (countRoot != null)
            countRoot.SetActive(false);
        if (countText != null)
        {
            countText.text = string.Empty;
            countText.gameObject.SetActive(false);
        }

        ConfigureHoverRaycast();
        gameObject.SetActive(false);
    }

    public void HandlePointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ShowTooltip(eventData);
    }

    public void HandlePointerMove(PointerEventData eventData)
    {
        if (pointerInside)
            ShowTooltip(eventData);
    }

    public void HandlePointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        tooltipUI?.Hide();
    }

    private void ShowTooltip(PointerEventData eventData)
    {
        if (!HasTooltip())
            return;

        Vector2 position = eventData != null ? eventData.position : (Vector2)Input.mousePosition;
        tooltipUI.Show(tooltipTitle, tooltipBody, position);
    }

    private bool HasTooltip()
    {
        return tooltipUI != null && (!string.IsNullOrWhiteSpace(tooltipTitle) || !string.IsNullOrWhiteSpace(tooltipBody));
    }

    private void ConfigureHoverRaycast()
    {
        AutoWireIfNeeded();

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }

        Image targetImage = hoverRaycastImage != null ? hoverRaycastImage : iconImage;
        if (targetImage == null)
            return;

        targetImage.raycastTarget = HasTooltip() && targetImage.enabled && gameObject.activeSelf;

        if (hoverTarget == null || hoverTarget.gameObject != targetImage.gameObject)
        {
            hoverTarget = targetImage.GetComponent<BattleStatusIconHoverTarget>();
            if (hoverTarget == null)
                hoverTarget = targetImage.gameObject.AddComponent<BattleStatusIconHoverTarget>();
        }

        hoverTarget.Bind(this);
    }

    [ContextMenu("Auto Wire From Children")]
    public void AutoWireIfNeeded()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (iconImage == null)
        {
            Transform icon = transform.Find("IconImage");
            if (icon != null)
                iconImage = icon.GetComponent<Image>();
        }

        if (iconImage == null)
            iconImage = GetComponentInChildren<Image>(true);

        if (hoverRaycastImage == null)
        {
            Transform hover = transform.Find("HoverRaycastImage");
            if (hover != null)
                hoverRaycastImage = hover.GetComponent<Image>();
        }

        if (hoverRaycastImage == null)
            hoverRaycastImage = iconImage;

        if (upDownImage == null)
        {
            Transform arrow = transform.Find("UpDownImage");
            if (arrow != null)
                upDownImage = arrow.GetComponent<Image>();
        }

        if (countText == null)
        {
            Transform count = transform.Find("CountText");
            if (count != null)
                countText = count.GetComponent<TMP_Text>();
        }

        if (countText == null)
            countText = GetComponentInChildren<TMP_Text>(true);

        if (countRoot == null && countText != null)
            countRoot = countText.gameObject;
    }
}
