using UnityEngine.EventSystems;

public class TableCardHitArea : HitArea
{
    public HolderSubType type;

    public override void OnPointerEnter(PointerEventData eventData)
    {
        UpdateHitArea(tag);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        UpdateHitArea();
    }

    private void UpdateHitArea(string hitAreaTag = null)
    {
        StartEventHandler(GameLogicEventType.TABLE_HITAREA_HOVERED_OVER, new GameTaskItemData() { subType = type, hitAreaTag = hitAreaTag });
    }
}
