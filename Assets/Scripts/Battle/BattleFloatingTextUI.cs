using System.Collections;
using TMPro;
using UnityEngine;

public class BattleFloatingTextUI : MonoBehaviour
{
    [Header("References - Legacy Combined")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("References - Separated")]
    [Tooltip("스킬명/출처 전용 텍스트입니다. 비워두면 기존 단일 Text 방식으로 표시됩니다.")]
    [SerializeField] private TMP_Text skillNameText;
    [Tooltip("데미지/회복량/회피 등 결과값 전용 텍스트입니다. 비워두면 기존 단일 Text 방식으로 표시됩니다.")]
    [SerializeField] private TMP_Text valueText;

    [Header("Defaults")]
    [SerializeField] private float defaultDuration = 1f;
    [SerializeField] private float defaultRiseDistance = 40f;

    private Coroutine playRoutine;

    private void Awake()
    {
        AutoResolveReferences();
    }

    private void AutoResolveReferences()
    {
        if (skillNameText == null)
            skillNameText = FindChildTextByName("SkillNameText", "SkillText", "TitleText", "SourceText");
        if (valueText == null)
            valueText = FindChildTextByName("DamageText", "ValueText", "AmountText", "ResultText");

        if (text == null && skillNameText == null && valueText == null)
            text = GetComponentInChildren<TMP_Text>(true);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        ConfigureText(text);
        ConfigureText(skillNameText);
        ConfigureText(valueText);
    }

    private TMP_Text FindChildTextByName(params string[] names)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
        if (texts == null || names == null)
            return null;

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text candidate = texts[i];
            if (candidate == null)
                continue;

            string objectName = candidate.gameObject.name;
            for (int n = 0; n < names.Length; n++)
            {
                if (string.Equals(objectName, names[n], System.StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }
        }

        return null;
    }

    private void ConfigureText(TMP_Text targetText)
    {
        if (targetText == null)
            return;

        targetText.raycastTarget = false;
        targetText.richText = true;
    }

    public void Bind(TMP_Text boundText, CanvasGroup boundCanvasGroup)
    {
        text = boundText;
        canvasGroup = boundCanvasGroup;
        ConfigureText(text);
    }

    public void BindSeparated(TMP_Text boundSkillNameText, TMP_Text boundValueText, CanvasGroup boundCanvasGroup)
    {
        skillNameText = boundSkillNameText;
        valueText = boundValueText;
        canvasGroup = boundCanvasGroup;
        ConfigureText(skillNameText);
        ConfigureText(valueText);
    }

    public void Play(string message, Color color, float duration = -1f, float riseDistance = -1f)
    {
        string title;
        string value;
        if (HasSeparatedFields() && TrySplitMessage(message, out title, out value))
        {
            PlaySeparated(title, value, color, color, duration, riseDistance);
            return;
        }

        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine(
            message,
            color,
            duration > 0f ? duration : defaultDuration,
            riseDistance >= 0f ? riseDistance : defaultRiseDistance,
            false,
            string.Empty,
            string.Empty,
            color,
            color));
    }

    public void PlaySeparated(string title, string value, Color titleColor, Color valueColor, float duration = -1f, float riseDistance = -1f)
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine(
            BuildCombinedMessage(title, value),
            valueColor,
            duration > 0f ? duration : defaultDuration,
            riseDistance >= 0f ? riseDistance : defaultRiseDistance,
            true,
            title,
            value,
            titleColor,
            valueColor));
    }

    private bool HasSeparatedFields()
    {
        return skillNameText != null || valueText != null;
    }

    private bool TrySplitMessage(string message, out string title, out string value)
    {
        title = string.Empty;
        value = string.Empty;

        if (string.IsNullOrEmpty(message))
            return false;

        int lineBreak = message.IndexOf('\n');
        if (lineBreak < 0)
        {
            value = message;
            return true;
        }

        title = message.Substring(0, lineBreak);
        value = message.Substring(lineBreak + 1);
        return true;
    }

    private string BuildCombinedMessage(string title, string value)
    {
        bool hasTitle = !string.IsNullOrWhiteSpace(title);
        bool hasValue = !string.IsNullOrWhiteSpace(value);

        if (hasTitle && hasValue)
            return title + "\n" + value;
        if (hasTitle)
            return title;
        return value ?? string.Empty;
    }

    private IEnumerator PlayRoutine(
        string combinedMessage,
        Color combinedColor,
        float duration,
        float riseDistance,
        bool preferSeparated,
        string title,
        string value,
        Color titleColor,
        Color valueColor)
    {
        ApplyTextState(combinedMessage, combinedColor, preferSeparated, title, value, titleColor, valueColor);

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        RectTransform rect = GetComponent<RectTransform>();
        Vector2 start = rect != null ? rect.anchoredPosition : Vector2.zero;
        Vector2 end = start + Vector2.up * riseDistance;

        float elapsed = 0f;
        duration = Mathf.Max(0.01f, duration);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (rect != null)
                rect.anchoredPosition = Vector2.Lerp(start, end, t);
            if (canvasGroup != null)
                canvasGroup.alpha = 1f - Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void ApplyTextState(
        string combinedMessage,
        Color combinedColor,
        bool preferSeparated,
        string title,
        string value,
        Color titleColor,
        Color valueColor)
    {
        bool useSeparated = preferSeparated && HasSeparatedFields();

        if (text != null)
        {
            text.gameObject.SetActive(!useSeparated);
            if (!useSeparated)
            {
                text.text = combinedMessage ?? string.Empty;
                text.color = combinedColor;
            }
        }

        if (skillNameText != null)
        {
            bool showTitle = useSeparated && !string.IsNullOrWhiteSpace(title);
            skillNameText.gameObject.SetActive(showTitle);
            if (showTitle)
            {
                skillNameText.text = title;
                skillNameText.color = titleColor;
            }
        }

        if (valueText != null)
        {
            bool showValue = useSeparated && !string.IsNullOrWhiteSpace(value);
            valueText.gameObject.SetActive(showValue);
            if (showValue)
            {
                valueText.text = value;
                valueText.color = valueColor;
            }
        }

        if (useSeparated && text == null && skillNameText == null && valueText == null)
        {
            // Safety fallback. In practice this branch should not be reached because useSeparated requires a field.
            Debug.LogWarning("[BattleFloatingTextUI] No TMP_Text references are assigned.");
        }
    }
}
