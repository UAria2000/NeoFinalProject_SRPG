using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBattleCinematicDriver
{
    bool IsCinematicEnabled { get; }
    bool IsCinematicPlaying { get; }

    IEnumerator PlayAttackCinematic(
        BattleUnit actor,
        SkillDefinition skill,
        IList<BattleUnit> targets,
        Sprite attackSprite,
        System.Func<IEnumerator> impactRoutine);

    IEnumerator PlayAttackImpact(
        BattleUnit target,
        SkillDefinition skill,
        AttackResult result,
        float hitDuration,
        float missHoldDuration);

    void PlaySupportImpact(BattleUnit target, SkillDefinition skill);

    void ShowFloatingText(BattleUnit target, string text, Color color, float duration);

    void ShowFloatingTextParts(
        BattleUnit target,
        string title,
        string value,
        Color titleColor,
        Color valueColor,
        float duration);
}
