using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SoldierCard : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public Image portraitImage;
    public GameObject highlightObject;

    // 인벤토리용: RosterUnitSaveData를 받아 설정
    public void SetupCard(RosterUnitSaveData unitData)
    {
        // 1. 이름 설정: 우선적으로 저장된 이름을 쓰고, 없으면 리졸버에서 정의(Definition)를 찾아 이름을 가져옴
        if (!string.IsNullOrEmpty(unitData.instanceDisplayNameOverride))
        {
            nameText.text = unitData.instanceDisplayNameOverride;
        }
        else
        {
            // SaveReferenceResolver를 통해 원본 유닛 정의를 찾음
            var def = SaveReferenceResolver.Instance.FindUnitDefinition(unitData.unitDefinitionId);
            nameText.text = (def != null) ? def.unitId : unitData.unitDefinitionId;
        }

        // 2. 초상화 설정: ViewDefinition을 찾아 해당 외형의 아이콘 등을 연결
        var viewDef = SaveReferenceResolver.Instance.FindUnitViewDefinition(unitData.unitViewDefinitionName);
        if (viewDef != null)
        {
            // viewDef에 연결된 스프라이트가 있다면 적용 (구조에 따라 아이콘 필드 참조)
            // portraitImage.sprite = viewDef.portrait; 
        }

        SetHighlight(false);
    }

    // 상점용: 기존 로직 유지
    public void SetupCard(string unitName, int price)
    {
        nameText.text = unitName;
        // 상점 매물도 리졸버에서 아이콘을 찾을 수 있음
        var viewDef = SaveReferenceResolver.Instance.FindUnitViewDefinition(unitName);
        SetHighlight(false);
    }

    public void SetHighlight(bool active) => highlightObject?.SetActive(active);
}