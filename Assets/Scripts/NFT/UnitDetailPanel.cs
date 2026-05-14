using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class UnitDetailPanel : MonoBehaviour
{
    [Header("Top Info")]
    public Image rankImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI levelText;

    [Header("Visuals")]
    public Image unitIllustration;
    public Image classIcon;
    public Image[] skillIcons;
    public GameObject nftBadge;

    [Header("HP Bar (Slider)")]
    public Slider hpSlider;
    public TextMeshProUGUI hpText;

    [Header("Stats")]
    public TextMeshProUGUI atkText;      // 검 아이콘 (DMG)
    public TextMeshProUGUI defText;      // 과녁 아이콘 (HIT)
    public TextMeshProUGUI armorText;    // 갑옷 아이콘 (AC)
    public TextMeshProUGUI blockText;    // 방패 아이콘 (IDT)
    public TextMeshProUGUI critText;     // 폭발 아이콘 (CRI)
    public TextMeshProUGUI critDmgText;  // 폭발+ 아이콘 (CRD)
    public TextMeshProUGUI speedText;    // 장화 아이콘 (SPD)

    [Header("Data Resources")]
    public Sprite[] rankSprites;

    public void Setup(RosterUnitSaveData unitData)
    {
        if (unitData == null) return;

        // 1. 랭크 설정
        int rankIndex = Mathf.Clamp(unitData.promotionRank - 1, 0, rankSprites.Length - 1);
        if (rankSprites != null && rankSprites.Length > 0) rankImage.sprite = rankSprites[rankIndex];

        // 2. 이름 및 레벨 설정 (Rich Text 적용)
        nameText.text = string.IsNullOrEmpty(unitData.instanceDisplayNameOverride)
            ? unitData.unitDefinitionId : unitData.instanceDisplayNameOverride;
        levelText.text = $"Lv. {unitData.level} <color=#AAAAAA>({unitData.originalLevel})</color>";

        // 3. 데이터 리졸버 및 변동치 참조
        var unitDef = SaveReferenceResolver.Instance.FindUnitDefinition(unitData.unitDefinitionId);
        var viewDef = SaveReferenceResolver.Instance.FindUnitViewDefinition(unitData.unitViewDefinitionName);
        var statVar = unitData.statVariance;

        // 4. 7개 핵심 능력치 출력 (UnitDefinition 필드명 적용)
        if (unitDef != null)
        {
            // 공격력 (DMG): 기본값 + 레벨 성장 + 변동치
            atkText.text = Mathf.RoundToInt(unitDef.dmg + unitData.levelGrowthDmg + statVar.dmgDelta).ToString();

            // 명중 (HIT): float 형식이므로 반올림 처리
            defText.text = Mathf.RoundToInt(unitDef.hit + statVar.hitDelta).ToString();

            // 회피 (AC): float 형식이므로 반올림 처리
            armorText.text = Mathf.RoundToInt(unitDef.ac + statVar.acDelta).ToString();

            // 피해 감소율 (IDT): % 단위 표기
            blockText.text = $"{Mathf.RoundToInt(unitDef.idt + statVar.idtDelta)}%";

            // 치명타 확률 (CRI): % 단위 표기
            critText.text = $"{Mathf.RoundToInt(unitDef.cri + statVar.criDelta)}%";

            // 치명타 피해 (CRD)
            critDmgText.text = Mathf.RoundToInt(unitDef.crd + statVar.crdDelta).ToString();

            // 이동 속도 (SPD)
            speedText.text = Mathf.RoundToInt(unitDef.spd + statVar.spdDelta).ToString();
        }

        // 5. HP 슬라이더 (maxHP 필드 사용)
        int baseHp = unitDef != null ? unitDef.maxHP : 10;
        float maxHp = baseHp + unitData.levelGrowthMaxHp + statVar.maxHpDelta;

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
            hpSlider.value = unitData.persistentCurrentHP;
        }
        hpText.text = $"{unitData.persistentCurrentHP} / {Mathf.RoundToInt(maxHp)}";

        // 6. 스킬 아이콘 및 NFT 배지 (기존 로직 유지)
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
                else skillIcons[i].gameObject.SetActive(false);
            }
            else skillIcons[i].gameObject.SetActive(false);
        }

        if (nftBadge != null) nftBadge.SetActive(unitData.isNft);
        gameObject.SetActive(true);
    }
}