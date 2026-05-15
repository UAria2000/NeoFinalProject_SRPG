using UnityEngine;

/// <summary>
/// 상태이상/전투 기믹 상태 공통 규칙.
/// </summary>
public static class BattleStatusUtility
{
    public const int MaxStack = 99;
    public const int BleedCurrentHpDamagePercentPerStack = 5;
    public const int BurnIdtPenaltyPercentPerStack = 5;
    public const int BurnMaxHpDamagePercentPerStack = 2;
    public const int FrostAcSpdPenaltyPercentPerStack = 10;
    public const int BlindFinalHitChancePenaltyPercent = 30;
    public const int HuntingTargetBonusCritChancePercentPerStack = 5;
    public const int LifeStealHealPercent = 30;

    public static StatusEffectType Normalize(StatusEffectType statusType)
    {
        return statusType;
    }

    public static bool IsRealAilment(StatusEffectType statusType)
    {
        statusType = Normalize(statusType);
        return statusType == StatusEffectType.Stun ||
               statusType == StatusEffectType.Bleed ||
               statusType == StatusEffectType.Burn ||
               statusType == StatusEffectType.Frost ||
               statusType == StatusEffectType.Blind;
    }

    public static bool IsStackingAilment(StatusEffectType statusType)
    {
        statusType = Normalize(statusType);
        return statusType == StatusEffectType.Bleed ||
               statusType == StatusEffectType.Burn ||
               statusType == StatusEffectType.Frost ||
               statusType == StatusEffectType.Hunting ||
               statusType == StatusEffectType.LifeSteal;
    }

    public static bool IsNonStackingAilment(StatusEffectType statusType)
    {
        statusType = Normalize(statusType);
        return statusType == StatusEffectType.Stun ||
               statusType == StatusEffectType.Blind;
    }

    public static bool IsBattleSpecialState(StatusEffectType statusType)
    {
        return statusType == StatusEffectType.Taunt ||
               statusType == StatusEffectType.CounterStance ||
               statusType == StatusEffectType.DuelArena ||
               statusType == StatusEffectType.Stealth ||
               statusType == StatusEffectType.BattleStance ||
               statusType == StatusEffectType.Marked ||
               statusType == StatusEffectType.Hunting ||
               statusType == StatusEffectType.LifeSteal;
    }

    public static int ClampStack(int stack)
    {
        return Mathf.Clamp(stack, 0, MaxStack);
    }

    public static string GetDisplayName(StatusEffectType statusType)
    {
        statusType = Normalize(statusType);
        switch (statusType)
        {
            case StatusEffectType.Stun: return "기절";
            case StatusEffectType.Bleed: return "출혈";
            case StatusEffectType.Burn: return "화상";
            case StatusEffectType.Frost: return "동상";
            case StatusEffectType.Blind: return "실명";
            case StatusEffectType.Taunt: return "도발";
            case StatusEffectType.CounterStance: return "반격 태세";
            case StatusEffectType.DuelArena: return "결투";
            case StatusEffectType.Stealth: return "은신";
            case StatusEffectType.BattleStance: return "전투 자세";
            case StatusEffectType.Marked: return "표식";
            case StatusEffectType.Hunting: return "사냥 표식";
            case StatusEffectType.LifeSteal: return "흡혈";
            default: return statusType.ToString();
        }
    }


    public static string GetTooltipTitle(StatusEffectType statusType)
    {
        return GetDisplayName(statusType);
    }

    public static string GetStatusTooltipText(BattleUnit unit, StatusEffectType statusType)
    {
        statusType = Normalize(statusType);
        int stack = unit != null ? Mathf.Max(0, unit.GetStatusStackCount(statusType)) : 0;

        switch (statusType)
        {
            case StatusEffectType.Stun:
                return "턴 시작 시 행동을 건너뜁니다.";

            case StatusEffectType.Bleed:
                return $"턴 시작 시 현재 체력의 {stack * BleedCurrentHpDamagePercentPerStack}% 데미지.";

            case StatusEffectType.Burn:
                return $"턴 시작 시 최대 체력의 {stack * BurnMaxHpDamagePercentPerStack}% 데미지, IDT +{stack * BurnIdtPenaltyPercentPerStack}%.";

            case StatusEffectType.Frost:
                return $"AC/SPD -{stack * FrostAcSpdPenaltyPercentPerStack}%.";

            case StatusEffectType.Blind:
                return $"최종 명중률 -{BlindFinalHitChancePenaltyPercent}%.";

            case StatusEffectType.Taunt:
                return "적의 단일 대상 공격을 자신에게 유도합니다.";

            case StatusEffectType.CounterStance:
                return "직접 공격을 받으면 반격합니다.";

            case StatusEffectType.DuelArena:
                return "결투 상대와 위치 이동이 제한됩니다.";

            case StatusEffectType.Stealth:
                return "적의 직접 지정 대상이 되기 어렵습니다.";

            case StatusEffectType.BattleStance:
                return "전투 자세 상태입니다.";

            case StatusEffectType.Marked:
                return "표식 상태입니다.";

            case StatusEffectType.Hunting:
                return $"이 유닛을 공격할 때 치명타 확률 +{stack * HuntingTargetBonusCritChancePercentPerStack}%.";

            case StatusEffectType.LifeSteal:
                return $"공격 시 입힌 HP 피해의 {LifeStealHealPercent}%를 회복합니다.";

            default:
                return string.Empty;
        }
    }

