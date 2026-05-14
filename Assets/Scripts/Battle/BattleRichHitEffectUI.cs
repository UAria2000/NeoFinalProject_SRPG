using System;
using UnityEngine;
using UnityEngine.UI;

public class BattleRichHitEffectUI : MonoBehaviour
{
    public enum RotationDirection
    {
        DirectValue,
        Clockwise,
        CounterClockwise
    }

    [Serializable]
    public class ImageLayer
    {
        public Graphic graphic;
        public RectTransform rectTransform;
        public bool animatePosition = true;
        public Vector2 startPosition;
        public Vector2 endPosition;
        public bool animateScale = true;
        public float startScale = 0.4f;
        public float peakScale = 1.2f;
        public float endScale = 1.5f;
        [Range(0.01f, 0.99f)] public float peakScaleAt = 0.28f;
        [Range(0f, 1f)] public float peakScaleHoldUntil;
        public bool animateRotation = true;
        public float startRotation;
        public float endRotation;
        public RotationDirection rotationDirection = RotationDirection.DirectValue;
        [Min(0f)] public float extraRotationTurns;
        [Range(0f, 1f)] public float appearAt;
        [Range(0f, 1f)] public float fadeOutAt = 0.55f;
        public Color startColor = Color.white;
        public bool useFadeIn;
        [Range(0f, 1f)] public float fadeInEndAt = 0.08f;
        public Color peakColor = Color.white;
        public Color endColor = new Color(1f, 1f, 1f, 0f);
        public bool usePulse;
        [Min(0f)] public float pulseScaleAmount;
        [Min(0.01f)] public float pulseFrequency = 2f;
        public bool useWobble;
        public Vector2 wobbleAmount;
        [Min(0.01f)] public float wobbleFrequency = 2f;
        public bool useFlicker;
        [Range(0f, 1f)] public float flickerAlphaAmount;
        [Min(0.01f)] public float flickerFrequency = 3f;
    }

    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private ImageLayer[] imageLayers;
    [SerializeField] private ParticleSystem[] particleSystems;
    [Header("Alpha Controls")]
    [SerializeField, Range(0f, 1f)] private float mainSpriteAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float subSpriteAlpha = 1f;
    [SerializeField, Range(0f, 1f)] private float particleAlpha = 1f;
    [SerializeField] private float duration = 1.25f;
    [SerializeField] private Vector2 spawnOffset;
    [SerializeField] private bool destroyOnComplete = true;

    private float elapsed;
    private Vector2[] baseLayerPositions;
    private float[] baseLayerScales;
    private float[] baseLayerRotations;
    private ParticleSystem.MinMaxGradient[] baseParticleStartColors;

