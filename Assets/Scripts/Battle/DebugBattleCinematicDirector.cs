using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public class DebugBattleCinematicDirector : MonoBehaviour, IBattleCinematicDriver
{
    [Header("Mode")]
    [SerializeField] private bool cinematicEnabled = true;

    [Header("Layer")]
    [SerializeField] private Canvas targetCanvas;
    [SerializeField] private RectTransform layerRoot;
    [SerializeField, Range(2, 16)] private int blurDownsample = 5;
    [SerializeField, Range(1, 8)] private int blurIterations = 3;
    [SerializeField, Range(1, 12)] private int blurRadius = 4;
    [SerializeField] private Color dimColor = new Color(0f, 0f, 0f, 0.78f);
    [SerializeField] private Color blurTintColor = new Color(0.025f, 0.025f, 0.025f, 0.32f);
    [SerializeField] private Color letterboxColor = Color.black;
    [SerializeField, Min(1f)] private float letterboxAspectWidth = 21f;
    [SerializeField, Min(1f)] private float letterboxAspectHeight = 9f;
    [SerializeField, Min(1f)] private float skillTitleFontSize = 58f;

    [Header("Layout")]
    [SerializeField] private Vector2 allyActorPosition = new Vector2(-520f, 150f);
    [SerializeField] private Vector2 enemyActorPosition = new Vector2(520f, 150f);
    [SerializeField] private Vector2 allySingleTargetPosition = new Vector2(-430f, 150f);
    [SerializeField] private Vector2 enemySingleTargetPosition = new Vector2(430f, 150f);
    [SerializeField] private Vector2 allyMultiTargetStartPosition = new Vector2(-430f, 270f);
    [SerializeField] private Vector2 enemyMultiTargetStartPosition = new Vector2(430f, 270f);
    [SerializeField] private Vector2 multiTargetSpacing = new Vector2(160f, -160f);
    [SerializeField] private Vector2 cinematicOffset = new Vector2(0f, -800f);
    [SerializeField, Min(0.1f)] private float cinematicViewScale = 1.55f;
    [SerializeField, Range(0f, 1f)] private float attackMotionOverflowRatio = 5f / 12f;
    [SerializeField] private int maxShownTargets = 4;
    [SerializeField, Range(0.25f, 0.55f)] private float actorEdgeStartXRatio = 0.46f;
    [SerializeField, Range(0.15f, 0.5f)] private float targetEdgeStartXRatio = 0.34f;

    [Header("Timing")]
    [SerializeField, Min(0f)] private float introDuration = 0.2f;
    [SerializeField, Min(0f)] private float preImpactStandingDuration = 0.15f;
    [SerializeField, Min(0f)] private float endHoldDuration = 1f;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.12f, 0.08f, 0.85f);
    [SerializeField] private Color missTextColor = new Color(0.75f, 0.82f, 0.9f, 1f);
    [SerializeField, Min(0.1f)] private float hitEffectScaleMultiplier = 1.3f;
    [SerializeField] private Vector2 floatingTextHeadOffset = new Vector2(0f, -90f);
    [SerializeField] private Vector2 floatingTextSize = new Vector2(620f, 260f);
    [SerializeField, Min(1f)] private float floatingTextFontSize = 112f;
    [SerializeField] private Vector2 floatingTextRiseOffset = new Vector2(0f, 95f);
    [SerializeField, Range(0f, 0.3f)] private float floatingTextOutlineWidth = 0.2f;
    [SerializeField] private Color floatingTextShadowColor = new Color(0f, 0f, 0f, 0.68f);
    [SerializeField] private Vector2 floatingTextShadowDistance = new Vector2(4f, -4f);

    [Header("Motion")]
    [SerializeField] private Vector2 foregroundDriftOffset = new Vector2(135f, 0f);
    [SerializeField, Range(0f, 0.5f)] private float foregroundDriftWidthRatio = 0.24f;
    [SerializeField, Min(0f)] private float targetOuterEdgeCenterOverrun = 90f;
    [SerializeField, Min(1f)] private float cutsceneZoomScale = 1.26f;
    [SerializeField] private Vector2 cutsceneZoomPivot = new Vector2(0.5f, 0.62f);
    [SerializeField, Range(0.05f, 1f)] private float driftEndSpeedRatio = 0.3f;
    [SerializeField, Range(1f, 4f)] private float driftStartSpeedMultiplier = 2.4f;
    [SerializeField, Range(1f, 4f)] private float zoomStartSpeedMultiplier = 2.8f;
    [SerializeField, Range(0.05f, 1f)] private float zoomEndSpeedRatio = 0.12f;

    [Header("Debug Font")]
    [SerializeField] private TMP_FontAsset debugTmpFont;

    private readonly Dictionary<BattleUnit, BattleUnitView> targetViews = new Dictionary<BattleUnit, BattleUnitView>();
    private readonly Dictionary<BattleUnit, RectTransform> targetRects = new Dictionary<BattleUnit, RectTransform>();
    private CanvasGroup layerCanvasGroup;
    private RawImage blurImage;
    private RectTransform foregroundMotionRoot;
    private RectTransform foregroundRoot;
    private RectTransform actorRect;
    private BattleUnitView actorView;
    private RectTransform effectRoot;
    private RectTransform textRoot;
    private RectTransform letterboxRoot;
    private RectTransform bottomLetterboxRoot;
    private RectTransform skillTitleRoot;
    private RectTransform topLetterbox;
    private RectTransform bottomLetterbox;
    private TMP_Text skillTitleText;
    private BattleViewManager sourceViewManager;
    private bool isPlaying;
    private TMP_FontAsset resolvedDebugTmpFont;
    private Texture2D capturedBackgroundTexture;
    private Texture2D blurredBackgroundTexture;
    private BattleUnit primaryCinematicTarget;
    private BattleUnit activeCinematicActor;
    private float activeForegroundDriftDistance;
    private bool activeSupportCinematic;

    private const string DefaultDebugTmpFontGuid = "6d30f13f865814d419adc2d97ba442be";

    public bool IsCinematicEnabled => cinematicEnabled;
    public bool IsCinematicPlaying => isPlaying;

    public IEnumerator PlayAttackCinematic(
        BattleUnit actor,
        SkillDefinition skill,
        IList<BattleUnit> targets,
        Sprite attackSprite,
        System.Func<IEnumerator> impactRoutine)
    {
        if (!cinematicEnabled)
        {
            if (impactRoutine != null)
                yield return impactRoutine();
            yield break;
        }

        EnsureLayer();
        ClearCinematicObjects();
        activeSupportCinematic = IsSupportCinematic(skill);
        activeCinematicActor = actor;
        BuildActor(actor, attackSprite);
        BuildTargets(actor, targets);
        SetSkillTitle(skill);
        BringCinematicForegroundToFront();
        yield return CaptureBlurredBackground();

        isPlaying = true;
        layerRoot.gameObject.SetActive(true);
        activeForegroundDriftDistance = ResolveForegroundDriftDistance(actor);
        SetLetterboxSlide(1f);
        float activeMotionDuration = introDuration + preImpactStandingDuration + endHoldDuration + introDuration;
        float zoomInDuration = introDuration + preImpactStandingDuration + endHoldDuration;
        Coroutine foregroundMotionRoutine = StartCoroutine(AnimateForegroundDrift(actor, 0f, 1f, activeMotionDuration));
        Coroutine foregroundZoomRoutine = StartCoroutine(ZoomCinematicForeground(1f, cutsceneZoomScale, zoomInDuration));
        yield return FadeLayer(0f, 1f, introDuration);

        if (preImpactStandingDuration > 0f)
            yield return new WaitForSeconds(preImpactStandingDuration);

        if (impactRoutine != null)
            yield return impactRoutine();

        if (endHoldDuration > 0f)
            yield return new WaitForSeconds(endHoldDuration);

        if (foregroundZoomRoutine != null)
            StopCoroutine(foregroundZoomRoutine);
        SetCinematicForegroundScale(cutsceneZoomScale);

        StartCoroutine(ZoomCinematicForeground(cutsceneZoomScale, 1f, introDuration));
        yield return FadeLayer(1f, 0f, introDuration);
        if (foregroundMotionRoutine != null)
            StopCoroutine(foregroundMotionRoutine);
        SetForegroundDrift(actor, 1f);
        SetCinematicForegroundScale(1f);
        ClearCinematicObjects();
        layerRoot.gameObject.SetActive(false);
        isPlaying = false;
    }

    public IEnumerator PlayAttackImpact(
        BattleUnit target,
        SkillDefinition skill,
        AttackResult result,
        float hitDuration,
        float missHoldDuration)
    {
        EnsureLayer();

        if (target == null)
            yield break;

        if (!targetViews.TryGetValue(target, out BattleUnitView targetView) || targetView == null)
            targetView = BuildTarget(target, targetRects.Count);

        if (targetView == null)
            yield break;

        if (!result.DidHit)
        {
            if (missHoldDuration > 0f)
                yield return new WaitForSeconds(missHoldDuration);
            yield break;
        }

        PlayHitEffect(skill, target);

        targetView.ShowHitReactionPose();
    }

    public void PlaySupportImpact(BattleUnit target, SkillDefinition skill)
    {
        EnsureLayer();
        PlayHitEffect(skill, target);
    }

    public void ShowFloatingText(BattleUnit target, string text, Color color, float duration)
    {
        if (target == null || string.IsNullOrWhiteSpace(text))
            return;

        RectTransform rect = CreateFloatingTextRoot(target);
        TMP_Text tmp = CreateFloatingTextElement(rect, "FloatingText", floatingTextFontSize);
        if (tmp == null)
            return;

        tmp.text = text;
        tmp.color = color;
        ApplyFloatingTextStyle(tmp, color);
        StartCoroutine(FloatUntilCinematicEnd(rect, endHoldDuration));
    }

    public void ShowFloatingTextParts(
        BattleUnit target,
        string title,
        string value,
        Color titleColor,
        Color valueColor,
        float duration)
    {
        if (target == null)
            return;

        if (string.IsNullOrWhiteSpace(title))
        {
            ShowFloatingText(target, value, valueColor, duration);
            return;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            ShowFloatingText(target, title, titleColor, duration);
            return;
        }

        ShowFloatingText(target, value, valueColor, duration);
    }

    private void EnsureLayer()
    {
        if (layerRoot != null && layerCanvasGroup != null && foregroundMotionRoot != null && foregroundRoot != null && effectRoot != null && textRoot != null && letterboxRoot != null && bottomLetterboxRoot != null && skillTitleRoot != null && skillTitleText != null)
        {
            UpdateLetterboxLayout();
            return;
        }

        if (targetCanvas == null)
        {
            GameObject battleCanvasObject = GameObject.Find("BattleCanvas");
            if (battleCanvasObject != null)
                targetCanvas = battleCanvasObject.GetComponent<Canvas>();
        }

        if (targetCanvas == null)
            targetCanvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        if (sourceViewManager == null)
            sourceViewManager = FindFirstObjectByType<BattleViewManager>(FindObjectsInactive.Include);

        Transform parent = targetCanvas != null ? targetCanvas.transform : transform;
        GameObject root = new GameObject("DebugBattleCinematicLayer", typeof(RectTransform), typeof(CanvasGroup));
        root.transform.SetParent(parent, false);
        layerRoot = root.GetComponent<RectTransform>();
        StretchToParent(layerRoot);

        layerCanvasGroup = root.GetComponent<CanvasGroup>();
        layerCanvasGroup.alpha = 0f;
        layerCanvasGroup.blocksRaycasts = true;
        layerCanvasGroup.interactable = false;

        blurImage = CreateRawPanel(layerRoot, "BlurBackground");
        blurImage.raycastTarget = false;
        blurImage.color = Color.white;
        Image dim = CreatePanel(layerRoot, "DimOverlay", dimColor);
        dim.raycastTarget = true;
        Image blurTint = CreatePanel(layerRoot, "BlurTintOverlay", blurTintColor);
        blurTint.raycastTarget = false;

        CreateLetterbox();
        foregroundMotionRoot = CreateRoot(layerRoot, "CinematicMotionRoot");
        foregroundRoot = CreateRoot(foregroundMotionRoot, "CinematicForegroundRoot");
        foregroundRoot.pivot = cutsceneZoomPivot;
        effectRoot = CreateRoot(foregroundRoot, "EffectRoot");
        textRoot = CreateRoot(layerRoot, "FloatingTextRoot");
        CreateSkillTitleRoot();

        layerRoot.gameObject.SetActive(false);
    }

    private void BuildActor(BattleUnit actor, Sprite attackSprite)
    {
        actorView = CreateCinematicView(actor, "CinematicActor", ResolveActorPosition(actor), true, attackSprite);
        actorRect = actorView != null ? actorView.RectTransform : null;
    }

    private void BuildTargets(BattleUnit actor, IList<BattleUnit> targets)
    {
        if (targets == null)
            return;

        int shownCount = 0;
        int targetViewIndex = 0;
        for (int i = 0; i < targets.Count && shownCount < maxShownTargets; i++)
        {
            BattleUnit target = targets[i];
            if (target == null)
                continue;

            if (primaryCinematicTarget == null)
                primaryCinematicTarget = target;

            if (activeSupportCinematic && target == actor)
            {
                RegisterActorAsTarget(target);
                shownCount++;
                continue;
            }

            BuildTarget(target, targetViewIndex);
            targetViewIndex++;
            shownCount++;
        }
    }

    private void RegisterActorAsTarget(BattleUnit target)
    {
        if (target == null)
            return;

        if (actorView != null)
            targetViews[target] = actorView;
        if (actorRect != null)
            targetRects[target] = actorRect;
    }

    private BattleUnitView BuildTarget(BattleUnit target, int index)
    {
        if (target == null)
            return null;

        BattleUnitView view = CreateCinematicView(target, $"CinematicTarget_{target.Name}", ResolveTargetPosition(target, index), false, null);
        RectTransform rect = view != null ? view.RectTransform : null;

        if (view != null)
            targetViews[target] = view;
        if (rect != null)
            targetRects[target] = rect;
        return view;
    }

    private void BringCinematicForegroundToFront()
    {
        if (foregroundMotionRoot != null)
            foregroundMotionRoot.SetAsLastSibling();

        if (actorRect != null)
            actorRect.SetAsLastSibling();

        if (effectRoot != null)
            effectRoot.SetAsLastSibling();
        if (bottomLetterboxRoot != null)
            bottomLetterboxRoot.SetAsLastSibling();
        if (skillTitleRoot != null)
            skillTitleRoot.SetAsLastSibling();
        if (textRoot != null)
            textRoot.SetAsLastSibling();
    }

    private void CreateLetterbox()
    {
        letterboxRoot = CreateRoot(layerRoot, "LetterboxRoot");

        Image topImage = CreatePanel(letterboxRoot, "TopLetterbox", letterboxColor);
        topImage.raycastTarget = false;
        topLetterbox = topImage.rectTransform;

        bottomLetterboxRoot = CreateRoot(layerRoot, "BottomLetterboxRoot");
        Image bottomImage = CreatePanel(bottomLetterboxRoot, "BottomLetterbox", letterboxColor);
        bottomImage.raycastTarget = false;
        bottomLetterbox = bottomImage.rectTransform;

        UpdateLetterboxLayout();
    }

    private void CreateSkillTitleRoot()
    {
        skillTitleRoot = CreateRoot(bottomLetterboxRoot != null ? bottomLetterboxRoot : layerRoot, "SkillTitleRoot");

        GameObject textObject = new GameObject("SkillTitleText", typeof(RectTransform));
        textObject.transform.SetParent(skillTitleRoot, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        StretchToParent(textRect);
        textRect.offsetMin = new Vector2(24f, 0f);
        textRect.offsetMax = new Vector2(-24f, 0f);

        skillTitleText = textObject.AddComponent<TextMeshProUGUI>();
        skillTitleText.alignment = TextAlignmentOptions.Center;
        skillTitleText.fontSize = skillTitleFontSize;
        skillTitleText.fontStyle = FontStyles.Bold;
        skillTitleText.color = Color.white;
        skillTitleText.outlineWidth = 0f;
        skillTitleText.outlineColor = Color.clear;
        skillTitleText.raycastTarget = false;
        skillTitleText.textWrappingMode = TextWrappingModes.NoWrap;

        TMP_FontAsset font = ResolveDebugTmpFont();
        if (font != null)
        {
            skillTitleText.font = font;
            skillTitleText.fontSharedMaterial = font.material;
        }

        Shadow shadow = textObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        shadow.effectDistance = new Vector2(4f, -4f);
        shadow.useGraphicAlpha = true;

        UpdateLetterboxLayout();
    }

    private void UpdateLetterboxLayout()
    {
        if (layerRoot == null || topLetterbox == null || bottomLetterbox == null)
            return;

        Rect rect = layerRoot.rect;
        float width = rect.width > 1f ? rect.width : Screen.width;
        float height = rect.height > 1f ? rect.height : Screen.height;
        float targetHeight = width * letterboxAspectHeight / Mathf.Max(0.01f, letterboxAspectWidth);
        float barHeight = Mathf.Max(0f, (height - targetHeight) * 0.5f);

        SetTopBar(topLetterbox, barHeight);
        SetBottomBar(bottomLetterbox, barHeight);

        if (skillTitleRoot != null)
            SetBottomBar(skillTitleRoot, barHeight);
    }

    private void SetSkillTitle(SkillDefinition skill)
    {
        if (skillTitleText == null)
            return;

        skillTitleText.text = skill != null && !string.IsNullOrWhiteSpace(skill.skillName)
            ? skill.skillName
            : string.Empty;
    }

    private static void SetTopBar(RectTransform rect, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(0f, -height);
        rect.offsetMax = Vector2.zero;
    }

    private static void SetBottomBar(RectTransform rect, float height)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = new Vector2(0f, height);
    }

    private IEnumerator AnimateForegroundDrift(BattleUnit actor, float from, float to, float duration)
    {
        duration = Mathf.Max(0.05f, duration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetForegroundDrift(actor, Mathf.Lerp(from, to, EvaluateDeceleratingMotion(t)));
            yield return null;
        }

        SetForegroundDrift(actor, to);
    }

    private float EvaluateDeceleratingMotion(float t)
    {
        return EvaluateDeceleratingMotion(t, driftStartSpeedMultiplier, driftEndSpeedRatio);
    }

    private static float EvaluateDeceleratingMotion(float t, float startSpeedMultiplier, float endSpeedRatio)
    {
        t = Mathf.Clamp01(t);
        endSpeedRatio = Mathf.Clamp(endSpeedRatio, 0.05f, 1f);
        float startSpeed = Mathf.Max(1f, startSpeedMultiplier);
        float endSpeed = startSpeed * endSpeedRatio;

        float a = endSpeed + startSpeed - 2f;
        float b = 3f - 2f * startSpeed - endSpeed;
        float curve = a * t * t * t + b * t * t + startSpeed * t;
        return Mathf.Clamp01(curve);
    }

    private void SetForegroundDrift(BattleUnit actor, float t)
    {
        float direction = actor != null && actor.Team == TeamType.Enemy ? -1f : 1f;
        float dynamicDrift = activeForegroundDriftDistance > 0f
            ? activeForegroundDriftDistance
            : GetLayerWidth() * foregroundDriftWidthRatio;
        float driftX = dynamicDrift > 0f ? dynamicDrift : foregroundDriftOffset.x;
        Vector2 offset = new Vector2(driftX * direction, foregroundDriftOffset.y) * Mathf.Clamp01(t);

        if (foregroundMotionRoot != null)
            foregroundMotionRoot.anchoredPosition = offset;
    }

    private float ResolveForegroundDriftDistance(BattleUnit actor)
    {
        float fallback = Mathf.Max(foregroundDriftOffset.x, GetLayerWidth() * foregroundDriftWidthRatio);
        BattleUnit target = primaryCinematicTarget;
        RectTransform targetRect = GetTargetRect(target);
        if (targetRect == null)
            return fallback;

        float direction = actor != null && actor.Team == TeamType.Enemy ? -1f : 1f;
        float targetWidth = Mathf.Abs(targetRect.rect.width * targetRect.localScale.x);
        float outerEdge = direction > 0f
            ? targetRect.anchoredPosition.x + targetWidth * (1f - targetRect.pivot.x)
            : targetRect.anchoredPosition.x - targetWidth * targetRect.pivot.x;
        float desiredOuterEdge = direction * targetOuterEdgeCenterOverrun;
        float required = (desiredOuterEdge - outerEdge) * direction;
        return Mathf.Max(fallback * 0.25f, required);
    }

    private void SetLetterboxSlide(float t)
    {
        float width = GetLayerWidth();
        float progress = Mathf.Clamp01(t);
        if (letterboxRoot != null)
            letterboxRoot.anchoredPosition = new Vector2(Mathf.Lerp(width, 0f, progress), 0f);
        if (bottomLetterboxRoot != null)
            bottomLetterboxRoot.anchoredPosition = new Vector2(Mathf.Lerp(-width, 0f, progress), 0f);
    }

    private BattleUnitView CreateCinematicView(BattleUnit unit, string objectName, Vector2 anchoredPosition, bool attackMotion, Sprite attackSprite)
    {
        BattleUnitView sourceView = sourceViewManager != null ? sourceViewManager.GetView(unit) : null;
        BattleUnitView prefab = unit != null && unit.ViewDefinition != null ? unit.ViewDefinition.viewPrefab : null;
        BattleUnitView source = sourceView != null ? sourceView : prefab;
        if (source == null)
            return null;

        BattleUnitView view = Instantiate(source, foregroundRoot != null ? foregroundRoot : layerRoot);
        view.name = objectName;
        view.Initialize(unit, string.Empty);
        view.SetCinematicChromeVisible(false);
        if (attackMotion)
        {
            view.SetBodySpriteOverride(ResolveActorSprite(unit, attackSprite));
            TeamType alignmentTeam = unit.Team;
            if (activeSupportCinematic)
                alignmentTeam = unit != null && unit.Team == TeamType.Enemy ? TeamType.Ally : TeamType.Enemy;
            view.ApplyCinematicAttackMotionAlignment(alignmentTeam, attackMotionOverflowRatio);
        }

        RectTransform rect = view.RectTransform;
        if (rect != null)
        {
            rect.anchoredPosition = anchoredPosition;
            Vector3 sourceScale = sourceView != null && sourceView.RectTransform != null
                ? sourceView.RectTransform.localScale
                : rect.localScale;
            rect.localScale = new Vector3(
                sourceScale.x * cinematicViewScale,
                sourceScale.y * cinematicViewScale,
                sourceScale.z);
        }

        return view;
    }

    private Vector2 ResolveActorPosition(BattleUnit actor)
    {
        float halfWidth = GetLayerWidth() * 0.5f;
        if (halfWidth > 0f)
        {
            if (activeSupportCinematic)
            {
                float supportActorX = halfWidth * targetEdgeStartXRatio;
                float direction = actor != null && actor.Team == TeamType.Enemy ? -1f : 1f;
                float actorY = actor != null && actor.Team == TeamType.Enemy
                    ? enemyActorPosition.y
                    : allyActorPosition.y;
                return new Vector2(supportActorX * direction, actorY) + cinematicOffset;
            }

            float x = halfWidth * actorEdgeStartXRatio;
            if (actor != null && actor.Team == TeamType.Enemy)
                return new Vector2(x, enemyActorPosition.y) + cinematicOffset;

            return new Vector2(-x, allyActorPosition.y) + cinematicOffset;
        }

        if (actor != null && actor.Team == TeamType.Enemy)
            return enemyActorPosition + cinematicOffset;

        return allyActorPosition + cinematicOffset;
    }

    private Vector2 ResolveTargetPosition(BattleUnit target, int index)
    {
        float halfWidth = GetLayerWidth() * 0.5f;
        if (index <= 0 && targetRects.Count <= 0)
        {
            if (halfWidth > 0f)
            {
                if (activeSupportCinematic)
                {
                    float supportTargetX = halfWidth * actorEdgeStartXRatio;
                    float direction = IsEnemySupportCinematic() ? 1f : -1f;
                    float targetY = target != null && target.Team == TeamType.Enemy
                        ? enemySingleTargetPosition.y
                        : allySingleTargetPosition.y;
                    return new Vector2(supportTargetX * direction, targetY) + cinematicOffset;
                }

                float x = halfWidth * targetEdgeStartXRatio;
                return target != null && target.Team == TeamType.Ally
                    ? new Vector2(-x, allySingleTargetPosition.y) + cinematicOffset
                    : new Vector2(x, enemySingleTargetPosition.y) + cinematicOffset;
            }

            return target != null && target.Team == TeamType.Ally
                ? allySingleTargetPosition + cinematicOffset
                : enemySingleTargetPosition + cinematicOffset;
        }

        Vector2 start = target != null && target.Team == TeamType.Ally
            ? allyMultiTargetStartPosition
            : enemyMultiTargetStartPosition;
        if (halfWidth > 0f)
        {
            float targetRatio = activeSupportCinematic ? actorEdgeStartXRatio : targetEdgeStartXRatio;
            start.x = activeSupportCinematic
                ? halfWidth * targetRatio * (IsEnemySupportCinematic() ? 1f : -1f)
                : (target != null && target.Team == TeamType.Ally ? -1f : 1f) * halfWidth * targetRatio;
        }

        Vector2 spacing = multiTargetSpacing;
        if (activeSupportCinematic)
            spacing.x = Mathf.Abs(spacing.x) * (IsEnemySupportCinematic() ? 1f : -1f);
        else if (target != null && target.Team == TeamType.Ally)
            spacing.x = -Mathf.Abs(spacing.x);
        else
            spacing.x = Mathf.Abs(spacing.x);

        return start + spacing * Mathf.Max(0, index) + cinematicOffset;
    }

    private float GetLayerWidth()
    {
        if (layerRoot == null)
            return Screen.width;

        float width = layerRoot.rect.width;
        return width > 1f ? width : Screen.width;
    }


    private Sprite ResolveActorSprite(BattleUnit actor, Sprite attackSprite)
    {
        if (attackSprite != null)
            return attackSprite;
        if (actor != null && actor.ViewDefinition != null)
            return actor.ViewDefinition.GetAttackBattleSprite();
        return actor != null ? actor.BattleSprite : null;
    }

    private static bool IsSupportCinematic(SkillDefinition skill)
    {
        return skill != null &&
               !(skill.resolutionMode == SkillResolutionMode.Attack && skill.HasDamageEffect());
    }

    private bool IsEnemySupportCinematic()
    {
        return activeSupportCinematic &&
               activeCinematicActor != null &&
               activeCinematicActor.Team == TeamType.Enemy;
    }

    private void PlayHitEffect(SkillDefinition skill, BattleUnit target)
    {
        if (!BattleEffectManager.TryResolveHitEffect(skill, null, out GameObject prefab, out _, out Vector2 offset, out float duration))
            return;

        if (prefab == null || effectRoot == null)
            return;

        GameObject effect = Instantiate(prefab, effectRoot);
        Vector3 anchorPosition = GetTargetEffectAnchorPosition(target);
        RectTransform effectRect = effect.GetComponent<RectTransform>();
        if (effectRect != null)
        {
            effectRect.position = anchorPosition;
            effectRect.anchoredPosition += offset;
        }
        else
        {
            effect.transform.position = anchorPosition + new Vector3(offset.x, offset.y, 0f);
        }

        bool mirrorX = target != null && target.Team == TeamType.Ally;
        Vector3 effectScale = effect.transform.localScale * hitEffectScaleMultiplier;
        if (mirrorX)
            effectScale.x = -Mathf.Abs(effectScale.x);
        effect.transform.localScale = effectScale;

        if (effect.TryGetComponent(out BattleRichHitEffectUI richEffect))
        {
            richEffect.SetDuration(duration);
            Vector2 spawnOffset = richEffect.SpawnOffset;
            if (mirrorX)
                spawnOffset.x = -spawnOffset.x;
            if (effectRect != null)
                effectRect.anchoredPosition += spawnOffset;
        }

        effectRoot.SetAsLastSibling();
        textRoot.SetAsLastSibling();
        Destroy(effect, duration);
    }

    private RectTransform GetTargetRect(BattleUnit target)
    {
        if (target != null && targetRects.TryGetValue(target, out RectTransform rect) && rect != null)
            return rect;

        return actorRect;
    }

    private Vector3 GetTargetEffectAnchorPosition(BattleUnit target)
    {
        if (target != null && targetViews.TryGetValue(target, out BattleUnitView view) && view != null)
            return view.HitEffectAnchorPosition;

        RectTransform rect = GetTargetRect(target);
        return rect != null ? rect.position : transform.position;
    }

    private RectTransform CreateFloatingTextRoot(BattleUnit target)
    {
        if (textRoot == null)
            return null;

        GameObject go = new GameObject("CinematicFloatingText", typeof(RectTransform), typeof(CanvasGroup));
        go.transform.SetParent(textRoot, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = floatingTextSize;
        rect.position = GetFloatingTextAnchorPosition(target);
        return rect;
    }

    private TMP_Text CreateFloatingTextElement(RectTransform parent, string objectName, float fontSize)
    {
        if (parent == null)
            return null;

        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(floatingTextSize.x, Mathf.Max(36f, fontSize + 12f));

        TMP_Text tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Normal;
        TMP_FontAsset font = ResolveDebugTmpFont();
        if (font != null)
        {
            tmp.font = font;
            tmp.fontSharedMaterial = font.material;
        }
        tmp.raycastTarget = false;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        return tmp;
    }

    private void ApplyFloatingTextStyle(TMP_Text tmp, Color textColor)
    {
        if (tmp == null)
            return;

        tmp.outlineWidth = floatingTextOutlineWidth;
        tmp.outlineColor = Color.white;

        Shadow shadow = tmp.GetComponent<Shadow>();
        if (shadow == null)
            shadow = tmp.gameObject.AddComponent<Shadow>();
        shadow.effectColor = floatingTextShadowColor;
        shadow.effectDistance = floatingTextShadowDistance;
        shadow.useGraphicAlpha = true;
    }

    private Vector3 GetFloatingTextAnchorPosition(BattleUnit target)
    {
        if (target != null && targetViews.TryGetValue(target, out BattleUnitView view) && view != null)
            return view.GetHitEffectAnchorPosition(HitEffectAnchorType.Overhead, floatingTextHeadOffset);

        RectTransform rect = GetTargetRect(target);
        if (rect == null)
            return transform.position;

        Vector3[] corners = new Vector3[4];
        rect.GetWorldCorners(corners);
        return (corners[1] + corners[2]) * 0.5f + rect.TransformVector(floatingTextHeadOffset);
    }

    private IEnumerator FloatUntilCinematicEnd(RectTransform rect, float duration)
    {
        if (rect == null)
            yield break;

        CanvasGroup group = rect.GetComponent<CanvasGroup>();
        if (group != null)
            group.alpha = 1f;

        Vector2 start = rect.anchoredPosition;
        Vector2 end = start + floatingTextRiseOffset;
        float total = Mathf.Max(0.05f, duration);
        float elapsed = 0f;
        while (elapsed < total)
        {
            if (rect == null)
                yield break;

            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / total);
            rect.anchoredPosition = Vector2.Lerp(start, end, t);
            yield return null;
        }
    }

    private IEnumerator ZoomCinematicForeground(float from, float to, float duration)
    {
        if (foregroundRoot == null)
            yield break;

        if (duration <= 0f)
        {
            SetCinematicForegroundScale(to);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = EvaluateDeceleratingMotion(Mathf.Clamp01(elapsed / duration), zoomStartSpeedMultiplier, zoomEndSpeedRatio);
            float scale = Mathf.Lerp(from, to, t);
            SetCinematicForegroundScale(scale);
            yield return null;
        }

        SetCinematicForegroundScale(to);
    }

    private void SetCinematicForegroundScale(float scale)
    {
        if (foregroundRoot == null)
            return;

        foregroundRoot.localScale = Vector3.one * Mathf.Max(1f, scale);
    }

    private IEnumerator FadeLayer(float from, float to, float duration)
    {
        if (layerCanvasGroup == null)
            yield break;

        if (duration <= 0f)
        {
            layerCanvasGroup.alpha = to;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            layerCanvasGroup.alpha = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        layerCanvasGroup.alpha = to;
    }

    private void ClearCinematicObjects()
    {
        targetViews.Clear();
        targetRects.Clear();
        primaryCinematicTarget = null;
        activeCinematicActor = null;
        activeForegroundDriftDistance = 0f;
        activeSupportCinematic = false;
        actorRect = null;
        actorView = null;
        ReleaseBlurredBackground();

        SetCinematicForegroundScale(1f);
        if (foregroundMotionRoot != null)
            foregroundMotionRoot.anchoredPosition = Vector2.zero;
        if (effectRoot != null)
            effectRoot.anchoredPosition = Vector2.zero;

        if (layerRoot == null)
            return;

        for (int i = layerRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = layerRoot.GetChild(i);
            if (child == null)
                continue;

            string childName = child.name;
            if (childName == "BlurBackground" || childName == "DimOverlay" || childName == "BlurTintOverlay" || childName == "CinematicMotionRoot" || childName == "FloatingTextRoot" || childName == "LetterboxRoot" || childName == "BottomLetterboxRoot")
                continue;

            Destroy(child.gameObject);
        }

        ClearForegroundChildren();
        ClearChildren(effectRoot);
        ClearChildren(textRoot);
        SetSkillTitle(null);
        SetLetterboxSlide(1f);
    }

    private void ClearForegroundChildren()
    {
        if (foregroundRoot == null)
            return;

        for (int i = foregroundRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = foregroundRoot.GetChild(i);
            if (child == null || child == effectRoot)
                continue;

            Destroy(child.gameObject);
        }
    }

    private static void ClearChildren(RectTransform root)
    {
        if (root == null)
            return;

        for (int i = root.childCount - 1; i >= 0; i--)
            Destroy(root.GetChild(i).gameObject);
    }

    private static Image CreatePanel(RectTransform parent, string objectName, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        StretchToParent(rect);
        Image image = go.GetComponent<Image>();
        image.color = color;
        return image;
    }

    private static RawImage CreateRawPanel(RectTransform parent, string objectName)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        StretchToParent(rect);
        return go.GetComponent<RawImage>();
    }

    private static RectTransform CreateRoot(RectTransform parent, string objectName)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        StretchToParent(rect);
        return rect;
    }

    private static void StretchToParent(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private TMP_FontAsset ResolveDebugTmpFont()
    {
        if (debugTmpFont != null)
            return debugTmpFont;
        if (resolvedDebugTmpFont != null)
            return resolvedDebugTmpFont;

#if UNITY_EDITOR
        string path = AssetDatabase.GUIDToAssetPath(DefaultDebugTmpFontGuid);
        resolvedDebugTmpFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
#endif
        if (resolvedDebugTmpFont == null)
            resolvedDebugTmpFont = TMP_Settings.defaultFontAsset;

        return resolvedDebugTmpFont;
    }

    private IEnumerator CaptureBlurredBackground()
    {
        if (blurImage == null)
            yield break;

        ReleaseBlurredBackground();
        blurImage.texture = null;

        yield return new WaitForEndOfFrame();

        capturedBackgroundTexture = ScreenCapture.CaptureScreenshotAsTexture();
        if (capturedBackgroundTexture == null)
            yield break;

        blurredBackgroundTexture = CreateBlurredBackgroundTexture(capturedBackgroundTexture);
        blurImage.texture = blurredBackgroundTexture;
    }

    private void ReleaseBlurredBackground()
    {
        if (blurImage != null)
            blurImage.texture = null;

        if (blurredBackgroundTexture != null)
        {
            Destroy(blurredBackgroundTexture);
            blurredBackgroundTexture = null;
        }

        if (capturedBackgroundTexture != null)
        {
            Destroy(capturedBackgroundTexture);
            capturedBackgroundTexture = null;
        }
    }

    private Texture2D CreateBlurredBackgroundTexture(Texture2D source)
    {
        if (source == null)
            return null;

        int downsample = Mathf.Max(2, blurDownsample);
        int width = Mathf.Max(1, source.width / downsample);
        int height = Mathf.Max(1, source.height / downsample);

        RenderTexture previous = RenderTexture.active;
        RenderTexture scaled = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);
        scaled.filterMode = FilterMode.Bilinear;

        Graphics.Blit(source, scaled);
        RenderTexture.active = scaled;

        Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        result.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        result.Apply(false, false);

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(scaled);

        ApplyBoxBlur(result, blurRadius, blurIterations);
        return result;
    }

    private static void ApplyBoxBlur(Texture2D texture, int radius, int iterations)
    {
        if (texture == null)
            return;

        int width = texture.width;
        int height = texture.height;
        if (width <= 1 || height <= 1)
            return;

        radius = Mathf.Max(1, radius);
        iterations = Mathf.Max(1, iterations);

        Color32[] source = texture.GetPixels32();
        Color32[] temp = new Color32[source.Length];
        Color32[] target = new Color32[source.Length];

        for (int i = 0; i < iterations; i++)
        {
            BoxBlurHorizontal(source, temp, width, height, radius);
            BoxBlurVertical(temp, target, width, height, radius);

            Color32[] swap = source;
            source = target;
            target = swap;
        }

        texture.SetPixels32(source);
        texture.Apply(false, false);
    }

    private static void BoxBlurHorizontal(Color32[] source, Color32[] target, int width, int height, int radius)
    {
        int diameter = radius * 2 + 1;
        for (int y = 0; y < height; y++)
        {
            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                int r = 0;
                int g = 0;
                int b = 0;
                int a = 0;

                for (int offset = -radius; offset <= radius; offset++)
                {
                    Color32 c = source[row + Mathf.Clamp(x + offset, 0, width - 1)];
                    r += c.r;
                    g += c.g;
                    b += c.b;
                    a += c.a;
                }

                target[row + x] = new Color32(
                    (byte)(r / diameter),
                    (byte)(g / diameter),
                    (byte)(b / diameter),
                    (byte)(a / diameter));
            }
        }
    }

    private static void BoxBlurVertical(Color32[] source, Color32[] target, int width, int height, int radius)
    {
        int diameter = radius * 2 + 1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int r = 0;
                int g = 0;
                int b = 0;
                int a = 0;

                for (int offset = -radius; offset <= radius; offset++)
                {
                    int sampleY = Mathf.Clamp(y + offset, 0, height - 1);
                    Color32 c = source[sampleY * width + x];
                    r += c.r;
                    g += c.g;
                    b += c.b;
                    a += c.a;
                }

                target[y * width + x] = new Color32(
                    (byte)(r / diameter),
                    (byte)(g / diameter),
                    (byte)(b / diameter),
                    (byte)(a / diameter));
            }
        }
    }
}
