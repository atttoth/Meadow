using UnityEngine;
using UnityEngine.EventSystems;

public abstract class HitArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    protected LogicEventDispatcher _dispatcher;

    public virtual void Init()
    {
        _dispatcher = new();
    }

    public virtual void OnPointerEnter(PointerEventData eventData) { }

    public virtual void OnPointerExit(PointerEventData eventData) { }

    public virtual void Toggle(bool value)
    {
        gameObject.SetActive(value);
    }
}
