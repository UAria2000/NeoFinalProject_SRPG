using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class BattleSkillInfoFormatter
{
    public static SkillClass GetSkillClass(SkillDefinition skill)
    {
        if (skill == null)
            return SkillClass.Melee;

        int raw = (int)skill.skillClass;

        // Former learnTags was a flags enum. If an old asset still contains multiple bits,
        // normalize it into one display class using this priority.
        if ((raw & (int)SkillClass.Unique) != 0) return SkillClass.Unique;
        if ((raw & (int)SkillClass.Common) != 0) return SkillClass.Common;
        if ((raw & (int)SkillClass.Melee) != 0) return SkillClass.Melee;
        if ((raw & (int)SkillClass.Mid) != 0) return SkillClass.Mid;
        if ((raw & (int)SkillClass.Ranged) != 0) return SkillClass.Ranged;

        // Old None/empty data falls back to the unit-style range tag so existing assets do not show blank badges.
        switch (skill.rangeTag)
        {
            case CharacterRangeType.Mid:
                return SkillClass.Mid;
            case CharacterRangeType.Ranged:
                return SkillClass.Ranged;
            default:
                return SkillClass.Melee;
        }
    }

    public static string GetSkillClassLabel(SkillDefinition skill)
    {
        return GetSkillClassLabel(GetSkillClass(skill));
    }

    public static string GetSkillClassLabel(SkillClass skillClass)
    {
        switch (skillClass)
        {
            case SkillClass.Unique:
                return "고유";
            case SkillClass.Common:
                return "공통";
            case SkillClass.Mid:
                return "미드";
            case SkillClass.Ranged:
                return "레인지";
            default:
                return "밀리";
        }
    }

    public static string GetDescriptionValueText(SkillDefinition skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.description))
            return string.Empty;

        string description = skill.description.Trim();

        // The generated ally/enemy assets currently store mechanical summaries in description
        // (for example: "피해 120%. 준 HP 피해의 25% 회복...").
        // The detail panel already has separate Power / Accuracy / Cooldown / Effect fields,
        // so showing that text again makes power and effects look mixed. Hide obvious mechanical summaries here.
        return LooksLikeMechanicalSummary(description) ? string.Empty : description;
    }

    public static string GetTooltipBodyText(SkillDefinition skill)
    {
        if (skill == null)
            return string.Empty;

        StringBuilder sb = new StringBuilder();

        string description = GetDescriptionValueText(skill);
        if (!string.IsNullOrWhiteSpace(description))
        {
            sb.AppendLine(description);
            sb.AppendLine();
        }

        sb.AppendLine(GetPowerText(skill));
        sb.AppendLine(GetSuccessText(skill));
        sb.AppendLine(GetCooldownText(skill));
        sb.Append(GetEffectText(skill));
        return sb.ToString();
    }

    public static string GetPowerText(SkillDefinition skill)
    {
        return "위력: " + GetPowerValueText(skill);
    }

    public static string GetPowerValueText(SkillDefinition skill)
    {
        if (skill == null || skill.effects == null || skill.effects.Count == 0)
            return "-";

        // Priority: if the skill deals damage, Power should show the main damage coefficient only.
        // Heal/drain/shield riders are effects, not the main power line.
        List<string> damageEntries = new List<string>();
        for (int i = 0; i < skill.effects.Count; i++)
        {
            BattleEffectBlock block = skill.effects[i];
            if (block == null || block.kind != BattleEffectKind.Damage)
                continue;

            damageEntries.Add("피해 " + FormatEffectPower(block));
        }

        if (damageEntries.Count > 0)
            return string.Join(" / ", damageEntries);

        // Non-damaging skills may use Power to show heal/shield magnitude.
        List<string> supportEntries = new List<string>();
        for (int i = 0; i < skill.effects.Count; i++)
        {
            BattleEffectBlock block = skill.effects[i];
            if (block == null)
                continue;

            switch (block.kind)
            {
                case BattleEffectKind.Heal:
                    supportEntries.Add("회복 " + FormatEffectPower(block));
                    break;
                case BattleEffectKind.Shield:
                    supportEntries.Add("보호막 " + FormatEffectPower(block));
                    break;
            }
        }

        return supportEntries.Count > 0 ? string.Join(" / ", supportEntries) : "-";
    }

    public static string GetSuccessText(SkillDefinition skill)
    {
        if (skill == null)
            return "성공률: -";

        string label = skill.resolutionMode == SkillResolutionMode.Attack || skill.HasDamageEffect() ? "명중률" : "성공률";
        return label + ": " + GetSuccessValueText(skill);
    }

    public static string GetSuccessValueText(SkillDefinition skill)
    {
        if (skill == null)
            return "-";

        if (skill.resolutionMode == SkillResolutionMode.Attack || skill.HasDamageEffect())
            return string.Format("{0}%", FormatPercentNumber(skill.accuracyCoefficientPercent));

        float success = 100f;
        bool found = false;
        if (skill.effects != null)
        {
            for (int i = 0; i < skill.effects.Count; i++)
            {
                BattleEffectBlock block = skill.effects[i];
                if (block == null)
                    continue;

                found = true;
                success = Mathf.Min(success, block.successChancePercent);
            }
        }

        return found ? string.Format("{0}%", FormatPercentNumber(success)) : "100%";
    }

    public static string GetCooldownText(SkillDefinition skill)
    {
        return "쿨타임: " + GetCooldownValueText(skill);
    }

    public static string GetCooldownValueText(SkillDefinition skill)
    {
        if (skill == null)
            return "-";

        return skill.cooldownTurns > 0 ? string.Format("{0}턴", skill.cooldownTurns) : "없음";
    }

    public static string GetEffectText(SkillDefinition skill)
    {
        return "효과: " + GetEffectValueText(skill);
    }

    public static string GetEffectValueText(SkillDefinition skill)
    {
        if (skill == null)
            return "-";

        bool hasDamage = skill.HasDamageEffect();
        List<string> entries = new List<string>();

        if (skill.effects != null)
        {
            for (int i = 0; i < skill.effects.Count; i++)
            {
                BattleEffectBlock block = skill.effects[i];
                if (block == null)
                    continue;

                AddEffectEntry(entries, FormatEffectBlockForEffectLine(skill, block, hasDamage));
            }
        }

        switch (skill.activeGimmick)
        {
            case ActiveSkillGimmick.DelayedReinforcement:
                AddEffectEntry(entries, "지연 증원");
                break;
            case ActiveSkillGimmick.BleedDrainStrike:
                if (!HasEffectOfKind(skill, BattleEffectKind.Heal))
                    AddEffectEntry(entries, "준 HP 피해 회복");
                break;
            case ActiveSkillGimmick.ForceMoveTargetToRankAfterHit:
                AddEffectEntry(entries, string.Format("대상 {0}열 이동", Mathf.Clamp(skill.forcedTargetMoveToRank, 1, 4)));
                break;
            case ActiveSkillGimmick.PushTargetBackwardAfterHit:
                if (skill.pushBackFailFinalPowerPercent > 0f)
                    AddEffectEntry(entries, string.Format("대상 뒤로 {0}칸, 이동 면역 시 피해 {1}%", Mathf.Max(1, skill.forcedTargetMoveSteps), FormatPercentNumber(skill.pushBackFailFinalPowerPercent)));
                else
                    AddEffectEntry(entries, string.Format("대상 뒤로 {0}칸", Mathf.Max(1, skill.forcedTargetMoveSteps)));
                break;
            case ActiveSkillGimmick.PullTargetForwardAfterHit:
                AddEffectEntry(entries, string.Format("대상 앞으로 {0}칸", Mathf.Max(1, skill.forcedTargetMoveSteps)));
                break;
            case ActiveSkillGimmick.AbyssReboundSelfRecoil20FromTotalDamage:
                AddEffectEntry(entries, "심연 반동");
                break;
            case ActiveSkillGimmick.BlackArenaDuel2Turns:
                AddEffectEntry(entries, string.Format("결투 {0}턴", Mathf.Max(1, skill.blackArenaDuelDurationTurns)));
                break;
            case ActiveSkillGimmick.FleeOnNextOwnTurn:
                AddEffectEntry(entries, "다음 자기 턴 도주");
                break;
            case ActiveSkillGimmick.RandomRepositionTargetsOnHit:
                AddEffectEntry(entries, string.Format("무작위 위치 이동 {0}%", FormatPercentNumber(skill.randomRepositionChancePercent)));
                break;
            case ActiveSkillGimmick.ImmediateSummonInFront:
                AddEffectEntry(entries, "본인 앞 소환");
                break;
            case ActiveSkillGimmick.ShieldSelfFromDamageDealt:
                AddEffectEntry(entries, string.Format("준 HP 피해의 {0}% 보호막", FormatPercentNumber(skill.selfShieldFromDamageDealtPercent)));
                break;
            case ActiveSkillGimmick.ChainLightning:
                AddEffectEntry(entries, string.Format("연쇄 피해 {0}% / {1}%", FormatPercentNumber(skill.chainLightningFirstJumpPowerPercent), FormatPercentNumber(skill.chainLightningSecondJumpPowerPercent)));
                break;
            case ActiveSkillGimmick.ChainExecutionOnce:
                AddEffectEntry(entries, "처치 시 1회 연쇄");
                break;
        }

        if (skill.HasSelfMoveAfterUse())
        {
            string direction = skill.selfMoveDirection == SkillSelfMoveDirection.Forward ? "사용자 전진" : "사용자 후퇴";
            AddEffectEntry(entries, string.Format("{0} {1}칸", direction, Mathf.Max(1, skill.selfMoveSteps)));
        }

        if (skill.HasSelfStatusAfterUse())
            AddEffectEntry(entries, "자가 " + FormatStatus(skill.selfApplyStatusAfterUse, skill.selfApplyStatusDurationTurns, 100f));

        if (skill.alsoApplyToSelfWhenTargetingAlly)
            AddEffectEntry(entries, "자신에게도 적용");

        if (skill.disableAfterUseInBattle)
            AddEffectEntry(entries, "전투당 1회");

        return entries.Count > 0 ? string.Join(", ", entries) : "-";
    }

    private static string FormatEffectBlockForEffectLine(SkillDefinition skill, BattleEffectBlock block, bool hasDamage)
    {
        if (block == null)
            return string.Empty;

        switch (block.kind)
        {
            case BattleEffectKind.Damage:
                // Damage coefficient belongs to Power.
                return string.Empty;

            case BattleEffectKind.Heal:
                // For pure support heals, magnitude belongs to Power. For damage+drain, it is a rider effect.
                if (!hasDamage)
                    return string.Empty;
                if (IsDrainSkill(skill))
                    return string.Format("준 HP 피해의 {0}% 회복", FormatPercentNumber(block.powerPercent));
                return "회복 " + FormatEffectPower(block);

            case BattleEffectKind.Shield:
                // For pure shield skills, magnitude belongs to Power. For damage+shield riders, show it as effect.
                if (!hasDamage)
                    return string.Empty;
                return "보호막 " + FormatEffectPower(block);

            case BattleEffectKind.Buff:
                return FormatTimedModifier(block, true);

            case BattleEffectKind.Debuff:
                return FormatTimedModifier(block, false);

            case BattleEffectKind.ApplyStatus:
                return FormatStatus(block.statusType, block.durationTurns, block.successChancePercent);

            case BattleEffectKind.RemoveStatus:
                return BattleStatusUtility.GetDisplayName(block.statusType) + " 해제";

            default:
                return string.Empty;
        }
    }

    private static string FormatTimedModifier(BattleEffectBlock block, bool isBuff)
    {
        string stat = GetStatModifierLabel(block.statModifierType);
        int amount = Mathf.Abs(block.flatValue);
        string direction = isBuff ? "증가" : "감소";

        if (block.statModifierType == StatModifierType.IncomingDamageTakenPercent)
            direction = isBuff ? "받는 피해 감소" : "받는 피해 증가";

        string turnText = block.durationTurns > 0 ? string.Format(" {0}턴", block.durationTurns) : string.Empty;

        if (amount > 0 && block.statModifierType != StatModifierType.IncomingDamageTakenPercent)
            return string.Format("{0} {1}% {2}{3}", stat, amount, direction, turnText);

        if (amount > 0)
            return string.Format("{0} {1}%{2}", direction, amount, turnText);

        return stat + " " + direction + turnText;
    }

    private static string FormatStatus(StatusEffectType statusType, int durationTurns, float chancePercent)
    {
        string label = BattleStatusUtility.GetDisplayName(statusType);
        string chance = chancePercent < 100f ? string.Format(" {0}%", FormatPercentNumber(chancePercent)) : string.Empty;
        string duration = durationTurns > 0 ? string.Format(" {0}턴/스택", durationTurns) : string.Empty;
        return label + chance + duration;
    }

    private static string FormatEffectPower(BattleEffectBlock block)
    {
        if (block == null)
            return "-";

        if (block.flatValue > 0)
            return block.flatValue.ToString();

        string basis = block.valueReference == EffectValueReference.TargetMaxHP ? "대상 최대 HP" : "DMG";
        if (block.useRandomPowerPercentRange)
            return string.Format("{0} {1}~{2}%", basis, block.GetMinPowerPercent(), block.GetMaxPowerPercent());

        return string.Format("{0} {1}%", basis, FormatPercentNumber(block.powerPercent));
    }

    private static string GetStatModifierLabel(StatModifierType type)
    {
        switch (type)
        {
            case StatModifierType.DMG: return "DMG";
            case StatModifierType.SPD: return "SPD";
            case StatModifierType.HIT: return "HIT";
            case StatModifierType.AC: return "AC";
            case StatModifierType.IDT: return "IDT";
            case StatModifierType.CRI: return "CRI";
            case StatModifierType.CRD: return "CRD";
            case StatModifierType.IncomingDamageTakenPercent: return "IDT";
            case StatModifierType.PierceBackOne: return "관통";
            default: return "효과";
        }
    }

    private static bool IsDrainSkill(SkillDefinition skill)
    {
        return skill != null && skill.activeGimmick == ActiveSkillGimmick.BleedDrainStrike;
    }

    private static bool HasEffectOfKind(SkillDefinition skill, BattleEffectKind kind)
    {
        if (skill == null || skill.effects == null)
            return false;

        for (int i = 0; i < skill.effects.Count; i++)
        {
            if (skill.effects[i] != null && skill.effects[i].kind == kind)
                return true;
        }

        return false;
    }

    private static void AddEffectEntry(List<string> entries, string value)
    {
        if (entries == null || string.IsNullOrWhiteSpace(value))
            return;

        if (!entries.Contains(value))
            entries.Add(value);
    }

    private static string FormatPercentNumber(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.#");
    }

    private static bool LooksLikeMechanicalSummary(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        string compact = value.Replace(" ", string.Empty);
        int score = 0;

        if (compact.StartsWith("피해") || compact.StartsWith("적") || compact.StartsWith("자신") || compact.StartsWith("대상"))
            score++;
        if (compact.Contains("피해") || compact.Contains("회복") || compact.Contains("보호막") || compact.Contains("기절") || compact.Contains("출혈") || compact.Contains("화상") || compact.Contains("동상") || compact.Contains("실명"))
            score++;
        if (compact.Contains("%") || compact.Contains("턴") || compact.Contains("스택") || compact.Contains("DMG") || compact.Contains("HP"))
            score++;
        if (compact.Contains("쿨타임") || compact.Contains("명중") || compact.Contains("계수") || compact.Contains("확률"))
            score++;

        return score >= 2;
    }
}
