using TMPro;
using UnityEngine;

/// <summary>
/// 전투 명령 버튼 hover용 간단 툴팁.
/// 스킬 버튼과 마나 행동 버튼 모두 제목/본문 문자열만 받아 표시한다.
/// 권장 하이어라키:
/// BattleActionTooltipUIRoot
/// └─ Panel
///    ├─ TitleText
///    └─ BodyText
/// </summary>
public class BattleActionTooltipUI : HoverPopupUIBase
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;

    private void Reset()
    {
        AutoWireFromHierarchy();
    }

    private void Awake()
    {
        EnsureReferences();
        Hide();
    }

    [ContextMenu("Auto Wire From Current Hierarchy")]
    public void AutoWireFromHierarchy()
    {
        root = gameObject;
        titleText = FindText("TitleText");
        if (titleText == null)
            titleText = FindText("NameText");

        bodyText = FindText("BodyText");
        if (bodyText == null)
            bodyText = FindText("DescText");
        if (bodyText == null)
            bodyText = FindText("DescriptionText");
    }

    public void Show(string title, string body, Vector2 pointerScreenPosition)
    {
        EnsureReferences();

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
        {
            Hide();
            return;
        }

        ShowRootAt(root != null ? root : gameObject, pointerScreenPosition);

        if (titleText != null)
        {
            titleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(title));
            titleText.text = title ?? string.Empty;
            titleText.richText = true;
            titleText.enableWordWrapping = true;
        }

        if (bodyText != null)
        {
            bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(body));
            bodyText.text = body ?? string.Empty;
            bodyText.richText = true;
            bodyText.enableWordWrapping = true;
        }
    }

    public void Hide()
    {
        HideRoot(root != null ? root : gameObject);
    }

    private void EnsureReferences()
    {
        if (root == null)
            root = gameObject;
        if (titleText == null || bodyText == null)
            AutoWireFromHierarchy();
    }

    private TMP_Text FindText(string childName)
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
}
