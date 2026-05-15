using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum WorldMapHoverPressButtonVisualMode
{
    NormalButton,
    ToggleButton
}

[RequireComponent(typeof(Button))]
public class WorldMapHoverPressButtonUI : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Image targetImage;
    [SerializeField] private RectTransform targetRect;

    [Header("Sprites")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite hoverSprite;
    [SerializeField] private Sprite pressedSprite;

    [Header("Behavior")]
    [Tooltip("NormalButton이면 일반 호버/프레스 버튼으로만 동작하고 Toggle On 이미지는 사용하지 않습니다. ToggleButton으로 설정한 버튼만 SetToggleOn 상태가 시각적으로 표시됩니다.")]
    [SerializeField] private WorldMapHoverPressButtonVisualMode visualMode = WorldMapHoverPressButtonVisualMode.NormalButton;

    [Header("Toggle On Sprites (Optional)")]
    [Tooltip("토글 ON 상태의 기본 이미지입니다. 비워두면 normalSprite를 사용합니다.")]
    [SerializeField] private Sprite toggleOnNormalSprite;
    [Tooltip("토글 ON 상태에서 호버 중일 때의 이미지입니다. 비워두면 hoverSprite, normalSprite 순서로 대체합니다.")]
    [SerializeField] private Sprite toggleOnHoverSprite;
    [Tooltip("토글 ON 상태에서 누르는 중일 때의 이미지입니다. 비워두면 pressedSprite, toggleOnNormalSprite, normalSprite 순서로 대체합니다.")]
    [SerializeField] private Sprite toggleOnPressedSprite;
    [Tooltip("토글 ON 상태일 때 켤 체크/선택 프레임/불빛 오브젝트입니다. 선택 사항입니다.")]
    [SerializeField] private GameObject toggleOnIndicatorRoot;

    [Header("Pressed Feel")]
    [SerializeField] private Vector2 pressedOffset = new Vector2(3f, -3f);
    [SerializeField] private float pressedScale = 0.96f;
    [SerializeField] private Color pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Header("State")]
    [SerializeField] private bool toggleOn;

    private Vector2 originalAnchoredPosition;
    private Vector3 originalScale;
    private Color originalColor;
    private bool isHovered;
    private bool isPressed;

    public bool IsToggleButton => visualMode == WorldMapHoverPressButtonVisualMode.ToggleButton;
    public bool ToggleOn => toggleOn;

    private void Awake()
    {
        if (targetImage == null)
            targetImage = GetComponent<Image>();

        if (targetRect == null)
            targetRect = transform as RectTransform;

        if (targetRect != null)
            originalAnchoredPosition = targetRect.anchoredPosition;

        if (targetRect != null)
            originalScale = targetRect.localScale;

        if (targetImage != null)
            originalColor = targetImage.color;

        ApplyVisual();
    }

    private void OnEnable()
    {
        ApplyVisual();
    }

    public void SetToggleOn(bool value)
    {
        if (toggleOn == value)
            return;

        toggleOn = value;
        ApplyVisual();
    }

    public void SetToggleOn()
    {
        SetToggleOn(true);
    }

    public void SetToggleOff()
    {
        SetToggleOn(false);
    }

    public void Toggle()
    {
        SetToggleOn(!toggleOn);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        ApplyVisual();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
        isPressed = false;
        ApplyVisual();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isPressed = true;
        ApplyVisual();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        isPressed = false;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (targetImage != null)
        {
            targetImage.sprite = ResolveSprite();
            targetImage.color = isPressed ? pressedColor : originalColor;
        }

        bool effectiveToggleOn = IsToggleButton && toggleOn;

        if (toggleOnIndicatorRoot != null)
            toggleOnIndicatorRoot.SetActive(effectiveToggleOn);

        if (targetRect != null)
        {
            targetRect.anchoredPosition = isPressed
                ? originalAnchoredPosition + pressedOffset
                : originalAnchoredPosition;

            targetRect.localScale = isPressed
                ? originalScale * pressedScale
                : originalScale;
        }
    }

    private Sprite ResolveSprite()
    {
        if (IsToggleButton && toggleOn)
        {
            Sprite toggleNormal = toggleOnNormalSprite != null ? toggleOnNormalSprite : normalSprite;
            Sprite toggleHover = toggleOnHoverSprite != null ? toggleOnHoverSprite : (hoverSprite != null ? hoverSprite : toggleNormal);
            Sprite togglePressed = toggleOnPressedSprite != null ? toggleOnPressedSprite : (pressedSprite != null ? pressedSprite : toggleNormal);

            if (isPressed && togglePressed != null)
                return togglePressed;
            if (isHovered && toggleHover != null)
                return toggleHover;
            return toggleNormal;
        }

        if (isPressed && pressedSprite != null)
            return pressedSprite;
        if (isHovered && hoverSprite != null)
            return hoverSprite;
        return normalSprite;
    }
}