    public float Duration => Mathf.Max(0.01f, duration);
    public Vector2 SpawnOffset => spawnOffset;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        elapsed = 0f;
        CaptureLayerDefaults();
        CaptureParticleDefaults();

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        ApplyLayers(0f);
        ApplyParticleAlpha();
        PlayParticles();
    }

    public void SetDuration(float value)
    {
        duration = Mathf.Max(0.01f, value);
    }

    private void Update()
    {
        float safeDuration = Mathf.Max(0.01f, duration);
        elapsed += Time.deltaTime;

        float t = Mathf.Clamp01(elapsed / safeDuration);
        ApplyLayers(t);

        if (canvasGroup != null)
        {
            float globalFade = Mathf.InverseLerp(0.72f, 1f, t);
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, globalFade);
        }

        if (destroyOnComplete && t >= 1f)
            Destroy(gameObject);
    }

    private void ApplyLayers(float normalizedTime)
    {
        if (imageLayers == null)
            return;

        for (int i = 0; i < imageLayers.Length; i++)
        {
            ImageLayer layer = imageLayers[i];
            if (layer == null || layer.rectTransform == null)
                continue;

            float localT = Mathf.InverseLerp(layer.appearAt, 1f, normalizedTime);
            float easeOut = 1f - Mathf.Pow(1f - localT, 3f);
            float peakScaleAt = Mathf.Clamp(layer.peakScaleAt, 0.01f, 0.99f);
            float popT = Mathf.InverseLerp(0f, peakScaleAt, localT);
            float holdUntil = Mathf.Clamp(layer.peakScaleHoldUntil, peakScaleAt, 0.99f);
            float settleT = Mathf.InverseLerp(holdUntil, 1f, localT);
            float scale = localT < peakScaleAt
                ? Mathf.Lerp(layer.startScale, layer.peakScale, 1f - Mathf.Pow(1f - popT, 2f))
                : localT < holdUntil
                    ? layer.peakScale
                : Mathf.Lerp(layer.peakScale, layer.endScale, settleT);

            Vector2 position = layer.animatePosition
                ? Vector2.Lerp(layer.startPosition, layer.endPosition, easeOut)
                : GetBaseLayerPosition(i);

            if (layer.useWobble)
            {
                float wobble = Mathf.Sin(normalizedTime * Mathf.PI * 2f * layer.wobbleFrequency);
                float counter = Mathf.Cos(normalizedTime * Mathf.PI * 2f * layer.wobbleFrequency * 0.73f);
                position += new Vector2(layer.wobbleAmount.x * wobble, layer.wobbleAmount.y * counter);
            }

            if (layer.animatePosition || layer.useWobble)
                layer.rectTransform.anchoredPosition = position;

            float finalScale = layer.animateScale ? scale : GetBaseLayerScale(i);

            if (layer.usePulse)
            {
                float pulse = Mathf.Sin(normalizedTime * Mathf.PI * 2f * layer.pulseFrequency);
                finalScale *= 1f + layer.pulseScaleAmount * pulse;
            }

            if (layer.animateScale || layer.usePulse)
                layer.rectTransform.localScale = new Vector3(finalScale, finalScale, 1f);

            float rotation = layer.animateRotation ? EvaluateRotation(layer, easeOut) : GetBaseLayerRotation(i);
            layer.rectTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);

            if (layer.graphic == null)
                continue;

            float fade = Mathf.InverseLerp(layer.fadeOutAt, 1f, localT);
            Color visibleColor = layer.useFadeIn ? layer.peakColor : layer.startColor;
            Color color = Color.Lerp(visibleColor, layer.endColor, fade);
            if (layer.useFadeIn && localT < layer.fadeInEndAt)
            {
                float fadeIn = Mathf.InverseLerp(0f, Mathf.Max(0.001f, layer.fadeInEndAt), localT);
                color = Color.Lerp(layer.startColor, layer.peakColor, fadeIn);
            }
            if (layer.useFlicker)
            {
                float flicker = (Mathf.Sin(normalizedTime * Mathf.PI * 2f * layer.flickerFrequency) + 1f) * 0.5f;
                color.a *= 1f - layer.flickerAlphaAmount * flicker;
            }
            color.a *= GetSpriteAlpha(i);
            color.a *= normalizedTime >= layer.appearAt ? 1f : 0f;
            layer.graphic.color = color;
        }
    }

    private float GetSpriteAlpha(int layerIndex)
    {
        return layerIndex == 0 ? mainSpriteAlpha : subSpriteAlpha;
    }

    private static float EvaluateRotation(ImageLayer layer, float t)
    {
        if (layer.rotationDirection == RotationDirection.DirectValue)
            return Mathf.Lerp(layer.startRotation, layer.endRotation, t);

        float start = Mathf.Repeat(layer.startRotation, 360f);
        float end = Mathf.Repeat(layer.endRotation, 360f);
        float turns = Mathf.Max(0f, layer.extraRotationTurns) * 360f;

        if (layer.rotationDirection == RotationDirection.Clockwise)
        {
            float delta = Mathf.Repeat(start - end, 360f) + turns;
            return start - delta * t;
        }

        float counterDelta = Mathf.Repeat(end - start, 360f) + turns;
        return start + counterDelta * t;
    }

    private void CaptureLayerDefaults()
    {
        int length = imageLayers != null ? imageLayers.Length : 0;
        if (baseLayerPositions == null || baseLayerPositions.Length != length)
        {
            baseLayerPositions = new Vector2[length];
            baseLayerScales = new float[length];
            baseLayerRotations = new float[length];
        }

        for (int i = 0; i < length; i++)
        {
            ImageLayer layer = imageLayers[i];
            RectTransform rectTransform = layer != null ? layer.rectTransform : null;
            if (rectTransform == null)
                continue;

            baseLayerPositions[i] = rectTransform.anchoredPosition;
            baseLayerScales[i] = rectTransform.localScale.x;
            baseLayerRotations[i] = rectTransform.localEulerAngles.z;
        }
    }

    private Vector2 GetBaseLayerPosition(int index)
    {
        if (baseLayerPositions == null || index < 0 || index >= baseLayerPositions.Length)
            return Vector2.zero;

        return baseLayerPositions[index];
    }

    private float GetBaseLayerScale(int index)
    {
        if (baseLayerScales == null || index < 0 || index >= baseLayerScales.Length || baseLayerScales[index] <= 0f)
            return 1f;

        return baseLayerScales[index];
    }

    private float GetBaseLayerRotation(int index)
    {
        if (baseLayerRotations == null || index < 0 || index >= baseLayerRotations.Length)
            return 0f;

        return baseLayerRotations[index];
    }

    private void PlayParticles()
    {
        if (particleSystems == null)
            return;

        for (int i = 0; i < particleSystems.Length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null)
                continue;

            ps.Clear(true);
            ps.Play(true);
        }
    }

    private void CaptureParticleDefaults()
    {
        int length = particleSystems != null ? particleSystems.Length : 0;
        if (baseParticleStartColors != null && baseParticleStartColors.Length == length)
            return;

        baseParticleStartColors = new ParticleSystem.MinMaxGradient[length];

        for (int i = 0; i < length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null)
                continue;

            baseParticleStartColors[i] = ps.main.startColor;
        }
    }

    private void ApplyParticleAlpha()
    {
        if (particleSystems == null || baseParticleStartColors == null)
            return;

        int length = Mathf.Min(particleSystems.Length, baseParticleStartColors.Length);
        for (int i = 0; i < length; i++)
        {
            ParticleSystem ps = particleSystems[i];
            if (ps == null)
                continue;

            ParticleSystem.MainModule main = ps.main;
            main.startColor = ScaleGradientAlpha(baseParticleStartColors[i], particleAlpha);
        }
    }

    private static ParticleSystem.MinMaxGradient ScaleGradientAlpha(ParticleSystem.MinMaxGradient gradient, float alphaScale)
    {
        alphaScale = Mathf.Clamp01(alphaScale);

        switch (gradient.mode)
        {
            case ParticleSystemGradientMode.Color:
            {
                gradient.color = ScaleColorAlpha(gradient.color, alphaScale);
                return gradient;
            }
            case ParticleSystemGradientMode.TwoColors:
            {
                gradient.colorMin = ScaleColorAlpha(gradient.colorMin, alphaScale);
                gradient.colorMax = ScaleColorAlpha(gradient.colorMax, alphaScale);
                return gradient;
            }
            case ParticleSystemGradientMode.Gradient:
            {
                gradient.gradient = ScaleGradientAlpha(gradient.gradient, alphaScale);
                return gradient;
            }
            case ParticleSystemGradientMode.TwoGradients:
            {
                gradient.gradientMin = ScaleGradientAlpha(gradient.gradientMin, alphaScale);
                gradient.gradientMax = ScaleGradientAlpha(gradient.gradientMax, alphaScale);
                return gradient;
            }
            default:
                return gradient;
        }
    }

    private static Color ScaleColorAlpha(Color color, float alphaScale)
    {
        color.a *= alphaScale;
        return color;
    }

    private static Gradient ScaleGradientAlpha(Gradient source, float alphaScale)
    {
        if (source == null)
            return null;

        Gradient gradient = new Gradient();
        GradientAlphaKey[] alphaKeys = source.alphaKeys;
        for (int i = 0; i < alphaKeys.Length; i++)
            alphaKeys[i].alpha *= alphaScale;

        gradient.SetKeys(source.colorKeys, alphaKeys);
        gradient.mode = source.mode;
        return gradient;
    }
}
