using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WorldSettlementPopupUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmText;

    [Header("Result")]
    [Tooltip("예: 1번째 세계 정복 / 1번째 세계 정복 실패. 큰 제목 '세계 정산'은 고정 라벨로 두면 연결하지 않아도 됩니다.")]
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text titleText; // legacy fallback

    [Header("Battle Record Values")]
    [SerializeField] private TMP_Text battleCountValueText;
    [SerializeField] private TMP_Text victoryCountValueText;
    [SerializeField] private TMP_Text defeatCountValueText;
    [SerializeField] private TMP_Text killedEnemyCountValueText;
    [SerializeField] private TMP_Text completedQuestCountValueText;

    [Header("EXP Values")]
    [SerializeField] private TMP_Text baseExpValueText;
    [SerializeField] private TMP_Text expBonusPercentValueText;
    [SerializeField] private TMP_Text finalExpValueText;

    [Header("Soul Values")]
    [SerializeField] private TMP_Text baseSoulValueText;
    [SerializeField] private TMP_Text soulBonusPercentValueText;
    [SerializeField] private TMP_Text finalSoulValueText;

    [Header("Captured Record")]
    [SerializeField] private TMP_Text capturedCountValueText;

    [Tooltip("포획 아이콘들이 자동 생성될 부모입니다. 이 오브젝트에 Grid Layout Group을 붙이면 한 줄 8개 같은 자동 배열이 됩니다.")]
    [SerializeField] private RectTransform capturedIconRoot;

    [Tooltip("포획 아이콘 1칸 프리팹입니다. Image 컴포넌트가 붙어 있어야 하며, 필요하면 Layout Element를 함께 붙여 크기를 고정하세요.")]
    [SerializeField] private Image capturedIconPrefab;

    [Tooltip("0 이하이면 전부 표시합니다. 예: 8이면 최대 8개만 생성합니다.")]
    [SerializeField] private int maxCapturedIconsToShow = 0;

    [Tooltip("이전 방식 호환용입니다. Captured Icon Root/Prefab을 연결하면 이 리스트는 사용하지 않습니다.")]
    [SerializeField] private List<Image> capturedIconImages = new List<Image>(8);

    private readonly List<Image> spawnedCapturedIconImages = new List<Image>();

    [Header("World Info Values")]
    [SerializeField] private TMP_Text worldSizeValueText;
    [SerializeField] private TMP_Text worldDifficultyValueText;
    [SerializeField] private TMP_Text lordNameValueText;
    [SerializeField] private TMP_Text lordLevelValueText;
    [SerializeField] private TMP_Text lordExpGainValueText;

    [Tooltip("군주 현재 경험치 숫자만 표시합니다. 예: 6,260")]
    [SerializeField] private TMP_Text lordExpCurrentValueText;

    [Tooltip("군주 다음 레벨까지 필요한 최대 경험치 숫자만 표시합니다. 예: 8,400")]
    [SerializeField] private TMP_Text lordExpMaxValueText;

    [Tooltip("구형 단일 텍스트 방식입니다. 비워도 됩니다. 연결되어 있으면 '현재 / 최대' 형식으로 표시합니다.")]
    [SerializeField] private TMP_Text lordExpText;

    [SerializeField] private Slider lordExpSlider;

    [Tooltip("전투 결과 카드처럼 Image Type=Filled 방식으로 게이지를 직접 제어할 때 연결합니다. Slider를 정상 세팅했다면 비워도 됩니다.")]
    [SerializeField] private Image lordExpFillImage;

    [Tooltip("선택 사항입니다. 연결하면 '다음 레벨까지 N' 형식으로 표시합니다.")]
    [SerializeField] private TMP_Text lordNextLevelText;

    [Header("Optional Legacy Body")]
    [SerializeField] private TMP_Text bodyText;

    [Header("Animation")]
    [SerializeField, Min(0f)] private float expCountDuration = 1f;

    private Action onConfirm;
    private bool initialized;
    private bool opening;
    private Coroutine expAnimationRoutine;

    private void Awake()
    {
        EnsureInitialized();

        if (!opening)
            CloseSilently();
    }

    private void EnsureInitialized()
    {
        if (initialized)
            return;

        initialized = true;

        if (root == null)
            root = gameObject;

        if (confirmButton == null)
            confirmButton = root != null ? root.GetComponentInChildren<Button>(true) : GetComponentInChildren<Button>(true);

        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(HandleConfirm);
        }
        else
        {
            Debug.LogWarning("[WorldSettlementPopupUI] Confirm Button is not assigned.", this);
        }
    }

    public void Open(WorldSettlementSummary summary, Action confirm)
    {
        opening = true;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureInitialized();

        onConfirm = confirm;
        Bind(summary);

        SetVisible(true);
        opening = false;
    }

    public void CloseSilently()
    {
        opening = false;
        onConfirm = null;
        if (expAnimationRoutine != null)
        {
            StopCoroutine(expAnimationRoutine);
            expAnimationRoutine = null;
        }

        ClearGeneratedCapturedIcons();
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (root != null)
            root.SetActive(visible);
        else
            gameObject.SetActive(visible);
    }

    private void HandleConfirm()
    {
        Action cb = onConfirm;
        CloseSilently();
        cb?.Invoke();
    }

    private void Bind(WorldSettlementSummary s)
    {
        if (s == null)
            return;

        string result = s.ResultLabel;
        SetText(resultText, result);
        if (resultText == null)
            SetText(titleText, result);

        SetText(confirmText, "메인 화면으로");

        SetText(battleCountValueText, FormatNumber(s.battleCount));
        SetText(victoryCountValueText, FormatNumber(s.victoryCount));
        SetText(defeatCountValueText, FormatNumber(s.defeatCount));
        SetText(killedEnemyCountValueText, FormatNumber(s.killedEnemyCount));
        SetText(completedQuestCountValueText, FormatNumber(s.completedQuestCount));

        SetText(baseExpValueText, FormatNumber(s.baseExpTotal));
        SetText(expBonusPercentValueText, FormatPercent(s.expBonusPercent));
        SetText(finalExpValueText, FormatNumber(s.totalSettlementExpAward));

        SetText(baseSoulValueText, FormatNumber(s.baseSoulTotal));
        SetText(soulBonusPercentValueText, FormatPercent(s.soulBonusPercent));
        SetText(finalSoulValueText, FormatNumber(s.totalSettlementSoulAward));

        SetText(capturedCountValueText, FormatNumber(s.capturedEnemyCount));
        BindCapturedIcons(s);

        SetText(worldSizeValueText, string.IsNullOrWhiteSpace(s.worldSizeLabel) ? "-" : s.worldSizeLabel);
        SetText(worldDifficultyValueText, string.IsNullOrWhiteSpace(s.worldDifficultyLabel) ? "-" : s.worldDifficultyLabel);
        SetText(lordNameValueText, string.IsNullOrWhiteSpace(s.lordName) ? "-" : s.lordName);
        SetText(lordLevelValueText, FormatNumber(s.lordLevelBefore));
        SetText(lordExpGainValueText, s.totalSettlementExpAward > 0 ? $"+{FormatNumber(s.totalSettlementExpAward)} 경험치" : "+0 경험치");

        if (bodyText != null)
            bodyText.text = BuildDebugBody(s);

        if (expAnimationRoutine != null)
            StopCoroutine(expAnimationRoutine);
        expAnimationRoutine = StartCoroutine(AnimateLordExpRoutine(s));
    }

    private void BindCapturedIcons(WorldSettlementSummary s)
    {
        int count = s != null && s.capturedPrisonerRecords != null ? s.capturedPrisonerRecords.Count : 0;

        if (capturedIconRoot != null && capturedIconPrefab != null)
        {
            BindGeneratedCapturedIcons(s, count);
            return;
        }

        BindLegacyCapturedIconSlots(s, count);
    }

    private void BindGeneratedCapturedIcons(WorldSettlementSummary s, int count)
    {
        ClearGeneratedCapturedIcons();

        if (s == null || s.capturedPrisonerRecords == null || count <= 0)
            return;

        int limit = maxCapturedIconsToShow > 0 ? Mathf.Min(count, maxCapturedIconsToShow) : count;
        for (int i = 0; i < limit; i++)
        {
            PrisonerRuntimeData prisoner = s.capturedPrisonerRecords[i];
            Sprite sprite = GetCapturedIcon(prisoner);
            if (sprite == null)
                continue;

            Image image = Instantiate(capturedIconPrefab, capturedIconRoot);
            image.gameObject.SetActive(true);
            image.sprite = sprite;
            image.enabled = true;
            image.raycastTarget = false;

            spawnedCapturedIconImages.Add(image);
        }
    }

    private void BindLegacyCapturedIconSlots(WorldSettlementSummary s, int count)
    {
        for (int i = 0; i < capturedIconImages.Count; i++)
        {
            Image image = capturedIconImages[i];
            if (image == null)
                continue;

            bool show = s != null && s.capturedPrisonerRecords != null && i < count;
            image.gameObject.SetActive(show);
            if (!show)
            {
                image.sprite = null;
                continue;
            }

            image.sprite = GetCapturedIcon(s.capturedPrisonerRecords[i]);
            image.enabled = image.sprite != null;
        }
    }

    private void ClearGeneratedCapturedIcons()
    {
        for (int i = 0; i < spawnedCapturedIconImages.Count; i++)
        {
            Image image = spawnedCapturedIconImages[i];
            if (image == null)
                continue;

            if (Application.isPlaying)
                Destroy(image.gameObject);
            else
                DestroyImmediate(image.gameObject);
        }

        spawnedCapturedIconImages.Clear();
    }

    private Sprite GetCapturedIcon(PrisonerRuntimeData prisoner)
    {
        if (prisoner == null)
            return null;

        if (prisoner.sourcePrisonerItem != null && prisoner.sourcePrisonerItem.icon != null)
            return prisoner.sourcePrisonerItem.icon;

        if (prisoner.sourceUnitViewDefinition != null)
            return prisoner.sourceUnitViewDefinition.GetSlotFaceSprite();

        if (prisoner.sourceUnit != null && prisoner.sourceUnit.captureRewardItem != null)
            return prisoner.sourceUnit.captureRewardItem.icon;

        return null;
    }

    private IEnumerator AnimateLordExpRoutine(WorldSettlementSummary s)
    {
        if (s == null)
            yield break;

        float duration = Mathf.Max(0f, expCountDuration);
        if (duration <= 0f)
        {
            ApplyLordExpVisual(s.lordLevelAfter, s.lordExpAfter, s.lordExpToNextAfter);
            yield break;
        }

        int beforeLevel = Mathf.Max(1, s.lordLevelBefore);
        int beforeExp = Mathf.Max(0, s.lordExpBefore);
        int beforeNeed = Mathf.Max(1, s.lordExpToNextBefore);
        int totalGain = Mathf.Max(0, s.totalSettlementExpAward);

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float p = Mathf.Clamp01(t / duration);
            int simulatedGain = Mathf.RoundToInt(totalGain * p);
            SimulateLevelExp(beforeLevel, beforeExp + simulatedGain, s.lordLevelAfter, out int lv, out int exp, out int need);
            ApplyLordExpVisual(lv, exp, need);
            yield return null;
        }

        ApplyLordExpVisual(s.lordLevelAfter, s.lordExpAfter, s.lordExpToNextAfter);
        expAnimationRoutine = null;
    }

    private void SimulateLevelExp(int startLevel, int totalExp, int maxPreviewLevel, out int level, out int exp, out int need)
    {
        level = Mathf.Max(1, startLevel);
        exp = Mathf.Max(0, totalExp);
        int guard = 0;
        while (level < Mathf.Max(level, maxPreviewLevel) && exp >= LegionFormula.GetExpToNextLevel(level) && guard < 1000)
        {
            exp -= LegionFormula.GetExpToNextLevel(level);
            level++;
            guard++;
        }

        need = Mathf.Max(1, LegionFormula.GetExpToNextLevel(level));
        exp = Mathf.Clamp(exp, 0, need);
    }

    private void ApplyLordExpVisual(int level, int exp, int need)
    {
        need = Mathf.Max(1, need);
        int clampedExp = Mathf.Clamp(exp, 0, need);
        float normalized = Mathf.Clamp01(clampedExp / (float)need);

        SetText(lordLevelValueText, FormatNumber(level));
        SetText(lordExpCurrentValueText, FormatNumber(clampedExp));
        SetText(lordExpMaxValueText, FormatNumber(need));
        SetText(lordExpText, $"{FormatNumber(clampedExp)} / {FormatNumber(need)}");
        SetText(lordNextLevelText, $"다음 레벨까지 {FormatNumber(Mathf.Max(0, need - clampedExp))}");

        if (lordExpSlider != null)
        {
            lordExpSlider.minValue = 0f;
            lordExpSlider.maxValue = 1f;
            lordExpSlider.value = normalized;
            lordExpSlider.interactable = false;
        }

        if (lordExpFillImage != null)
            lordExpFillImage.fillAmount = normalized;
    }

    private string BuildDebugBody(WorldSettlementSummary s)
    {
        if (s == null) return string.Empty;
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(s.ResultLabel);
        sb.AppendLine($"전투 {s.battleCount} / 승리 {s.victoryCount} / 패배 {s.defeatCount}");
        sb.AppendLine($"처치한 적 {s.killedEnemyCount} / 완료 임무 {s.completedQuestCount} / 포획 {s.capturedEnemyCount}");
        sb.AppendLine($"EXP {s.baseExpTotal} * {s.expBonusPercent}% = {s.totalSettlementExpAward}");
        sb.AppendLine($"소울 {s.baseSoulTotal} * {s.soulBonusPercent}% = {s.totalSettlementSoulAward}");

        if (s.purpleEssenceAward > 0)
        {
            sb.AppendLine($"보라색 정수 {s.purpleEssenceBaseAward} * {100 + s.purpleEssenceDifficultyBonusPercent}% = {s.purpleEssenceAward}");
            sb.AppendLine($"정수 기준: 점령 {s.occupiedTileCountForEssence} / 타락 {s.corruptedUnitCountForEssence}");
        }

        return sb.ToString();
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value ?? string.Empty;
    }

    private string FormatNumber(int value) => Mathf.Max(0, value).ToString("N0");
    private string FormatPercent(int value) => $"{Mathf.Max(0, value):N0}%";
}
