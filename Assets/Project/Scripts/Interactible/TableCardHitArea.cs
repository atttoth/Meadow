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
        _dispatcher.InvokeEventHandler(GameLogicEventType.TABLE_HITAREA_HOVERED_OVER, new object[] { type, hitAreaTag });
    }
}
