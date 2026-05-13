using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoldierCard : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI soldierNameText;
    public TextMeshProUGUI levelText;
    public Image portraitImage;
    public GameObject highlightObject;

    [Header("Settings")]
    public SoldierPortraitSettings portraitSettings; // 인스펙터에서 등록

    // 1. 내 인벤토리용 (RosterUnitSaveData 활용)
    public void SetupCard(RosterUnitSaveData unitData)
    {
        // 이름 설정
        soldierNameText.text = string.IsNullOrEmpty(unitData.instanceDisplayNameOverride)
            ? unitData.unitDefinitionId
            : unitData.instanceDisplayNameOverride;

        levelText.text = $"Lv.{unitData.level}";

        // 초상화 자동 연결 (unitViewDefinitionName 기반)
        if (portraitSettings != null)
        {
            portraitImage.sprite = portraitSettings.GetPortrait(unitData.unitViewDefinitionName);
        }

        SetHighlight(false);
    }

    // 2. 상점 매물용 (ID 문자열 활용)
    public void SetupCard(string unitViewName, int price)
    {
        soldierNameText.text = unitViewName;
        levelText.text = $"{price:N0} GOLD";

        // 초상화 자동 연결
        if (portraitSettings != null)
        {
            portraitImage.sprite = portraitSettings.GetPortrait(unitViewName);
        }

        SetHighlight(false);
    }

    public void SetHighlight(bool isActive)
    {
        if (highlightObject != null) highlightObject.SetActive(isActive);
    }
}