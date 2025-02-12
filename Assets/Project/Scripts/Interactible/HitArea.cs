using UnityEngine.EventSystems;

public abstract class HitArea : GameLogicEvent, IPointerEnterHandler, IPointerExitHandler
{
    public virtual void OnPointerEnter(PointerEventData eventData) { }

    public virtual void OnPointerExit(PointerEventData eventData) { }

    public virtual void Toggle(bool value)
    {
        gameObject.SetActive(value);
    }
}
