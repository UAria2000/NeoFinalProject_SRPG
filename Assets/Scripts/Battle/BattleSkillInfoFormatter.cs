using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class SkillEffectDisplayEntry
{
    public Sprite icon;
    public string text;

    public SkillEffectDisplayEntry(Sprite icon, string text)
    {
        this.icon = icon;
        this.text = text;
    }
}

public static class BattleSkillInfoFormatter
{
    public static SkillClass GetSkillClass(SkillDefinition skill)
    {
        if (skill == null)
            return SkillClass.Melee;

        int raw = (int)skill.skillClass;

        if ((raw & (int)SkillClass.Unique) != 0) return SkillClass.Unique;
        if ((raw & (int)SkillClass.Common) != 0) return SkillClass.Common;
        if ((raw & (int)SkillClass.Melee) != 0) return SkillClass.Melee;
        if ((raw & (int)SkillClass.Mid) != 0) return SkillClass.Mid;
        if ((raw & (int)SkillClass.Ranged) != 0) return SkillClass.Ranged;

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
            case SkillClass.Unique: return "고유";
            case SkillClass.Common: return "공통";
            case SkillClass.Mid: return "미드";
            case SkillClass.Ranged: return "레인지";
            default: return "밀리";
        }
    }

    public static string GetDescriptionValueText(SkillDefinition skill)
    {
        if (skill == null || string.IsNullOrWhiteSpace(skill.description))
            return string.Empty;

        return skill.description.Trim();
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

        sb.AppendLine(GetSuccessText(skill));
        sb.AppendLine(GetCooldownText(skill));
        sb.Append(GetUnifiedEffectText(skill));
        return sb.ToString();
    }

    public static string GetPowerText(SkillDefinition skill)
    {
        return "위력: " + GetPowerValueText(skill);
    }

    public static string GetPowerValueText(SkillDefinition skill)
    {
        // 새 UI에서는 위력/효과를 분리하지 않고 효과 목록 하나에 통합한다.
        // 기존 Text 필드 호환을 위해 값만 필요한 경우에도 통합 효과 문자열을 반환한다.
        return GetUnifiedEffectValueText(skill);
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
        return GetUnifiedEffectText(skill);
    }

    public static string GetEffectValueText(SkillDefinition skill)
    {
        return GetUnifiedEffectValueText(skill);
    }

    public static string GetUnifiedEffectText(SkillDefinition skill)
    {
        return "효과: " + GetUnifiedEffectValueText(skill);
    }

    public static string GetUnifiedEffectValueText(SkillDefinition skill)
    {
        List<SkillEffectDisplayEntry> entries = GetUnifiedEffectEntries(skill);
        if (entries.Count <= 0)
            return "-";

        List<string> texts = new List<string>();
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && !string.IsNullOrWhiteSpace(entries[i].text))
                texts.Add(entries[i].text);
        }

        return texts.Count > 0 ? string.Join(", ", texts) : "-";
    }

    public static List<SkillEffectDisplayEntry> GetUnifiedEffectEntries(SkillDefinition skill)
    {
        List<SkillEffectDisplayEntry> entries = new List<SkillEffectDisplayEntry>();
        if (skill == null)
            return entries;

        if (skill.effects != null)
        {
            for (int i = 0; i < skill.effects.Count; i++)
            {
                BattleEffectBlock block = skill.effects[i];
                if (block == null)
                    continue;

                AddEffectEntry(entries, block.displayIcon, FormatEffectBlock(skill, block));
            }
        }

        AddGimmickEntries(entries, skill);
        return entries;
    }

    private static void AddGimmickEntries(List<SkillEffectDisplayEntry> entries, SkillDefinition skill)
    {
        if (skill == null)
            return;

        switch (skill.activeGimmick)
        {
            case ActiveSkillGimmick.DelayedReinforcement:
                AddEffectEntry(entries, null, "지연 증원");
                break;
            case ActiveSkillGimmick.BleedDrainStrike:
                if (!HasEffectOfKind(skill, BattleEffectKind.Heal))
                    AddEffectEntry(entries, null, "준 HP 피해 회복");
                break;
            case ActiveSkillGimmick.ForceMoveTargetToRankAfterHit:
                AddEffectEntry(entries, null, string.Format("대상 {0}열 이동", Mathf.Clamp(skill.forcedTargetMoveToRank, 1, 4)));
                break;
            case ActiveSkillGimmick.PushTargetBackwardAfterHit:
                if (skill.pushBackFailFinalPowerPercent > 0f)
                    AddEffectEntry(entries, null, string.Format("대상 뒤로 {0}칸, 이동 면역 시 피해 {1}%", Mathf.Max(1, skill.forcedTargetMoveSteps), FormatPercentNumber(skill.pushBackFailFinalPowerPercent)));
                else
                    AddEffectEntry(entries, null, string.Format("대상 뒤로 {0}칸", Mathf.Max(1, skill.forcedTargetMoveSteps)));
                break;
            case ActiveSkillGimmick.PullTargetForwardAfterHit:
                AddEffectEntry(entries, null, string.Format("대상 앞으로 {0}칸", Mathf.Max(1, skill.forcedTargetMoveSteps)));
                break;
            case ActiveSkillGimmick.AbyssReboundSelfRecoil20FromTotalDamage:
                AddEffectEntry(entries, null, "심연 반동");
                break;
            case ActiveSkillGimmick.BlackArenaDuel2Turns:
                AddEffectEntry(entries, null, string.Format("결투 {0}턴", Mathf.Max(1, skill.blackArenaDuelDurationTurns)));
                break;
            case ActiveSkillGimmick.FleeOnNextOwnTurn:
                AddEffectEntry(entries, null, "다음 자기 턴 도주");
                break;
            case ActiveSkillGimmick.RandomRepositionTargetsOnHit:
                AddEffectEntry(entries, null, string.Format("무작위 위치 이동 {0}%", FormatPercentNumber(skill.randomRepositionChancePercent)));
                break;
            case ActiveSkillGimmick.ImmediateSummonInFront:
                AddEffectEntry(entries, null, "본인 앞 소환");
                break;
            case ActiveSkillGimmick.ShieldSelfFromDamageDealt:
                AddEffectEntry(entries, null, string.Format("준 HP 피해의 {0}% 보호막", FormatPercentNumber(skill.selfShieldFromDamageDealtPercent)));
                break;
            case ActiveSkillGimmick.ChainLightning:
                AddEffectEntry(entries, null, string.Format("연쇄 피해 {0}% / {1}%", FormatPercentNumber(skill.chainLightningFirstJumpPowerPercent), FormatPercentNumber(skill.chainLightningSecondJumpPowerPercent)));
                break;
            case ActiveSkillGimmick.ChainExecutionOnce:
                AddEffectEntry(entries, null, "처치 시 1회 연쇄");
                break;
        }

        if (skill.HasSelfMoveAfterUse())
        {
            string direction = skill.selfMoveDirection == SkillSelfMoveDirection.Forward ? "사용자 전진" : "사용자 후퇴";
            AddEffectEntry(entries, null, string.Format("{0} {1}칸", direction, Mathf.Max(1, skill.selfMoveSteps)));
        }

        if (skill.HasSelfStatusAfterUse())
            AddEffectEntry(entries, null, "자가 " + FormatStatus(skill.selfApplyStatusAfterUse, skill.selfApplyStatusDurationTurns, 100f));

        if (skill.alsoApplyToSelfWhenTargetingAlly)
            AddEffectEntry(entries, null, "자신에게도 적용");

        if (skill.disableAfterUseInBattle)
            AddEffectEntry(entries, null, "전투당 1회");
    }

    private static string FormatEffectBlock(SkillDefinition skill, BattleEffectBlock block)
    {
        if (block == null)
            return string.Empty;

        switch (block.kind)
        {
            case BattleEffectKind.Damage:
                return "피해 " + FormatEffectPower(block);
            case BattleEffectKind.Heal:
                if (IsDrainSkill(skill))
                    return string.Format("준 HP 피해의 {0}% 회복", FormatPercentNumber(block.powerPercent));
                return "회복 " + FormatEffectPower(block);
            case BattleEffectKind.Shield:
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
            direction = isBuff ? "방어 증가" : "방어 감소";

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

        string basis = block.valueReference == EffectValueReference.TargetMaxHP ? "HP" : "DMG";
        if (block.useRandomPowerPercentRange)
            return string.Format("{0} {1}~{2}%", basis, block.GetMinPowerPercent(), block.GetMaxPowerPercent());

        return string.Format("{0} {1}%", basis, FormatPercentNumber(block.powerPercent));
    }

    public static string GetStatModifierLabel(StatModifierType type)
    {
        switch (type)
        {
            case StatModifierType.DMG: return "피해";
            case StatModifierType.SPD: return "속도";
            case StatModifierType.HIT: return "명중";
            case StatModifierType.AC: return "회피";
            case StatModifierType.IDT: return "방어";
            case StatModifierType.CRI: return "치명";
            case StatModifierType.CRD: return "치명피해";
            case StatModifierType.IncomingDamageTakenPercent: return "방어";
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

    private static void AddEffectEntry(List<SkillEffectDisplayEntry> entries, Sprite icon, string value)
    {
        if (entries == null || string.IsNullOrWhiteSpace(value))
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i] != null && entries[i].text == value && entries[i].icon == icon)
                return;
        }

        entries.Add(new SkillEffectDisplayEntry(icon, value));
    }

    private static string FormatPercentNumber(float value)
    {
        return Mathf.Approximately(value, Mathf.Round(value))
            ? Mathf.RoundToInt(value).ToString()
            : value.ToString("0.#");
    }
}
