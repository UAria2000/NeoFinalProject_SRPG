using System.Collections.Generic;
using UnityEngine;

public partial class DebugBattleSceneController
{
    private const float PickerPadding = 12f;
    private const float PickerSearchHeight = 26f;
    private const int PickerPreferredColumns = 4;
    private const float PickerMinimumCardWidth = 146f;
    private const float PickerMaximumCardWidth = 180f;
    private const float PickerCardHeight = 190f;
    private const float PickerCardGap = 8f;
    private const float PickerButtonHeight = 24f;

    private Vector2 pickerWindowSize;

    private void DrawPickerWindow()
    {
        float width = Mathf.Min(820f, Screen.width - 80f);
        float height = Mathf.Min(620f, Screen.height - 80f);
        width = Mathf.Max(360f, width);
        height = Mathf.Max(320f, height);
        pickerWindowSize = new Vector2(width, height);

        Rect rect = new Rect((Screen.width - width) * 0.5f, (Screen.height - height) * 0.5f, width, height);
        GUI.ModalWindow(48312, rect, DrawPickerContents, GetPickerTitle());
    }

    private void DrawPickerContents(int windowId)
    {
        float windowWidth = Mathf.Max(360f, pickerWindowSize.x);
        float windowHeight = Mathf.Max(320f, pickerWindowSize.y);

        Rect searchLabelRect = new Rect(PickerPadding, 22f, 48f, 20f);
        Rect closeRect = new Rect(windowWidth - PickerPadding - 76f, 20f, 76f, PickerSearchHeight);
        Rect searchRect = new Rect(searchLabelRect.xMax + 4f, 20f, closeRect.x - searchLabelRect.xMax - 10f, PickerSearchHeight);

        GUI.Label(searchLabelRect, "Search");
        pickerSearch = GUI.TextField(searchRect, pickerSearch ?? string.Empty);
        if (GUI.Button(closeRect, "Close"))
        {
            picker.Close();
            pickerSearch = string.Empty;
            return;
        }

        Rect scrollRect = new Rect(PickerPadding, 52f, windowWidth - PickerPadding * 2f, windowHeight - 64f);
        if (picker.Kind == PickerKind.Unit)
            DrawUnitPickerContents(scrollRect);
        else
            DrawSkillPickerContents(scrollRect);
    }

    private void DrawUnitPickerContents(Rect scrollRect)
    {
        UnitDefinition[] pool = picker.IsAlly ? allyUnitDefinitions : enemyUnitDefinitions;
        if (pool == null)
            return;

        float usableWidth = Mathf.Max(PickerMinimumCardWidth, scrollRect.width - 18f);
        int columns = GetPickerColumnCount(usableWidth);
        float cardWidth = GetPickerCardWidth(usableWidth, columns);
        float contentWidth = columns * cardWidth + (columns - 1) * PickerCardGap;
        float startX = Mathf.Max(0f, (usableWidth - contentWidth) * 0.5f);
        int visibleIndex = 0;
        int visibleCount = Mathf.Max(1, GetMatchingUnitCount(pool));
        int rowCount = Mathf.CeilToInt(visibleCount / (float)columns);
        float contentHeight = rowCount * PickerCardHeight + Mathf.Max(0, rowCount - 1) * PickerCardGap + PickerPadding;
        Rect contentRect = new Rect(0f, 0f, usableWidth, contentHeight);

        pickerScroll = GUI.BeginScrollView(scrollRect, pickerScroll, contentRect);
        for (int i = 0; i < pool.Length; i++)
        {
            UnitDefinition unit = pool[i];
            if (!MatchesSearch(GetDisplayName(unit), pickerSearch))
                continue;

            UnitViewDefinition view = GetMatchingView(unit);
            int column = visibleIndex % columns;
            int row = visibleIndex / columns;
            Rect cardRect = new Rect(
                startX + column * (cardWidth + PickerCardGap),
                row * (PickerCardHeight + PickerCardGap),
                cardWidth,
                PickerCardHeight);

            if (DrawUnitCard(cardRect, unit, view))
            {
                DebugSlot slot = picker.IsAlly ? allySlots[picker.SlotIndex] : enemySlots[picker.SlotIndex];
                slot.unitIndex = i;
                slot.viewIndex = FindMatchingViewIndex(unit);
                slot.ResetSkills();
                picker.Close();
                break;
            }

            visibleIndex++;
        }
        GUI.EndScrollView();
    }

    private bool DrawUnitCard(Rect cardRect, UnitDefinition unit, UnitViewDefinition view)
    {
        GUI.Box(cardRect, GUIContent.none);

        float innerX = cardRect.x + 6f;
        float innerWidth = cardRect.width - 12f;
        float y = cardRect.y + 6f;
        float portraitSize = Mathf.Min(84f, innerWidth);
        Rect portraitRect = new Rect(innerX + (innerWidth - portraitSize) * 0.5f, y, portraitSize, portraitSize);
        GUI.Box(portraitRect, GUIContent.none);
        DrawSpriteInRect(portraitRect, view != null ? view.GetSlotFaceSprite() : null);

        Rect unitNameRect = new Rect(innerX, y + portraitSize + 6f, innerWidth, 20f);
        Rect viewNameRect = new Rect(innerX, unitNameRect.yMax + 2f, innerWidth, 36f);
        Rect buttonRect = new Rect(innerX, cardRect.yMax - PickerButtonHeight - 6f, innerWidth, PickerButtonHeight);

        GUI.Label(unitNameRect, GetDisplayName(unit));
        GUI.Label(viewNameRect, view != null ? view.name : "No matching view");
        return GUI.Button(buttonRect, "Select");
    }

