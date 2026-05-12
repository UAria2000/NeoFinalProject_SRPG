using System.Collections.Generic;
using UnityEngine;

public partial class DebugBattleSceneController
{
    private const float SetupOuterPadding = 8f;
    private const float FormationGap = 14f;
    private const float FormationPadding = 6f;
    private const float CardGap = 8f;
    private const float HeaderHeight = 30f;
    private const float FormationHeaderHeight = 28f;
    private const float CardHeight = 350f;
    private const float ButtonHeight = 24f;
    private const float TextGap = 4f;
    private const int VisibleSkillSlotCount = 4;

    private void OnGUI()
    {
        EnsureSlotsInitialized();

        if (!showSetupPanel)
        {
            if (debugBattleRunning)
            {
                DrawRuntimeDebugControls();
                return;
            }

            if (GUI.Button(new Rect(12, 12, 150, 32), "Battle Setup"))
                showSetupPanel = true;
            return;
        }

        float cardWidth = CalculateCardWidth();
        float formationWidth = GetFormationPanelWidth(cardWidth);
        float setupWidth = formationWidth * 2f + FormationGap + SetupOuterPadding * 2f;
        float setupHeight = HeaderHeight + FormationHeaderHeight + CardHeight + SetupOuterPadding * 2f;
        Rect setupRect = new Rect(
            Mathf.Round((Screen.width - setupWidth) * 0.5f),
            Mathf.Round(Screen.height - setupHeight - 12f),
            Mathf.Round(setupWidth),
            Mathf.Round(setupHeight));

        GUI.Box(setupRect, GUIContent.none);
        DrawSetupHeader(setupRect);

        float formationY = setupRect.y + SetupOuterPadding + HeaderHeight;
        Rect allyRect = new Rect(setupRect.x + SetupOuterPadding, formationY, formationWidth, FormationHeaderHeight + CardHeight);
        Rect enemyRect = new Rect(allyRect.xMax + FormationGap, formationY, formationWidth, FormationHeaderHeight + CardHeight);

        DrawFormationPanel(allyRect, "Ally Formation", allySlots, allyUnitDefinitions, true, cardWidth);
        DrawFormationPanel(enemyRect, "Enemy Formation", enemySlots, enemyUnitDefinitions, false, cardWidth);

        if (picker.IsOpen)
            DrawPickerWindow();

        if (captureGameViewOnPlay && !gameViewCaptureQueued && Event.current.type == EventType.Repaint)
        {
            gameViewCaptureQueued = true;
            StartCoroutine(CaptureGameViewAfterFrame());
        }
    }

    private static float GetFormationPanelWidth(float cardWidth)
    {
        return FormationPadding * 2f + cardWidth * 4f + CardGap * 3f;
    }

    private static float CalculateCardWidth()
    {
        const float preferredCardWidth = 170f;
        const float minimumCardWidth = 118f;

        float availableWidth = Mathf.Max(720f, Screen.width - SetupOuterPadding * 2f);
        float preferredTotalWidth = GetFormationPanelWidth(preferredCardWidth) * 2f + FormationGap + SetupOuterPadding * 2f;
        if (preferredTotalWidth <= availableWidth)
            return preferredCardWidth;

        float cardWidth = (availableWidth - SetupOuterPadding * 2f - FormationGap - FormationPadding * 4f - CardGap * 6f) / 8f;
        return Mathf.Floor(Mathf.Max(minimumCardWidth, cardWidth));
    }

    private void DrawSetupHeader(Rect setupRect)
    {
        Rect headerRect = new Rect(setupRect.x + SetupOuterPadding, setupRect.y + SetupOuterPadding, setupRect.width - SetupOuterPadding * 2f, HeaderHeight);
        GUI.Label(new Rect(headerRect.x, headerRect.y + 5f, 260f, 20f), "Original Battle Debug Setup");

        float x = headerRect.xMax;
        x -= 70f;
        if (GUI.Button(new Rect(x, headerRect.y + 1f, 70f, ButtonHeight), "Hide"))
            showSetupPanel = false;
        x -= 6f + 110f;
        if (GUI.Button(new Rect(x, headerRect.y + 1f, 110f, ButtonHeight), "Reset Setup"))
            ResetSetup();
        x -= 6f + 130f;

        bool previousEnabled = GUI.enabled;
        GUI.enabled = battleManager != null && HasAnyEnabled(allySlots) && HasAnyEnabled(enemySlots);
        if (GUI.Button(new Rect(x, headerRect.y + 1f, 130f, ButtonHeight), "Start Battle"))
            StartDebugBattle();
        GUI.enabled = previousEnabled;
    }

