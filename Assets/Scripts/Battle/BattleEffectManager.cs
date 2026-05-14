using UnityEngine;

public static class BattleEffectManager
{
    private const string DefaultHitEffectRegistryResourcePath = "Battle/HitEffectRegistry";
    private const float DefaultHitEffectDuration = 2f;

    private static HitEffectRegistry cachedDefaultRegistry;

    public static void PlayHitEffect(
        SkillDefinition skill,
        BattleUnitView targetView,
        BattleViewManager viewManager,
        HitEffectRegistry registryOverride = null)
    {
        if (skill == null || targetView == null || viewManager == null)
            return;

        if (!TryResolveHitEffect(skill, registryOverride, out GameObject prefab, out HitEffectAnchorType anchorType, out Vector2 offset, out float duration))
            return;

        Vector3 worldPosition = targetView.GetHitEffectAnchorPosition(anchorType, offset);
        bool mirrorX = targetView.Unit != null && targetView.Unit.Team == TeamType.Ally;
        viewManager.PlayEffect(prefab, worldPosition, duration, mirrorX);
    }

    public static bool TryResolveHitEffect(
        SkillDefinition skill,
        HitEffectRegistry registryOverride,
        out GameObject prefab,
        out HitEffectAnchorType anchorType,
        out Vector2 offset,
        out float duration)
    {
        prefab = null;
        anchorType = HitEffectAnchorType.Default;
        offset = Vector2.zero;
        duration = DefaultHitEffectDuration;

        if (skill == null)
            return false;

        if (skill.hitEffectPrefab != null)
        {
            prefab = skill.hitEffectPrefab;
            anchorType = skill.hitEffectAnchorType;
            duration = ResolveDuration(skill, prefab, DefaultHitEffectDuration);
            return true;
        }

        HitEffectRegistry registry = registryOverride != null ? registryOverride : GetDefaultRegistry();
        if (registry == null || !registry.TryGetEntry(skill.hitEffectType, out HitEffectRegistry.Entry entry))
            return false;

        prefab = entry.prefab;
        anchorType = entry.anchorType;
        offset = entry.offset;
        duration = ResolveDuration(skill, prefab, entry.duration > 0f ? entry.duration : DefaultHitEffectDuration);
        return prefab != null;
    }

    private static float ResolveDuration(SkillDefinition skill, GameObject prefab, float fallbackDuration)
    {
        if (skill != null && skill.hitEffectDurationOverride > 0f)
            return skill.GetHitEffectDurationOverride(fallbackDuration);

        if (prefab != null && prefab.TryGetComponent(out BattleRichHitEffectUI richEffect))
            return richEffect.Duration;

        return Mathf.Max(0.01f, fallbackDuration);
    }

    private static HitEffectRegistry GetDefaultRegistry()
    {
        if (cachedDefaultRegistry == null)
            cachedDefaultRegistry = Resources.Load<HitEffectRegistry>(DefaultHitEffectRegistryResourcePath);

        return cachedDefaultRegistry;
    }
}
