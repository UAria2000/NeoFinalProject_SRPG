using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 전투 상태 아이콘 호버 설명 패널.
/// 상태 아이콘 개별 hover target에서 전달받은 마우스 screen position 기준으로 표시한다.
/// </summary>
public class BattleStatusTooltipUI : HoverPopupUIBase
{
    [Header("References")]
    [Tooltip("내용을 켜고 끄는 자식 루트. 비워두면 이 오브젝트 전체를 사용한다. 위치 이동은 항상 이 컴포넌트가 붙은 오브젝트 기준으로 처리한다.")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text bodyText;
    [SerializeField] private LayoutElement layoutElement;

    [Header("Layout")]
    [SerializeField, Min(80f)] private float preferredWidth = 360f;

    private RectTransform selfRect;

    private void Awake()
    {
        AutoWireIfNeeded();
        Hide();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        AutoWireIfNeeded();
    }
#endif

    public void Show(string title, string body, Vector2 pointerScreenPosition)
    {
        AutoWireIfNeeded();

        if (string.IsNullOrWhiteSpace(title) && string.IsNullOrWhiteSpace(body))
        {
            Hide();
            return;
        }

        // 중요:
        // root 필드에 StatusRow 같은 자식이 연결되어 있어도,
        // 마우스 기준 위치 이동은 StatusHoverUIRoot 전체를 이동해야 한다.
        // 자식 Row만 움직이면 부모 좌표/스케일/캔버스 기준이 꼬여서 화면 아래쪽에 고정되는 문제가 생긴다.
        gameObject.SetActive(true);
        if (root != null)
            root.SetActive(true);

        if (titleText != null)
        {
            titleText.gameObject.SetActive(!string.IsNullOrWhiteSpace(title));
            titleText.text = title ?? string.Empty;
        }

        if (bodyText != null)
        {
            bodyText.gameObject.SetActive(!string.IsNullOrWhiteSpace(body));
            bodyText.text = body ?? string.Empty;
        }

        if (layoutElement != null)
            layoutElement.preferredWidth = preferredWidth;

        if (selfRect != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(selfRect);

        ShowRootAt(gameObject, pointerScreenPosition);

        if (root != null && root != gameObject)
            root.transform.SetAsLastSibling();
    }

    public void Hide()
    {
        if (root != null && root != gameObject)
            root.SetActive(false);

        HideRoot(gameObject);
    }

    private void AutoWireIfNeeded()
    {
        if (selfRect == null)
            selfRect = transform as RectTransform;

        if (root == null)
            root = gameObject;

        if (titleText == null)
        {
            Transform title = transform.Find("TitleText");
            if (title != null)
                titleText = title.GetComponent<TMP_Text>();
        }

        if (bodyText == null)
        {
            Transform body = transform.Find("BodyText");
            if (body != null)
                bodyText = body.GetComponent<TMP_Text>();
        }

        if (titleText == null || bodyText == null)
        {
            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            if (titleText == null && texts.Length > 0)
                titleText = texts[0];
            if (bodyText == null && texts.Length > 1)
                bodyText = texts[1];
        }

        if (layoutElement == null)
            layoutElement = GetComponent<LayoutElement>();

        Graphic[] graphics = GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i] != null)
                graphics[i].raycastTarget = false;
        }
    }
}
