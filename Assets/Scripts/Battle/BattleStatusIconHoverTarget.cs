using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 상태이상 아이콘 1개의 실제 raycast 영역.
/// BattleStatusIconUI 루트가 아니라 아이콘 이미지 위에 붙여서,
/// 아이콘 바 전체가 아닌 해당 아이콘에만 호버 툴팁이 뜨도록 한다.
/// </summary>
[DisallowMultipleComponent]
public class BattleStatusIconHoverTarget : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    private BattleStatusIconUI owner;

    public void Bind(BattleStatusIconUI iconOwner)
    {
        owner = iconOwner;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        owner?.HandlePointerEnter(eventData);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        owner?.HandlePointerMove(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        owner?.HandlePointerExit(eventData);
    }
}
