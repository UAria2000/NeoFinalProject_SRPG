using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BattleBackgroundController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Sprite fallbackBackground;

    [Header("Layout")]
    [Tooltip("전투 배경 RectTransform 크기를 기준 해상도용 24:9 크기로 강제합니다.")]
    [SerializeField] private bool forceReferenceSize = true;
    [Tooltip("2560x1440 화면 안에서 좌우로 스크롤되는 24:9 배경 기준 크기입니다. 정확한 24:9는 3840x1440입니다.")]
    [SerializeField] private Vector2 referenceSizeDelta = new Vector2(3840f, 1440f);
    [Tooltip("켜면 referenceSizeDelta의 높이를 기준으로 가로를 항상 24:9로 보정합니다.")]
    [SerializeField] private bool enforceTwentyFourByNine = true;
    [SerializeField] private Vector2 pivot = new Vector2(0.5f, 0.5f);
    [SerializeField] private Vector2 anchoredPosition = Vector2.zero;

    [Header("Options")]
    [SerializeField] private bool preserveAspect = true;
    [SerializeField] private bool hideWhenMissing = false;

    private void Awake()
    {
        ApplyReferenceLayout();
    }

#if UNITY_EDITOR
    private bool validateLayoutQueued;

    private void OnValidate()
    {
        // RectTransform 값을 OnValidate 중에 직접 바꾸면 Unity가
        // "SendMessage cannot be called during Awake, CheckConsistency, or OnValidate"
        // 경고를 출력할 수 있습니다. 에디터 다음 틱으로 미뤄서 적용합니다.
        if (Application.isPlaying || validateLayoutQueued)
            return;

        validateLayoutQueued = true;
        UnityEditor.EditorApplication.delayCall += ApplyReferenceLayoutDelayedInEditor;
    }

    private void ApplyReferenceLayoutDelayedInEditor()
    {
        validateLayoutQueued = false;

        if (this == null || Application.isPlaying)
            return;

        ApplyReferenceLayout();
    }
#endif

    public void ApplyBackground(Sprite sprite)
    {
        Sprite resolved = sprite != null ? sprite : fallbackBackground;

        if (backgroundImage == null)
            return;

        ApplyReferenceLayout();

        bool hasSprite = resolved != null;
        backgroundImage.sprite = resolved;
        backgroundImage.enabled = hasSprite || !hideWhenMissing;
        backgroundImage.preserveAspect = preserveAspect;

        if (backgroundImage.gameObject != null)
            backgroundImage.gameObject.SetActive(hasSprite || !hideWhenMissing);
    }

    public void ApplyBackground(WorldGenerationSettings settings, FactionType faction, WorldTileEventType eventType)
    {
        Sprite sprite = settings != null ? settings.GetRandomBattleBackground(faction, eventType) : null;
        ApplyBackground(sprite);
    }

    public void ClearBackground()
    {
        if (backgroundImage == null)
            return;

        ApplyReferenceLayout();

        backgroundImage.sprite = null;
        backgroundImage.enabled = !hideWhenMissing;
        if (backgroundImage.gameObject != null)
            backgroundImage.gameObject.SetActive(!hideWhenMissing);
    }

    public void ApplyReferenceLayout()
    {
        if (!forceReferenceSize || backgroundImage == null)
            return;

        RectTransform rect = backgroundImage.rectTransform;
        if (rect == null)
            return;

        Vector2 size = referenceSizeDelta;
        if (enforceTwentyFourByNine)
            size.x = size.y * (24f / 9f);

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }
}