    public static string GetTimedModifierTooltipTitle(BattleTimedModifierInstance modifier)
    {
        if (modifier == null)
            return string.Empty;

        return GetStatModifierDisplayName(modifier.statModifierType);
    }

    public static string GetTimedModifierTooltipText(BattleTimedModifierInstance modifier)
    {
        if (modifier == null)
            return string.Empty;

        string statName = GetStatModifierDisplayName(modifier.statModifierType);
        int magnitude = modifier.magnitude;
        string sign = magnitude > 0 ? "+" : string.Empty;
        string suffix = IsPercentStatModifier(modifier.statModifierType) ? "%" : string.Empty;
        string turns = modifier.remainingTurns > 0 ? $" / 남은 턴 {modifier.remainingTurns}" : string.Empty;

        if (modifier.statModifierType == StatModifierType.IncomingDamageTakenPercent)
            return $"받는 피해 {sign}{magnitude}%{turns}.";

        return $"{statName} {sign}{magnitude}{suffix}{turns}.";
    }

    public static string GetShieldTooltipText(int shieldAmount)
    {
        return $"보호막: HP 피해를 {Mathf.Max(0, shieldAmount)}만큼 대신 막습니다.";
    }

    public static string GetEndTurnGuardTooltipText(int guardPercent)
    {
        return $"방어 태세: 받는 피해 -{Mathf.Max(0, guardPercent)}%.";
    }

    public static string GetEliteTooltipText(int allStatsPercent)
    {
        return $"정예: 주요 능력치 +{Mathf.Max(0, allStatsPercent)}%.";
    }

    public static string GetStatModifierDisplayName(StatModifierType type)
    {
        switch (type)
        {
            case StatModifierType.DMG: return "DMG";
            case StatModifierType.SPD: return "SPD";
            case StatModifierType.HIT: return "HIT";
            case StatModifierType.AC: return "AC";
            case StatModifierType.IDT:
            case StatModifierType.IncomingDamageTakenPercent:
                return "IDT";
            case StatModifierType.CRI: return "CRI";
            case StatModifierType.CRD: return "CRD";
            case StatModifierType.PierceBackOne: return "후열 관통";
            default: return type.ToString();
        }
    }

    private static bool IsPercentStatModifier(StatModifierType type)
    {
        return type == StatModifierType.IDT ||
               type == StatModifierType.CRI ||
               type == StatModifierType.CRD ||
               type == StatModifierType.IncomingDamageTakenPercent;
    }

    public static int GetResistance(BattleUnit unit, StatusEffectType statusType)
    {
        if (unit == null)
            return 0;

        statusType = Normalize(statusType);
        int baseResist;
        switch (statusType)
        {
            case StatusEffectType.Stun: baseResist = unit.StunResist; break;
            case StatusEffectType.Bleed: baseResist = unit.BleedResist; break;
            case StatusEffectType.Burn: baseResist = unit.BurnResist; break;
            case StatusEffectType.Frost: baseResist = unit.FrostResist; break;
            case StatusEffectType.Blind: baseResist = unit.BlindResist; break;
            default: return 0;
        }

        return Mathf.Max(0, baseResist + BattlePassiveController.GetActiveStatusResistanceAuraBonus(unit));
    }
}

/// <summary>
/// 저항 판정 대상이 아닌 전투 기믹 상태를 점진적으로 분리하기 위한 enum.
/// 이번 단계에서는 기존 StatusEffectType.Taunt/CounterStance/DuelArena/Stealth 호환을 유지한다.
/// </summary>
public enum BattleSpecialStateType
{
    None,
    Taunt,
    CounterStance,
    DuelArena,
    Stealth,
    Shield
}