    private static int GetPickerColumnCount(float usableWidth)
    {
        if (usableWidth <= PickerMinimumCardWidth)
            return 1;

        for (int columns = PickerPreferredColumns; columns > 1; columns--)
        {
            float requiredWidth = columns * PickerMinimumCardWidth + (columns - 1) * PickerCardGap;
            if (requiredWidth <= usableWidth)
                return columns;
        }

        return 1;
    }

    private static float GetPickerCardWidth(float usableWidth, int columns)
    {
        if (columns <= 1)
            return Mathf.Min(PickerMaximumCardWidth, usableWidth);

        float rawWidth = (usableWidth - (columns - 1) * PickerCardGap) / columns;
        return Mathf.Floor(Mathf.Clamp(rawWidth, PickerMinimumCardWidth, PickerMaximumCardWidth));
    }

    private void DrawSkillPickerContents(Rect scrollRect)
    {
        DebugSlot slot = allySlots[picker.SlotIndex];
        UnitDefinition unit = GetSelected(allyUnitDefinitions, slot.unitIndex);
        IReadOnlyList<SkillDefinition> pool = GetAllySkillChoices(unit);
        int visibleCount = Mathf.Max(1, GetMatchingSkillCount(pool, slot));
        Rect contentRect = new Rect(0f, 0f, scrollRect.width - 18f, visibleCount * 34f + PickerPadding);

        pickerScroll = GUI.BeginScrollView(scrollRect, pickerScroll, contentRect);
        float y = 0f;
        for (int i = 0; i < pool.Count; i++)
        {
            SkillDefinition skill = pool[i];
            if (!MatchesSearch(GetDisplayName(skill), pickerSearch))
                continue;
            if (IsSkillAlreadySelected(slot, pool, i))
                continue;

            if (GUI.Button(new Rect(0f, y, contentRect.width, 30f), GetDisplayName(skill)))
            {
                SetSkillIndex(slot, picker.Kind, i);
                picker.Close();
                break;
            }
            y += 34f;
        }
        GUI.EndScrollView();
    }

    private int GetMatchingSkillCount(IReadOnlyList<SkillDefinition> pool, DebugSlot slot)
    {
        if (pool == null)
            return 0;

        int count = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            SkillDefinition skill = pool[i];
            if (MatchesSearch(GetDisplayName(skill), pickerSearch) && !IsSkillAlreadySelected(slot, pool, i))
                count++;
        }

        return count;
    }

    private bool IsSkillAlreadySelected(DebugSlot slot, IReadOnlyList<SkillDefinition> pool, int candidateIndex)
    {
        if (slot == null || pool == null || candidateIndex < 0 || candidateIndex >= pool.Count)
            return false;

        SkillDefinition candidate = pool[candidateIndex];
        return IsSameSkill(candidate, GetSkillByIndex(pool, slot.skill0Index)) ||
               IsSameSkill(candidate, GetSkillByIndex(pool, slot.skill1Index)) ||
               IsSameSkill(candidate, GetSkillByIndex(pool, slot.skill2Index));
    }

    private void DrawSpriteInRect(Rect rect, Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return;

        Rect textureRect = sprite.textureRect;
        Rect texCoords = new Rect(
            textureRect.x / sprite.texture.width,
            textureRect.y / sprite.texture.height,
            textureRect.width / sprite.texture.width,
            textureRect.height / sprite.texture.height);
        GUI.DrawTextureWithTexCoords(GetAspectFitRect(rect, textureRect.width, textureRect.height), sprite.texture, texCoords, true);
    }

    private static Rect GetAspectFitRect(Rect bounds, float contentWidth, float contentHeight)
    {
        if (contentWidth <= 0f || contentHeight <= 0f || bounds.width <= 0f || bounds.height <= 0f)
            return bounds;

        float contentAspect = contentWidth / contentHeight;
        float boundsAspect = bounds.width / bounds.height;
        if (boundsAspect > contentAspect)
        {
            float width = bounds.height * contentAspect;
            return new Rect(bounds.x + (bounds.width - width) * 0.5f, bounds.y, width, bounds.height);
        }

        float height = bounds.width / contentAspect;
        return new Rect(bounds.x, bounds.y + (bounds.height - height) * 0.5f, bounds.width, height);
    }

    private void OpenPicker(bool isAlly, int slotIndex, PickerKind kind)
    {
        picker = new PickerState
        {
            IsOpen = true,
            IsAlly = isAlly,
            SlotIndex = slotIndex,
            Kind = kind,
        };
        pickerScroll = Vector2.zero;
        pickerSearch = string.Empty;
    }

#if UNITY_EDITOR
    public void EditorOpenUnitPickerForDebugCapture(bool isAlly, int slotIndex)
    {
        OpenPicker(isAlly, slotIndex, PickerKind.Unit);
    }

    public void EditorOpenSkillPickerForDebugCapture(int slotIndex, int skillSlotIndex)
    {
        PickerKind kind = (PickerKind)((int)PickerKind.Skill0 + Mathf.Clamp(skillSlotIndex, 0, 2));
        OpenPicker(true, slotIndex, kind);
    }
#endif

    private string GetPickerTitle()
    {
        if (!picker.IsOpen)
            return string.Empty;
        return picker.Kind == PickerKind.Unit ? "Select Unit" : "Select Skill";
    }

    private int GetMatchingUnitCount(UnitDefinition[] pool)
    {
        if (pool == null)
            return 0;

        int count = 0;
        for (int i = 0; i < pool.Length; i++)
        {
            if (MatchesSearch(GetDisplayName(pool[i]), pickerSearch))
                count++;
        }

        return count;
    }
}