    private void DrawFormationPanel(Rect panelRect, string title, DebugSlot[] slots, UnitDefinition[] unitPool, bool isAlly, float cardWidth)
    {
        GUI.Box(panelRect, GUIContent.none);

        Rect headerRect = new Rect(panelRect.x + FormationPadding, panelRect.y, panelRect.width - FormationPadding * 2f, FormationHeaderHeight);
        GUI.Label(new Rect(headerRect.x, headerRect.y + 6f, cardWidth, 18f), title);

        float slotButtonWidth = 54f;
        float clearX = headerRect.xMax - slotButtonWidth;
        if (GUI.Button(new Rect(clearX, headerRect.y + 3f, slotButtonWidth, ButtonHeight), "Clear"))
            SetAllSlotsEnabled(slots, false);

        float allX = clearX - 4f - slotButtonWidth;
        if (GUI.Button(new Rect(allX, headerRect.y + 3f, slotButtonWidth, ButtonHeight), "All"))
            SetAllSlotsEnabled(slots, true);

        float levelX = headerRect.x + cardWidth + 12f;
        levelX = DrawLevelGroup(levelX, headerRect.y + 3f, slots);

        float cardY = panelRect.y + FormationHeaderHeight;
        float cardX = panelRect.x + FormationPadding;
        for (int i = 0; i < slots.Length; i++)
        {
            Rect cardRect = new Rect(cardX + i * (cardWidth + CardGap), cardY, cardWidth, CardHeight);
            DrawFormationSlotCard(cardRect, slots, unitPool, isAlly, i);
        }
    }

    private void DrawFormationSlotCard(Rect cardRect, DebugSlot[] slots, UnitDefinition[] unitPool, bool isAlly, int slotIndex)
    {
        if (slots[slotIndex] == null)
            slots[slotIndex] = CreateDefaultSlot(slotIndex);

        DebugSlot slot = slots[slotIndex];
        UnitDefinition unit = GetSelected(unitPool, slot.unitIndex);
        UnitViewDefinition view = GetMatchingView(unit);
        IReadOnlyList<SkillDefinition> allyPool = isAlly ? GetAllySkillChoices(unit) : null;

        GUI.Box(cardRect, GUIContent.none);

        float innerX = cardRect.x + 6f;
        float innerWidth = cardRect.width - 12f;
        float y = cardRect.y + 6f;

        slot.enabled = GUI.Toggle(new Rect(innerX, y + 2f, 18f, 18f), slot.enabled, GUIContent.none);
        int displaySlotNumber = GetFormationSlotIndex(isAlly ? TeamType.Ally : TeamType.Enemy, slotIndex) + 1;
        GUI.Label(new Rect(innerX + 24f, y + 2f, Mathf.Max(40f, innerWidth - 102f), 18f), $"Slot {displaySlotNumber}");
        GUI.Label(new Rect(cardRect.xMax - 72f, y + 2f, 20f, 18f), "Lv");
        slot.level = DrawLevel(new Rect(cardRect.xMax - 48f, y, 38f, 22f), slot.level);
        y += 28f;

        bool previousEnabled = GUI.enabled;
        GUI.enabled = slot.enabled;

        float skillAreaHeight = VisibleSkillSlotCount * ButtonHeight + (VisibleSkillSlotCount - 1) * TextGap;
        float reservedBelowPortrait = ButtonHeight + TextGap + skillAreaHeight + 8f;
        float availablePortraitHeight = Mathf.Max(72f, cardRect.yMax - y - reservedBelowPortrait - 6f);
        float portraitSize = Mathf.Min(innerWidth, availablePortraitHeight);
        Rect portraitRect = new Rect(innerX + (innerWidth - portraitSize) * 0.5f, y, portraitSize, portraitSize);
        Rect rankBadgeRect;
        Rect rankMinusRect;
        GetPromotionRankRects(portraitRect, out rankBadgeRect, out rankMinusRect);

        GUI.Box(portraitRect, GUIContent.none);
        DrawSpriteInRect(portraitRect, view != null ? view.GetSlotFaceSprite() : null);
        if (isAlly)
            DrawPromotionRankOverlay(rankBadgeRect, rankMinusRect, slot);
        HandlePortraitClick(portraitRect, isAlly ? rankBadgeRect : default, isAlly ? rankMinusRect : default, isAlly, slotIndex);
        y = portraitRect.yMax + TextGap;

        if (GUI.Button(new Rect(innerX, y, innerWidth, ButtonHeight), GetDisplayName(unit)))
            OpenPicker(isAlly, slotIndex, PickerKind.Unit);
        y += ButtonHeight + TextGap;

        if (isAlly)
            DrawAllySkillSlots(innerX, y, innerWidth, slotIndex, slot, unit, allyPool);
        else
            DrawEnemySkillSlots(innerX, y, innerWidth, unit);

        GUI.enabled = previousEnabled;
    }

