using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(menuName = "Battle/Unit View Definition")]
public class UnitViewDefinition : ScriptableObject
{
    [Header("UI Portraits")]
    [FormerlySerializedAs("portrait")]
    public Sprite slotFaceSprite;
    public Sprite bustPortraitSprite;

    [Header("Dead UI Portraits")]
    public Sprite deadSlotFaceSprite;
    public Sprite deadBustPortraitSprite;

    [Header("Battle")]
    [FormerlySerializedAs("bodySprite")]
    public Sprite battleSprite;
    [Tooltip("공격/스킬 사용 중 잠깐 교체할 전신 스프라이트입니다. 비워두면 기본 battleSprite를 사용합니다.")]
    public Sprite attackBattleSprite;
    [Tooltip("피격/도트 피해 중 1초간 교체할 전신 스프라이트입니다. 비워두면 기본 battleSprite를 사용합니다.")]
    public Sprite hitBattleSprite;

    [Header("Battle Highlight")]
    [Tooltip("전투 중 현재 턴/선택 가능/호버 상태를 표시할 유닛별 하이라이트 이미지입니다.")]
    public Sprite battleHighlightSprite;

    [Header("Legion Highlight")]
    [Tooltip("군단 카드에서 분해 선택 상태를 표시할 유닛별 하이라이트 이미지입니다. 비워두면 battleHighlightSprite를 사용합니다.")]
    public Sprite legionDecomposeSelectedHighlightSprite;

    [Header("Attack Motion Transform")]
    [Tooltip("공격 모션 전용 Image의 RectTransform anchoredPosition입니다.")]
    public Vector2 attackSpriteAnchoredPosition = Vector2.zero;
    [Tooltip("0 이하 값이면 공격 모션 이미지의 크기를 프리팹 기본값으로 유지합니다.")]
    public Vector2 attackSpriteSizeDelta = Vector2.zero;
    [Tooltip("공격 모션 전용 Image의 localScale입니다.")]
    public Vector3 attackSpriteLocalScale = Vector3.one;

    [Header("Hit Motion Transform")]
    [Tooltip("피격 모션 전용 Image의 RectTransform anchoredPosition입니다.")]
    public Vector2 hitSpriteAnchoredPosition = Vector2.zero;
    [Tooltip("0 이하 값이면 피격 모션 이미지의 크기를 프리팹 기본값으로 유지합니다.")]
    public Vector2 hitSpriteSizeDelta = Vector2.zero;
    [Tooltip("피격 모션 전용 Image의 localScale입니다.")]
    public Vector3 hitSpriteLocalScale = Vector3.one;

    [Header("Dead Battle")]
    public Sprite deadBattleSprite;

    public BattleUnitView viewPrefab;

    public Sprite GetSlotFaceSprite()
    {
        return GetSlotFaceSprite(false);
    }

    public Sprite GetBustPortraitSprite()
    {
        return GetBustPortraitSprite(false);
    }

    public Sprite GetBattleSprite()
    {
        return GetBattleSprite(false);
    }

    public Sprite GetAttackBattleSprite()
    {
        if (attackBattleSprite != null)
            return attackBattleSprite;
        return GetBattleSprite(false);
    }

    public Sprite GetHitBattleSprite()
    {
        if (hitBattleSprite != null)
            return hitBattleSprite;
        return GetBattleSprite(false);
    }

    public Sprite GetBattleHighlightSprite()
    {
        return battleHighlightSprite;
    }

    public Sprite GetLegionDecomposeSelectedHighlightSprite()
    {
        if (legionDecomposeSelectedHighlightSprite != null)
            return legionDecomposeSelectedHighlightSprite;
        return battleHighlightSprite;
    }

    public Sprite GetSlotFaceSprite(bool isDead)
    {
        if (isDead)
        {
            if (deadSlotFaceSprite != null)
                return deadSlotFaceSprite;
            if (deadBustPortraitSprite != null)
                return deadBustPortraitSprite;
            if (deadBattleSprite != null)
                return deadBattleSprite;
        }

        if (slotFaceSprite != null)
            return slotFaceSprite;
        if (bustPortraitSprite != null)
            return bustPortraitSprite;
        return battleSprite;
    }

    public Sprite GetBustPortraitSprite(bool isDead)
    {
        if (isDead)
        {
            if (deadBustPortraitSprite != null)
                return deadBustPortraitSprite;
            if (deadSlotFaceSprite != null)
                return deadSlotFaceSprite;
            if (deadBattleSprite != null)
                return deadBattleSprite;
        }

        if (bustPortraitSprite != null)
            return bustPortraitSprite;
        if (slotFaceSprite != null)
            return slotFaceSprite;
        return battleSprite;
    }

    public Sprite GetBattleSprite(bool isDead)
    {
        if (isDead)
        {
            if (deadBattleSprite != null)
                return deadBattleSprite;
            if (deadBustPortraitSprite != null)
                return deadBustPortraitSprite;
            if (deadSlotFaceSprite != null)
                return deadSlotFaceSprite;
        }

        if (battleSprite != null)
            return battleSprite;
        if (bustPortraitSprite != null)
            return bustPortraitSprite;
        return slotFaceSprite;
    }
}
