using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UnitDetailPanel : MonoBehaviour
{
    [Header("Top Info")]
    public Image rankImage;           // 랭크 이미지 (방패 모양)
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText; // Lv. (현재레벨) (포획레벨)

    [Header("Visuals")]
    public Image unitIllustration;    // 전신 일러스트
    public Image classIcon;           // 클래스 아이콘 (랭크 아래 첫 번째 칸)
    public Image[] skillIcons;        // 스킬 아이콘들 (그 아래 4개 칸)
    public GameObject nftBadge;       // NFT 마크

    [Header("HP Bar (Slider)")]
    public Slider hpSlider;           // 체력바 슬라이더
    public TextMeshProUGUI hpText;

    [Header("Stats")]
    public TextMeshProUGUI atkText;
    public TextMeshProUGUI defText;
    public TextMeshProUGUI armorText;
    public TextMeshProUGUI blockText;
    public TextMeshProUGUI critText;
    public TextMeshProUGUI critDmgText;
    public TextMeshProUGUI speedText;

    [Header("Data Resources")]
    public Sprite[] rankSprites;      // 랭크별 방패 이미지 (인덱스 0이 1단계)
    public int baseMaxHp = 100;       // 캐릭터 기본 최대 체력

    public void Setup(RosterUnitSaveData unitData)
    {
        if (unitData == null) return;

        // 1. 랭크 이미지 설정 (promotionRank 기반)
        int rankIndex = Mathf.Clamp(unitData.promotionRank - 1, 0, rankSprites.Length - 1);
        if (rankSprites != null && rankSprites.Length > 0)
        {
            rankImage.sprite = rankSprites[rankIndex];
        }

        // 2. 이름 및 레벨 설정 (현재레벨, 포획레벨)
        nameText.text = string.IsNullOrEmpty(unitData.instanceDisplayNameOverride)
            ? unitData.unitDefinitionId : unitData.instanceDisplayNameOverride;

        levelText.text = $"Lv. {unitData.level} <color=#AAAAAA>({unitData.originalLevel})</color>";

        // 3. 클래스 및 일러스트 설정 (SaveReferenceResolver 활용)
        var unitDef = SaveReferenceResolver.Instance.FindUnitDefinition(unitData.unitDefinitionId);
        var viewDef = SaveReferenceResolver.Instance.FindUnitViewDefinition(unitData.unitViewDefinitionName);

        if (unitDef != null && classIcon != null)
        {
            // UnitDefinition의 아이콘을 클래스 아이콘으로 사용
            // (에셋 구조에 따라 unitDef.icon 또는 별도 필드 사용)
            // classIcon.sprite = unitDef.icon; 
        }

        if (viewDef != null && unitIllustration != null)
        {
            // 전신 일러스트 적용 (ViewDefinition에 정의된 필드 사용)
            // unitIllustration.sprite = viewDef.fullIllustration;
        }

        // 4. 4개의 스킬 아이콘 설정
        for (int i = 0; i < skillIcons.Length; i++)
        {
            if (i < unitData.learnedSkillIds.Count)
            {
                var skillDef = SaveReferenceResolver.Instance.FindSkillDefinition(unitData.learnedSkillIds[i]);
                if (skillDef != null && skillDef.icon != null)
                {
                    skillIcons[i].sprite = skillDef.icon;
                    skillIcons[i].gameObject.SetActive(true);
                }
                else
                {
                    skillIcons[i].gameObject.SetActive(false);
                }
            }
            else
            {
                skillIcons[i].gameObject.SetActive(false); // 배운 스킬이 슬롯보다 적으면 비활성화
            }
        }

        // 5. NFT 배지 표시 여부
        if (nftBadge != null)
        {
            nftBadge.SetActive(unitData.isNft);
        }

        // 6. HP 슬라이더 설정
        float maxHp = baseMaxHp + unitData.levelGrowthMaxHp;
        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = unitData.persistentCurrentHP;
        }
        hpText.text = $"{unitData.persistentCurrentHP} / {maxHp}";

        // 7. 능력치 텍스트 갱신 (보유 데이터 그대로 출력)
        atkText.text = unitData.levelGrowthDmg.ToString();

        // 데이터 구조에 따라 나머지 스탯도 동일하게 매핑
        // defText.text = unitData.someDefValue.ToString();
        // speedText.text = unitData.someSpeedValue.ToString();

        gameObject.SetActive(true);
    }
}