    private void DrawAllySkillSlots(float x, float y, float width, int slotIndex, DebugSlot slot, UnitDefinition unit, IReadOnlyList<SkillDefinition> allyPool)
    {
        if (IsMainPlayerUnit(unit))
        {
            DrawLockedSkillButton(new Rect(x, y, width, ButtonHeight), unit != null ? unit.basicAttack : null);
            y += ButtonHeight + TextGap;
            DrawLockedSkillButton(new Rect(x, y, width, ButtonHeight), GetFixedSkill(unit, 0));
            y += ButtonHeight + TextGap;
            DrawLockedSkillButton(new Rect(x, y, width, ButtonHeight), GetFixedSkill(unit, 1));
            y += ButtonHeight + TextGap;
            DrawLockedSkillButton(new Rect(x, y, width, ButtonHeight), GetFixedSkill(unit, 2));
            return;
        }

        NormalizeAllySkillSelection(slot, allyPool);
        DrawCompactSkillButton(new Rect(x, y, width, ButtonHeight), slotIndex, 0, GetSelected(allyPool, slot.skill0Index));
        y += ButtonHeight + TextGap;
        DrawCompactSkillButton(new Rect(x, y, width, ButtonHeight), slotIndex, 1, GetSelected(allyPool, slot.skill1Index));
        y += ButtonHeight + TextGap;
        DrawCompactSkillButton(new Rect(x, y, width, ButtonHeight), slotIndex, 2, GetSelected(allyPool, slot.skill2Index));
        y += ButtonHeight + TextGap;
        DrawEmptySkillSlot(new Rect(x, y, width, ButtonHeight));
    }

    private void DrawEnemySkillSlots(float x, float y, float width, UnitDefinition unit)
    {
        for (int i = 0; i < VisibleSkillSlotCount; i++)
        {
            SkillDefinition skill = GetEnemyDisplaySkill(unit, i);
            Rect rect = new Rect(x, y + i * (ButtonHeight + TextGap), width, ButtonHeight);
            if (skill != null)
                DrawLockedSkillButton(rect, skill);
            else
                DrawEmptySkillSlot(rect);
        }
    }

    private void DrawCompactSkillButton(Rect rect, int slotIndex, int skillSlotIndex, SkillDefinition skill)
    {
        if (GUI.Button(rect, GetDisplayName(skill)))
            OpenPicker(true, slotIndex, (PickerKind)((int)PickerKind.Skill0 + skillSlotIndex));
    }

