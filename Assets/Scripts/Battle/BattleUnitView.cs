using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUnitView : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private Image unitBodyImage;
    [SerializeField] private Image attackMotionImage;
    [SerializeField] private Image hitMotionImage;
    [SerializeField] private TMP_Text labelText;
    [SerializeField] private Image hpFillImage;
    [Tooltip("전투 중 HP 바 위/근처에 표시할 현재 HP/최대 HP 텍스트입니다. 비워두면 HpText/HPText/HpValueText 이름으로 자동 탐색합니다.")]
    [SerializeField] private TMP_Text hpValueText;
    [SerializeField] private BattleStatusIconBarUI statusIconBar;

    [Header("Markers")]
    [SerializeField] private GameObject turnMark;
    [SerializeField] private GameObject targetMark;
    [SerializeField] private Image highlightImage;
    [SerializeField] private Color currentTurnHighlightColor = new Color(0.996f, 0.855f, 0.451f, 1f); // #FEDA73
    [SerializeField] private Color selectableHighlightColor = new Color(1f, 0.635f, 0.675f, 1f); // #FFA2AC
    [SerializeField] private Color hoverHighlightColor = new Color(1f, 0.227f, 0.286f, 1f); // #FF3A49
    [SerializeField] private RectTransform hoverAnchor;

    [Header("Click Area")]
    [Tooltip("유닛 클릭/호버/대상 선택을 담당할 투명 클릭 영역입니다. 프리팹의 ClickableArea를 연결하세요.")]
    [SerializeField] private RectTransform clickableArea;
    [Tooltip("ClickableArea에 붙은 Image/Graphic입니다. 비워두면 자동으로 찾거나 투명 Image를 추가합니다.")]
    [SerializeField] private Graphic clickableAreaGraphic;
    [Tooltip("켜면 ClickableArea를 제외한 하위 Graphic의 Raycast Target을 자동으로 끕니다. 상태 아이콘/HP바가 클릭을 가로막는 것을 방지합니다.")]
    [SerializeField] private bool disableChildGraphicRaycastsExceptClickableArea = true;

    [Header("Selection Rings")]
    [SerializeField] private GameObject activeRingRoot;
    [SerializeField] private GameObject infoSelectedRingRoot;

    [Header("Turn Finished Visual")]
    [Tooltip("한 라운드 안에서 이미 턴을 종료한 유닛의 전신 이미지 투명도입니다. 0.8 = 80% 표시.")]
    [Range(0f, 1f)]
    [SerializeField] private float finishedTurnBodyAlpha = 0.8f;

    [Header("Hit Flash")]
    [SerializeField] private Color hitFlashColor = new Color(1f, 0f, 0f, 0.65f);

    [Header("Effect Anchors")]
    [SerializeField, Range(0f, 1f)] private float hitEffectHeightNormalized = 0.62f;
    [SerializeField] private Vector2 hitEffectOffset;

    [Header("Deprecated - Not Used")]
    [Tooltip("폐기 예정. 더 이상 턴 예정/완료 표시로 사용하지 않습니다.")]
    [SerializeField] private Image upcomingGrayOverlayImage;
    [Tooltip("폐기 예정. 더 이상 턴 완료 표시로 사용하지 않습니다. 턴 완료 표시는 전신 이미지 투명도로 처리합니다.")]
    [SerializeField] private Image finishedGrayOverlayImage;

    private RectTransform rectTransform;
    private bool currentlyUsingFinishedVisual;
    private Sprite bodyOverrideSprite;
    private Coroutine hitFlashRoutine;
    private Color baseBodyColor = Color.white;
    private bool currentTurnHighlightActive;
    private bool selectableHighlightActive;
    private bool hoverHighlightActive;
    private bool usingAttackMotionImage;
    private bool hitReactionActive;
    private bool movementMotionActive;

    public BattleUnit Unit { get; private set; }
    public RectTransform RectTransform
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            return rectTransform;
        }
    }

    public RectTransform HoverAnchor => hoverAnchor != null ? hoverAnchor : RectTransform;
    public Vector3 HitEffectAnchorPosition => GetHitEffectAnchorPosition(HitEffectAnchorType.Default, Vector2.zero);

    public Vector3 GetHitEffectAnchorPosition(HitEffectAnchorType anchorType, Vector2 additionalOffset)
    {
        RectTransform sourceRect = unitBodyImage != null ? unitBodyImage.rectTransform : rectTransform;
        if (sourceRect == null)
            return transform.position;

        Vector3[] corners = new Vector3[4];
        sourceRect.GetWorldCorners(corners);

        Vector3 bottomCenter = (corners[0] + corners[3]) * 0.5f;
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
        Vector3 anchor;

        switch (anchorType)
        {
            case HitEffectAnchorType.Center:
                anchor = Vector3.Lerp(bottomCenter, topCenter, 0.5f);
                break;
            case HitEffectAnchorType.Overhead:
                anchor = topCenter;
                break;
            case HitEffectAnchorType.Feet:
                anchor = bottomCenter;
                break;
            default:
                anchor = Vector3.Lerp(bottomCenter, topCenter, hitEffectHeightNormalized);
                break;
        }

        Vector2 totalOffset = hitEffectOffset + additionalOffset;
        if (rectTransform != null)
            anchor += rectTransform.TransformVector(totalOffset);
        else
            anchor += new Vector3(totalOffset.x, totalOffset.y, 0f);

        return anchor;
    }
    public RectTransform ClickableArea => clickableArea;
    public Vector2 AnchoredPosition
    {
        get
        {
            if (rectTransform == null)
                rectTransform = GetComponent<RectTransform>();
            return rectTransform != null ? rectTransform.anchoredPosition : Vector2.zero;
        }
    }

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        EnsureHpValueText();
    }

    public void Initialize(BattleUnit unit, string label)
    {
        Unit = unit;
        currentlyUsingFinishedVisual = false;
        bodyOverrideSprite = null;
        baseBodyColor = Color.white;
        usingAttackMotionImage = false;
        hitReactionActive = false;
        movementMotionActive = false;
        currentTurnHighlightActive = false;
        selectableHighlightActive = false;
        hoverHighlightActive = false;

        if (labelText != null)
            labelText.text = label;

        DisableDeprecatedOverlays();
        SetTurnMark(false);
        SetTargetMark(false);
        SetHighlighted(false);
        ConfigureAttackMotionImage(false);
        ConfigureHitMotionImage(false);
        ApplyHighlightSprite();
        SetActionOwnerRing(false);
        SetInfoSelectedRing(false);
        SetFinishedTurnVisual(false);
        RefreshHPInstant();
        RefreshStatusIcons();
    }

    public void ConfigureClickHandling(BattleInputController inputController)
    {
        // Root에도 핸들러를 붙여 둔다. 다른 자식 Graphic이 Raycast를 먹어도 부모로 이벤트가 전달된다.
        BattleClickable rootClickable = GetComponent<BattleClickable>();
        if (rootClickable == null)
            rootClickable = gameObject.AddComponent<BattleClickable>();
        rootClickable.Initialize(this, inputController);

        RectTransform targetArea = clickableArea != null ? clickableArea : rectTransform;
        if (targetArea == null)
            return;

        BattleClickable areaClickable = targetArea.GetComponent<BattleClickable>();
        if (areaClickable == null)
            areaClickable = targetArea.gameObject.AddComponent<BattleClickable>();
        areaClickable.Initialize(this, inputController);

        EnsureClickableAreaGraphic(targetArea);

        if (disableChildGraphicRaycastsExceptClickableArea)
            DisableChildGraphicRaycastsExcept(targetArea);
    }

    private void EnsureClickableAreaGraphic(RectTransform targetArea)
    {
        if (targetArea == null)
            return;

        if (clickableAreaGraphic == null)
            clickableAreaGraphic = targetArea.GetComponent<Graphic>();

        if (clickableAreaGraphic == null)
        {
            Image image = targetArea.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = true;
            clickableAreaGraphic = image;
        }

        clickableAreaGraphic.raycastTarget = true;
        if (clickableAreaGraphic is Image clickableImage)
        {
            Color c = clickableImage.color;
            c.a = 0f;
            clickableImage.color = c;
        }
    }

    private void DisableChildGraphicRaycastsExcept(RectTransform exceptionRoot)
    {
        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            Graphic g = graphics[i];
            if (g == null)
                continue;

            if (exceptionRoot != null && (g.transform == exceptionRoot || g.transform.IsChildOf(exceptionRoot)))
                continue;

            g.raycastTarget = false;
        }
    }

    public void RefreshHPInstant()
    {
        if (hpFillImage != null && Unit != null)
        {
            float ratio = Unit.MaxHP > 0 ? (float)Unit.CurrentHP / Unit.MaxHP : 0f;
            hpFillImage.fillAmount = Mathf.Clamp01(ratio);
        }

        RefreshHpValueText();
        RefreshStatusIcons();
    }

    public IEnumerator AnimateHPChange(float duration)
    {
        if (hpFillImage == null || Unit == null)
        {
            RefreshHpValueText();
            RefreshStatusIcons();
            yield break;
        }

        float start = hpFillImage.fillAmount;
        float target = Unit.MaxHP > 0 ? Mathf.Clamp01((float)Unit.CurrentHP / Unit.MaxHP) : 0f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            hpFillImage.fillAmount = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        hpFillImage.fillAmount = target;
        RefreshHpValueText();
        RefreshStatusIcons();
    }


    private void EnsureHpValueText()
    {
        if (hpValueText != null)
            return;

        hpValueText = FindTextDeep("HpText");
        if (hpValueText == null)
            hpValueText = FindTextDeep("HPText");
        if (hpValueText == null)
            hpValueText = FindTextDeep("HpValueText");
        if (hpValueText == null)
            hpValueText = FindTextDeep("HPValueText");
    }

    private void RefreshHpValueText()
    {
        EnsureHpValueText();
        if (hpValueText == null)
            return;

        if (Unit == null)
        {
            hpValueText.text = string.Empty;
            hpValueText.gameObject.SetActive(false);
            return;
        }

        hpValueText.gameObject.SetActive(true);
        hpValueText.text = string.Format("{0}/{1}", Mathf.Max(0, Unit.CurrentHP), Mathf.Max(1, Unit.MaxHP));
    }

    private TMP_Text FindTextDeep(string childName)
    {
        Transform child = FindDeepChild(transform, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(childName))
            return null;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == childName)
                return child;

            Transform result = FindDeepChild(child, childName);
            if (result != null)
                return result;
        }

        return null;
    }

    public void RefreshStatusIcons()
    {
        if (statusIconBar != null)
            statusIconBar.Refresh(Unit);
    }

    public void RefreshBattleVisualState(bool isCurrentActionOwner, bool isInfoSelected, bool isFinishedThisRound)
    {
        SetActionOwnerRing(isCurrentActionOwner);
        SetCurrentTurnHighlight(isCurrentActionOwner);
        SetInfoSelectedRing(isInfoSelected);
        SetFinishedTurnVisual(isFinishedThisRound && !isCurrentActionOwner);
        RefreshHPInstant();
    }

    public void SetTurnMark(bool active)
    {
        if (turnMark != null)
            turnMark.SetActive(active);
    }

    public void SetTargetMark(bool active)
    {
        if (targetMark != null)
            targetMark.SetActive(active);
    }

    public void SetHighlighted(bool active)
    {
        SetSelectableHighlight(active);
    }

    public void SetCinematicChromeVisible(bool visible)
    {
        SetOptionalActive(labelText, visible);
        SetOptionalActive(hpFillImage, visible);
        SetOptionalActive(hpValueText, visible);
        if (hpFillImage != null && hpFillImage.transform.parent != null && hpFillImage.transform.parent != transform)
            SetOptionalActive(hpFillImage.transform.parent.gameObject, visible);
        SetOptionalActive(statusIconBar, visible);
        SetOptionalActive(turnMark, visible);
        SetOptionalActive(targetMark, visible);
        SetOptionalActive(highlightImage, visible);
        SetOptionalActive(activeRingRoot, visible);
        SetOptionalActive(infoSelectedRingRoot, visible);
        if (clickableArea != null && clickableArea != RectTransform)
            SetOptionalActive(clickableArea, visible);
        if (clickableAreaGraphic != null && clickableAreaGraphic.transform != transform)
            SetOptionalActive(clickableAreaGraphic, visible);
        SetOptionalActive(upcomingGrayOverlayImage, false);
        SetOptionalActive(finishedGrayOverlayImage, false);
    }

    public void SetCurrentTurnHighlight(bool active)
    {
        currentTurnHighlightActive = active;
        RefreshHighlightVisual();
    }

    public void SetSelectableHighlight(bool active)
    {
        selectableHighlightActive = active;
        RefreshHighlightVisual();
    }

    public void SetHoverHighlight(bool active)
    {
        hoverHighlightActive = active;
        RefreshHighlightVisual();
    }

    private void ApplyHighlightSprite()
    {
        if (highlightImage == null)
            return;

        Sprite sprite = Unit != null && Unit.ViewDefinition != null ? Unit.ViewDefinition.GetBattleHighlightSprite() : null;
        highlightImage.sprite = sprite;
        highlightImage.preserveAspect = true;
        highlightImage.raycastTarget = false;
        RefreshHighlightVisual();
    }

    private void RefreshHighlightVisual()
    {
        if (highlightImage == null)
            return;

        bool suppressedByMotion = hitReactionActive || movementMotionActive || usingAttackMotionImage;
        bool active = !suppressedByMotion && (hoverHighlightActive || selectableHighlightActive || currentTurnHighlightActive);
        highlightImage.gameObject.SetActive(active && highlightImage.sprite != null);

        if (!active || highlightImage.sprite == null)
            return;

        if (hoverHighlightActive)
            highlightImage.color = hoverHighlightColor;
        else if (selectableHighlightActive)
            highlightImage.color = selectableHighlightColor;
        else
            highlightImage.color = currentTurnHighlightColor;
    }

    public void SetActionOwnerRing(bool active)
    {
        if (activeRingRoot != null)
            activeRingRoot.SetActive(active);
    }

    public void SetInfoSelectedRing(bool active)
    {
        if (infoSelectedRingRoot != null)
            infoSelectedRingRoot.SetActive(active);
    }

    /// <summary>
    /// 기존 코드 호환용. Upcoming/Finished 회색 오버레이는 더 이상 사용하지 않고,
    /// finished 값이 true일 때 전신 이미지를 20% 알파로 표시한다.
    /// </summary>
    public void SetRoundStateOverlay(bool upcoming, bool finished)
    {
        DisableDeprecatedOverlays();
        SetFinishedTurnVisual(finished);
    }

    public void SetFinishedTurnVisual(bool finished)
    {
        if (currentlyUsingFinishedVisual == finished && unitBodyImage != null && unitBodyImage.sprite != null)
        {
            ApplyBodyAlpha(finished);
            return;
        }

        currentlyUsingFinishedVisual = finished;
        ApplyBodySprite(Unit != null && Unit.IsDead);
        ApplyBodyAlpha(finished);
    }

    private void ApplyBodySprite(bool useDeadBattleSprite)
    {
        if (unitBodyImage == null)
            return;

        Sprite sprite = null;
        if (!useDeadBattleSprite && bodyOverrideSprite != null)
            sprite = bodyOverrideSprite;
        if (sprite == null && Unit != null && Unit.ViewDefinition != null)
            sprite = Unit.ViewDefinition.GetBattleSprite(useDeadBattleSprite);
        if (sprite == null && Unit != null)
            sprite = Unit.BattleSprite;

        unitBodyImage.gameObject.SetActive(!usingAttackMotionImage && !hitReactionActive);
        unitBodyImage.sprite = sprite;
        ApplyBodyAlpha(currentlyUsingFinishedVisual);
        unitBodyImage.preserveAspect = true;
        unitBodyImage.raycastTarget = false;
    }

    private void ApplyBodyAlpha(bool finished)
    {
        if (unitBodyImage == null)
            return;

        Color color = Color.white;
        color.a = unitBodyImage.sprite == null ? 0f : (finished ? Mathf.Clamp01(finishedTurnBodyAlpha) : 1f);
        baseBodyColor = color;
        if (hitFlashRoutine == null)
            unitBodyImage.color = color;
    }

    private void DisableDeprecatedOverlays()
    {
        if (upcomingGrayOverlayImage != null)
            upcomingGrayOverlayImage.gameObject.SetActive(false);
        if (finishedGrayOverlayImage != null)
            finishedGrayOverlayImage.gameObject.SetActive(false);
    }

    public void SetBodySpriteOverride(Sprite overrideSprite)
    {
        bodyOverrideSprite = overrideSprite;

        if (overrideSprite != null && attackMotionImage != null)
        {
            usingAttackMotionImage = true;
            ConfigureAttackMotionImage(true, overrideSprite);
            RefreshPrimaryMotionVisibility();
            RefreshHighlightVisual();
            return;
        }

        usingAttackMotionImage = false;
        ConfigureAttackMotionImage(false);
        ApplyBodySprite(Unit != null && Unit.IsDead);
        RefreshPrimaryMotionVisibility();
        RefreshHighlightVisual();
    }

    public void ClearBodySpriteOverride()
    {
        if (bodyOverrideSprite == null && !usingAttackMotionImage)
            return;

        bodyOverrideSprite = null;
        usingAttackMotionImage = false;
        ConfigureAttackMotionImage(false);
        ApplyBodySprite(Unit != null && Unit.IsDead);
        RefreshPrimaryMotionVisibility();
        RefreshHighlightVisual();
    }

    public void ApplyCinematicAttackMotionAlignment(TeamType team, float overflowRatio)
    {
        if (attackMotionImage == null || !usingAttackMotionImage || attackMotionImage.sprite == null || Unit == null || Unit.ViewDefinition == null)
            return;

        RectTransform rect = attackMotionImage.rectTransform;
        if (rect == null)
            return;

        rect.anchoredPosition = Unit.ViewDefinition.attackSpriteAnchoredPosition;

        float width = rect.rect.width;
        if (width <= 0f)
            width = rect.sizeDelta.x;
        if (width <= 0f && attackMotionImage.sprite != null)
            width = attackMotionImage.sprite.rect.width;

        float shift = width * Mathf.Clamp01(overflowRatio) * 0.5f;
        if (team == TeamType.Enemy)
            shift = -shift;

        rect.anchoredPosition += new Vector2(shift, 0f);
    }

    private void ConfigureAttackMotionImage(bool active, Sprite sprite = null)
    {
        if (attackMotionImage == null)
            return;

        bool show = active && sprite != null;
        attackMotionImage.gameObject.SetActive(show && !hitReactionActive);
        attackMotionImage.sprite = show ? sprite : null;
        attackMotionImage.enabled = show;
        attackMotionImage.preserveAspect = true;
        attackMotionImage.raycastTarget = false;

        if (!active || Unit == null || Unit.ViewDefinition == null)
            return;

        RectTransform rect = attackMotionImage.rectTransform;
        rect.anchoredPosition = Unit.ViewDefinition.attackSpriteAnchoredPosition;
        if (Unit.ViewDefinition.attackSpriteSizeDelta.x > 0f && Unit.ViewDefinition.attackSpriteSizeDelta.y > 0f)
            rect.sizeDelta = Unit.ViewDefinition.attackSpriteSizeDelta;
        rect.localScale = Unit.ViewDefinition.attackSpriteLocalScale == Vector3.zero
            ? Vector3.one
            : Unit.ViewDefinition.attackSpriteLocalScale;
    }


    private void ConfigureHitMotionImage(bool active, Sprite sprite = null, Color? overrideColor = null)
    {
        if (hitMotionImage == null)
            return;

        bool show = active && sprite != null;
        hitMotionImage.gameObject.SetActive(show);
        hitMotionImage.sprite = show ? sprite : null;
        hitMotionImage.enabled = show;
        hitMotionImage.preserveAspect = true;
        hitMotionImage.raycastTarget = false;
        hitMotionImage.color = overrideColor.HasValue ? overrideColor.Value : baseBodyColor;

        if (!show || Unit == null || Unit.ViewDefinition == null)
            return;

        RectTransform rect = hitMotionImage.rectTransform;
        rect.anchoredPosition = Unit.ViewDefinition.hitSpriteAnchoredPosition;
        if (Unit.ViewDefinition.hitSpriteSizeDelta.x > 0f && Unit.ViewDefinition.hitSpriteSizeDelta.y > 0f)
            rect.sizeDelta = Unit.ViewDefinition.hitSpriteSizeDelta;
        rect.localScale = Unit.ViewDefinition.hitSpriteLocalScale == Vector3.zero
            ? Vector3.one
            : Unit.ViewDefinition.hitSpriteLocalScale;
    }


    private void RefreshPrimaryMotionVisibility()
    {
        if (unitBodyImage != null)
            unitBodyImage.gameObject.SetActive(!hitReactionActive && !usingAttackMotionImage);

        if (attackMotionImage != null)
        {
            bool showAttack = !hitReactionActive && usingAttackMotionImage && attackMotionImage.sprite != null;
            attackMotionImage.gameObject.SetActive(showAttack);
            attackMotionImage.enabled = showAttack;
        }
    }

    private void SetMotionHighlightSuppressed(bool suppressed)
    {
        if (movementMotionActive == suppressed)
            return;

        movementMotionActive = suppressed;
        RefreshHighlightVisual();
    }

    public void SetPositionInstant(Vector2 anchoredPosition)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
            rectTransform.anchoredPosition = anchoredPosition;
    }

    public void SetPositionInstant(Vector3 anchoredPosition)
    {
        SetPositionInstant(new Vector2(anchoredPosition.x, anchoredPosition.y));
    }

    public IEnumerator MoveToPosition(Vector2 anchoredPosition, float duration)
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        if (rectTransform == null)
            yield break;

        Vector2 start = rectTransform.anchoredPosition;
        SetMotionHighlightSuppressed(true);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(start, anchoredPosition, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        rectTransform.anchoredPosition = anchoredPosition;
        SetMotionHighlightSuppressed(false);
    }

    public IEnumerator MoveToPosition(Vector3 anchoredPosition, float duration)
    {
        yield return MoveToPosition(new Vector2(anchoredPosition.x, anchoredPosition.y), duration);
    }

    private IEnumerator MoveAnchoredPosition(Vector2 from, Vector2 to, float duration)
    {
        if (rectTransform == null)
            yield break;

        if (duration <= 0f)
        {
            rectTransform.anchoredPosition = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            rectTransform.anchoredPosition = Vector2.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        rectTransform.anchoredPosition = to;
    }

    public void PlayHitFlash(float duration)
    {
        if (!gameObject.activeInHierarchy || unitBodyImage == null)
            return;

        StopHitReactionImmediate();
        hitFlashRoutine = StartCoroutine(HitFlashRoutine(Mathf.Max(0.01f, duration)));
    }

    public IEnumerator PlayHitReaction(float duration)
    {
        if (!gameObject.activeInHierarchy || unitBodyImage == null)
            yield break;

        if (hitFlashRoutine != null)
            yield return hitFlashRoutine;

        hitFlashRoutine = StartCoroutine(HitFlashRoutine(Mathf.Max(0.01f, duration)));
        yield return hitFlashRoutine;
    }

    public void ShowHitReactionPose()
    {
        if (!gameObject.activeInHierarchy || unitBodyImage == null)
            return;

        StopHitReactionImmediate();

        Sprite hitSprite = Unit != null && Unit.ViewDefinition != null ? Unit.ViewDefinition.GetHitBattleSprite() : null;
        bool useDedicatedHitImage = hitMotionImage != null && hitSprite != null;

        Color flash = hitFlashColor;
        flash.a = Mathf.Clamp01(hitFlashColor.a);
        Color flashColor = Color.Lerp(baseBodyColor, flash, flash.a);

        hitReactionActive = true;
        RefreshPrimaryMotionVisibility();
        RefreshHighlightVisual();

        if (useDedicatedHitImage)
        {
            ConfigureHitMotionImage(true, hitSprite, flashColor);
            return;
        }

        if (hitSprite != null)
            unitBodyImage.sprite = hitSprite;

        unitBodyImage.gameObject.SetActive(true);
        unitBodyImage.color = flashColor;
    }

    private void StopHitReactionImmediate()
    {
        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
            hitFlashRoutine = null;
        }

        hitReactionActive = false;
        ConfigureHitMotionImage(false);

        if (unitBodyImage != null)
            unitBodyImage.color = baseBodyColor;

        RefreshPrimaryMotionVisibility();
        RefreshHighlightVisual();
    }

    private IEnumerator HitFlashRoutine(float duration)
    {
        if (unitBodyImage == null)
            yield break;

        Sprite originalSprite = unitBodyImage.sprite;
        Color originalColor = unitBodyImage.color;
        Sprite hitSprite = Unit != null && Unit.ViewDefinition != null ? Unit.ViewDefinition.GetHitBattleSprite() : null;
        bool useDedicatedHitImage = hitMotionImage != null && hitSprite != null;
        bool swappedBodySprite = false;

        Color start = baseBodyColor;
        Color flash = hitFlashColor;
        flash.a = Mathf.Clamp01(hitFlashColor.a);
        Color flashColor = Color.Lerp(start, flash, flash.a);

        hitReactionActive = true;
        RefreshPrimaryMotionVisibility();
        RefreshHighlightVisual();

        if (useDedicatedHitImage)
        {
            ConfigureHitMotionImage(true, hitSprite, flashColor);
        }
        else
        {
            if (hitSprite != null && unitBodyImage.sprite != hitSprite)
            {
                unitBodyImage.sprite = hitSprite;
                swappedBodySprite = true;
            }
            unitBodyImage.gameObject.SetActive(true);
            unitBodyImage.color = flashColor;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Color current = Color.Lerp(flashColor, baseBodyColor, t);
            if (useDedicatedHitImage && hitMotionImage != null)
                hitMotionImage.color = current;
            else if (unitBodyImage != null)
                unitBodyImage.color = current;
            yield return null;
        }

        if (useDedicatedHitImage)
        {
            ConfigureHitMotionImage(false);
        }
        else
        {
            if (swappedBodySprite)
                unitBodyImage.sprite = originalSprite;
            unitBodyImage.color = baseBodyColor;
        }

        if (unitBodyImage != null && !useDedicatedHitImage && unitBodyImage.color.a == 0f)
            unitBodyImage.color = originalColor;

        hitReactionActive = false;
        RefreshPrimaryMotionVisibility();
        RefreshHighlightVisual();
        hitFlashRoutine = null;
    }

    private static void SetOptionalActive(Component component, bool active)
    {
        if (component != null)
            component.gameObject.SetActive(active);
    }

    private static void SetOptionalActive(GameObject gameObject, bool active)
    {
        if (gameObject != null)
            gameObject.SetActive(active);
    }
}
