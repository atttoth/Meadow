using UnityEngine;
using UnityEngine.EventSystems;

public abstract class HitArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    protected GameEventController _eventController;

    public virtual void Init()
    {
        _eventController = new();
    }

    public virtual void OnPointerEnter(PointerEventData eventData) { }

    public virtual void OnPointerExit(PointerEventData eventData) { }

    public virtual void Toggle(bool value)
    {
        gameObject.SetActive(value);
    }
}