    private void DrawPromotionRankOverlay(Rect rankBadgeRect, Rect rankMinusRect, DebugSlot slot)
    {
        if (slot == null)
            return;

        EnsurePromotionRankSpritesLoaded();
        slot.promotionRank = LegionFormula.ClampLegionRank(slot.promotionRank);

        Color previousColor = GUI.color;
        GUI.color = new Color(1f, 1f, 1f, 0.78f);
        Sprite rankSprite = GetPromotionRankSprite(slot.promotionRank);
        if (rankSprite != null)
            DrawSpriteInRect(rankBadgeRect, rankSprite);
        else
            GUI.Box(rankBadgeRect, slot.promotionRank.ToString());
        GUI.color = previousColor;

        if (GUI.Button(rankBadgeRect, GUIContent.none, GUIStyle.none))
            slot.promotionRank = LegionFormula.ClampLegionRank(slot.promotionRank + 1);

        Color previousBackground = GUI.backgroundColor;
        GUI.backgroundColor = new Color(0.25f, 0.25f, 0.25f, 0.82f);
        if (GUI.Button(rankMinusRect, "-"))
            slot.promotionRank = LegionFormula.ClampLegionRank(slot.promotionRank - 1);
        GUI.backgroundColor = previousBackground;
    }

    private Sprite GetPromotionRankSprite(int rank)
    {
        EnsurePromotionRankSpritesLoaded();
        int index = LegionFormula.ClampLegionRank(rank) - 1;
        if (promotionRankSprites == null || index < 0 || index >= promotionRankSprites.Length)
            return null;
        return promotionRankSprites[index];
    }

    private static void GetPromotionRankRects(Rect portraitRect, out Rect rankBadgeRect, out Rect rankMinusRect)
    {
        float badgeSize = Mathf.Clamp(portraitRect.width * 0.34f, 36f, 52f);
        rankBadgeRect = new Rect(portraitRect.xMax - badgeSize - 6f, portraitRect.y + 6f, badgeSize, badgeSize);

        float minusSize = Mathf.Clamp(badgeSize * 0.45f, 18f, 22f);
        rankMinusRect = new Rect(rankBadgeRect.x - minusSize - 3f, rankBadgeRect.yMax - minusSize, minusSize, minusSize);
    }

    private void HandlePortraitClick(Rect portraitRect, Rect rankBadgeRect, Rect rankMinusRect, bool isAlly, int slotIndex)
    {
        Event current = Event.current;
        if (current == null || current.type != EventType.MouseUp || current.button != 0)
            return;
        if (!portraitRect.Contains(current.mousePosition))
            return;
        if (isAlly && (rankBadgeRect.Contains(current.mousePosition) || rankMinusRect.Contains(current.mousePosition)))
            return;

        OpenPicker(isAlly, slotIndex, PickerKind.Unit);
        current.Use();
    }

    private void DrawLockedSkillButton(Rect rect, SkillDefinition skill)
    {
        bool previousEnabled = GUI.enabled;
        GUI.enabled = false;
        GUI.Button(rect, GetDisplayName(skill));
        GUI.enabled = previousEnabled;
    }

    private void DrawEmptySkillSlot(Rect rect)
    {
        Color previousColor = GUI.backgroundColor;
        bool previousEnabled = GUI.enabled;
        GUI.backgroundColor = new Color(0.36f, 0.36f, 0.36f, 1f);
        GUI.enabled = false;
        GUI.Button(rect, "-");
        GUI.enabled = previousEnabled;
        GUI.backgroundColor = previousColor;
    }

    private static SkillDefinition GetFixedSkill(UnitDefinition unit, int index)
    {
        if (unit == null || unit.fixedStartingSkills == null || index < 0 || index >= unit.fixedStartingSkills.Count)
            return null;
        return unit.fixedStartingSkills[index];
    }

    private SkillDefinition GetEnemyDisplaySkill(UnitDefinition unit, int index)
    {
        if (unit == null || index < 0)
            return null;
        if (index == 0)
            return unit.basicAttack;
        return GetFixedSkill(unit, index - 1);
    }

    private void NormalizeAllySkillSelection(DebugSlot slot, IReadOnlyList<SkillDefinition> pool)
    {
        if (slot == null || pool == null || pool.Count == 0)
            return;

        slot.skill0Index = ClampIndex(pool, slot.skill0Index);
        slot.skill1Index = GetValidUniqueSkillIndex(pool, slot.skill1Index, slot.skill0Index, -1);
        slot.skill2Index = GetValidUniqueSkillIndex(pool, slot.skill2Index, slot.skill0Index, slot.skill1Index);
    }

