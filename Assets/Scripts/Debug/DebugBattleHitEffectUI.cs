using UnityEngine;
using UnityEngine.UI;

public class DebugBattleHitEffectUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private RectTransform[] scaleTargets;
    [SerializeField] private float duration = 0.42f;
    [SerializeField] private float startScale = 0.55f;
    [SerializeField] private float endScale = 1.35f;

    private float elapsed;

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
    }

    private void OnEnable()
    {
        elapsed = 0f;
        if (canvasGroup != null)
            canvasGroup.alpha = 1f;
        ApplyScale(startScale);
    }

    private void Update()
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / safeDuration);
        float eased = 1f - Mathf.Pow(1f - t, 3f);
        ApplyScale(Mathf.Lerp(startScale, endScale, eased));

        if (canvasGroup != null)
            canvasGroup.alpha = t < 0.35f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.35f) / 0.65f);

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
}
