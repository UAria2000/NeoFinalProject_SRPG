using UnityEngine;
using UnityEngine.UI;

public class DebugBattleHitEffectUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform[] scaleTargets;
    [SerializeField] private RectTransform[] sprayTargets;
    [SerializeField] private Vector2[] sprayEndOffsets;
    [SerializeField] private float duration = 1.12f;
    [SerializeField] private float startScale = 0.55f;
    [SerializeField] private float endScale = 1.35f;
    [SerializeField, Range(0f, 1f)] private float fadeStartNormalized = 0.45f;
    [SerializeField] private float sprayStartScale = 0.28f;
    [SerializeField] private float sprayEndScale = 1f;

    private float elapsed;
    private Vector2[] sprayStartPositions;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        Image[] images = GetComponentsInChildren<Image>(true);
        if (scaleTargets == null || scaleTargets.Length == 0)
        {
            scaleTargets = new RectTransform[images.Length];
            for (int i = 0; i < images.Length; i++)
                scaleTargets[i] = images[i] != null ? images[i].rectTransform : null;
        }

        CacheSprayStartPositions();
    }

    private void OnEnable()
    {
        elapsed = 0f;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        ApplyScale(startScale);
        ResetSprayTargets();
    }

    private void Update()
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / safeDuration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        ApplyScale(Mathf.Lerp(startScale, endScale, eased));
        ApplySprayMotion(t);

        if (canvasGroup != null)
        {
            float fadeStart = Mathf.Clamp01(fadeStartNormalized);
            float fadeProgress = Mathf.InverseLerp(fadeStart, 1f, t);
            canvasGroup.alpha = t < fadeStart ? 1f : Mathf.Lerp(1f, 0f, fadeProgress);
        }

        if (t >= 1f)
            Destroy(gameObject);
    }

    private void ApplyScale(float scale)
    {
        if (scaleTargets == null)
            return;

        for (int i = 0; i < scaleTargets.Length; i++)
        {
            if (scaleTargets[i] != null)
                scaleTargets[i].localScale = new Vector3(scale, scale, 1f);
        }
    }

    private void CacheSprayStartPositions()
    {
        if (sprayTargets == null || sprayTargets.Length == 0)
        {
            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
            System.Collections.Generic.List<RectTransform> found = new System.Collections.Generic.List<RectTransform>();
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i] != null && rects[i] != transform && rects[i].name.StartsWith("BloodSpray"))
                    found.Add(rects[i]);
            }

            sprayTargets = found.ToArray();
        }

        if (sprayTargets == null)
            return;

        sprayStartPositions = new Vector2[sprayTargets.Length];
        for (int i = 0; i < sprayTargets.Length; i++)
        {
            if (sprayTargets[i] != null)
                sprayStartPositions[i] = sprayTargets[i].anchoredPosition;
        }
    }

    private void ResetSprayTargets()
    {
        CacheSprayStartPositions();

        if (sprayTargets == null)
            return;

        for (int i = 0; i < sprayTargets.Length; i++)
        {
            RectTransform target = sprayTargets[i];
            if (target == null)
                continue;

            target.anchoredPosition = sprayStartPositions != null && i < sprayStartPositions.Length
                ? sprayStartPositions[i]
                : target.anchoredPosition;
            target.localScale = new Vector3(sprayStartScale, sprayStartScale, 1f);
        }
    }

    private void ApplySprayMotion(float normalizedTime)
    {
        if (sprayTargets == null || sprayTargets.Length == 0)
            return;

        float motionT = Mathf.Clamp01(normalizedTime / 0.72f);
        float eased = 1f - Mathf.Pow(1f - motionT, 3f);
        float scale = Mathf.Lerp(sprayStartScale, sprayEndScale, eased);

        for (int i = 0; i < sprayTargets.Length; i++)
        {
            RectTransform target = sprayTargets[i];
            if (target == null)
                continue;

            Vector2 start = sprayStartPositions != null && i < sprayStartPositions.Length
                ? sprayStartPositions[i]
                : target.anchoredPosition;
            Vector2 offset = sprayEndOffsets != null && i < sprayEndOffsets.Length
                ? sprayEndOffsets[i]
                : Vector2.zero;

            target.anchoredPosition = start + offset * eased;
            target.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