    private int GetValidUniqueSkillIndex(IReadOnlyList<SkillDefinition> pool, int preferredIndex, int blockedIndex0, int blockedIndex1)
    {
        int clamped = ClampIndex(pool, preferredIndex);
        if (!IsSameSkill(GetSelected(pool, clamped), GetSkillByIndex(pool, blockedIndex0)) &&
            !IsSameSkill(GetSelected(pool, clamped), GetSkillByIndex(pool, blockedIndex1)))
            return clamped;

        for (int i = 0; i < pool.Count; i++)
        {
            SkillDefinition skill = pool[i];
            if (!IsSameSkill(skill, GetSkillByIndex(pool, blockedIndex0)) &&
                !IsSameSkill(skill, GetSkillByIndex(pool, blockedIndex1)))
                return i;
        }

        return clamped;
    }

    private static SkillDefinition GetSkillByIndex(IReadOnlyList<SkillDefinition> pool, int index)
    {
        if (pool == null || index < 0 || index >= pool.Count)
            return null;
        return pool[index];
    }

    private static void AddLevelToSlots(DebugSlot[] slots, int amount)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].level = Mathf.Max(1, slots[i].level + amount);
        }
    }

    private static void ResetLevels(DebugSlot[] slots, int level)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null)
                slots[i].level = Mathf.Max(1, level);
        }
    }

    private float DrawLevelGroup(float x, float y, DebugSlot[] slots)
    {
        const float smallWidth = 32f;
        const float resetWidth = 46f;
        const float gap = 3f;

        if (GUI.Button(new Rect(x, y, smallWidth, ButtonHeight), "-10"))
            AddLevelToSlots(slots, -10);
        x += smallWidth + gap;
        if (GUI.Button(new Rect(x, y, smallWidth, ButtonHeight), "-5"))
            AddLevelToSlots(slots, -5);
        x += smallWidth + gap;
        if (GUI.Button(new Rect(x, y, smallWidth, ButtonHeight), "-1"))
            AddLevelToSlots(slots, -1);
        x += smallWidth + 8f;
        if (GUI.Button(new Rect(x, y, resetWidth, ButtonHeight), "Reset"))
            ResetLevels(slots, defaultLevel);
        x += resetWidth + 8f;
        if (GUI.Button(new Rect(x, y, smallWidth, ButtonHeight), "+1"))
            AddLevelToSlots(slots, 1);
        x += smallWidth + gap;
        if (GUI.Button(new Rect(x, y, smallWidth, ButtonHeight), "+5"))
            AddLevelToSlots(slots, 5);
        x += smallWidth + gap;
        if (GUI.Button(new Rect(x, y, smallWidth, ButtonHeight), "+10"))
            AddLevelToSlots(slots, 10);

        return x + smallWidth;
    }

    private void DrawRuntimeDebugControls()
    {
        Rect rect = new Rect(12f, 12f, 470f, 72f);
        GUI.Box(rect, "Debug Battle Controls");

        float x = rect.x + 10f;
        float y = rect.y + 28f;
        if (GUI.Button(new Rect(x, y, 110f, ButtonHeight), "Stop Battle"))
            StopDebugBattle();
        x += 124f;

        bool nextInvincible = GUI.Toggle(new Rect(x, y + 2f, 120f, 20f), debugAllyInvincible, "Ally Invincible");
        if (nextInvincible != debugAllyInvincible)
            debugAllyInvincible = nextInvincible;
        x += 136f;

        bool nextNoCooldown = GUI.Toggle(new Rect(x, y + 2f, 130f, 20f), debugNoSkillCooldown, "No Skill Cooldown");
        if (nextNoCooldown != debugNoSkillCooldown)
        {
            debugNoSkillCooldown = nextNoCooldown;
            ApplyRuntimeCooldownMode();
        }
    }

    private int DrawLevel(Rect rect, int level)
    {
        string text = GUI.TextField(rect, Mathf.Max(1, level).ToString());
        return int.TryParse(text, out int parsed) ? Mathf.Max(1, parsed) : Mathf.Max(1, level);
    }
}
