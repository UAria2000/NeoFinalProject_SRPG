using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 월드맵 점령 진행도에 따라 추가 배경 이미지의 알파값을 조절합니다.
/// 기본 계산식은 "플레이어가 점령한 타일 수 / 전체 점령 가능 타일 수"입니다.
/// </summary>
[DisallowMultipleComponent]
public class WorldConquestProgressBackgroundUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WorldRunManager worldRunManager;
    [SerializeField] private Image progressBackgroundImage;
    [SerializeField] private CanvasGroup progressCanvasGroup;

    [Header("Progress Calculation")]
    [Tooltip("켜두면 플레이어 시작 타일은 점령 진행도 계산에서 제외합니다. 시작 직후 알파가 0이 되게 하려면 켜두세요.")]
    [SerializeField] private bool excludePlayerStartTile = true;

    [Tooltip("켜두면 숨겨진 타일까지 포함해서 전체 월드 기준으로 계산합니다. 꺼두면 현재 드러난 타일만 분모로 사용합니다.")]
    [SerializeField] private bool includeUnrevealedTilesInTotal = true;

    [Header("Alpha")]
    [Range(0f, 1f)] [SerializeField] private float minAlpha = 0f;
    [Range(0f, 1f)] [SerializeField] private float maxAlpha = 1f;
    [SerializeField] private AnimationCurve alphaByProgress = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    [Header("Refresh")]
    [SerializeField] private bool refreshOnEnable = true;
    [SerializeField] private bool animateAlphaChange = true;
    [Min(0f)] [SerializeField] private float alphaTweenDuration = 0.25f;

    private Coroutine alphaRoutine;
    private float currentAlpha = -1f;

    private void Reset()
    {
        progressBackgroundImage = GetComponent<Image>();
        progressCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void Awake()
    {
        if (progressBackgroundImage == null)
            progressBackgroundImage = GetComponent<Image>();

        if (progressCanvasGroup == null)
            progressCanvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        BindManagerIfNeeded();
        Subscribe();

        if (refreshOnEnable)
            RefreshImmediate();
    }

    private void Start()
    {
        BindManagerIfNeeded();
        Subscribe();
        RefreshImmediate();
    }

    private void OnDisable()
    {
        Unsubscribe();

        if (alphaRoutine != null)
        {
            StopCoroutine(alphaRoutine);
            alphaRoutine = null;
        }
    }

    private void BindManagerIfNeeded()
    {
        if (worldRunManager == null)
            worldRunManager = UnityEngine.Object.FindFirstObjectByType<WorldRunManager>();
    }

    private void Subscribe()
    {
        if (worldRunManager == null)
            return;

        worldRunManager.OnWorldStateChanged -= HandleWorldStateChanged;
        worldRunManager.OnWorldStateChanged += HandleWorldStateChanged;
        worldRunManager.OnCurrentTileChanged -= HandleCurrentTileChanged;
        worldRunManager.OnCurrentTileChanged += HandleCurrentTileChanged;
    }

    private void Unsubscribe()
    {
        if (worldRunManager == null)
            return;

        worldRunManager.OnWorldStateChanged -= HandleWorldStateChanged;
        worldRunManager.OnCurrentTileChanged -= HandleCurrentTileChanged;
    }

    private void HandleWorldStateChanged()
    {
        Refresh();
    }

    private void HandleCurrentTileChanged(WorldTileData tile)
    {
        Refresh();
    }

    [ContextMenu("Refresh Now")]
    public void RefreshImmediate()
    {
        float alpha = CalculateTargetAlpha();
        SetAlpha(alpha);
    }

    public void Refresh()
    {
        float targetAlpha = CalculateTargetAlpha();

        if (!animateAlphaChange || alphaTweenDuration <= 0f || !Application.isPlaying)
        {
            SetAlpha(targetAlpha);
            return;
        }

        if (alphaRoutine != null)
            StopCoroutine(alphaRoutine);

        alphaRoutine = StartCoroutine(TweenAlpha(targetAlpha));
    }

    private float CalculateTargetAlpha()
    {
        float progress = CalculateConquestProgress01();
        float curved = alphaByProgress != null ? alphaByProgress.Evaluate(progress) : progress;
        return Mathf.Lerp(minAlpha, maxAlpha, Mathf.Clamp01(curved));
    }

    public float CalculateConquestProgress01()
    {
        WorldMapData mapData = worldRunManager != null ? worldRunManager.MapData : null;
        if (mapData == null || mapData.tiles == null || mapData.tiles.Count == 0)
            return 0f;

        int owned = 0;
        int total = 0;

        for (int i = 0; i < mapData.tiles.Count; i++)
        {
            WorldTileData tile = mapData.tiles[i];
            if (tile == null)
                continue;

            if (excludePlayerStartTile && tile.isPlayerStart)
                continue;

            if (!includeUnrevealedTilesInTotal && !tile.revealed && tile.currentOwner != FactionType.Player)
                continue;

            total++;

            if (tile.currentOwner == FactionType.Player)
                owned++;
        }

        if (total <= 0)
            return 0f;

        return Mathf.Clamp01(owned / (float)total);
    }

    private IEnumerator TweenAlpha(float targetAlpha)
    {
        float startAlpha = currentAlpha >= 0f ? currentAlpha : GetCurrentAlpha();
        float elapsed = 0f;

        while (elapsed < alphaTweenDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = alphaTweenDuration > 0f ? Mathf.Clamp01(elapsed / alphaTweenDuration) : 1f;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
        alphaRoutine = null;
    }

    private float GetCurrentAlpha()
    {
        if (progressCanvasGroup != null)
            return progressCanvasGroup.alpha;

        if (progressBackgroundImage != null)
            return progressBackgroundImage.color.a;

        return 0f;
    }

    private void SetAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        currentAlpha = alpha;

        if (progressCanvasGroup != null)
            progressCanvasGroup.alpha = alpha;

        if (progressBackgroundImage != null)
        {
            Color color = progressBackgroundImage.color;
            color.a = alpha;
            progressBackgroundImage.color = color;

            if (!progressBackgroundImage.gameObject.activeSelf && alpha > 0f)
                progressBackgroundImage.gameObject.SetActive(true);
        }
    }
}
