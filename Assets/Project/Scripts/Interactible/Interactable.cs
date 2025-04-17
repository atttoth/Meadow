using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Interactable : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    protected LogicEventDispatcher _dispatcher;
    protected Image _mainImage; // to toggle raycast
    protected Transform _parent; // used at marker and card hierarchy positioning

    public virtual void Init()
    {
        _dispatcher = new();
    }

    public Image MainImage {  get { return _mainImage; } }

    public virtual void ToggleRayCast(bool value) { return; }

    public virtual void OnBeginDrag(PointerEventData eventData) { return; }

    public virtual void OnDrag(PointerEventData eventData) { return; }

    public virtual void OnEndDrag(PointerEventData eventData) { return; }

    public virtual void OnPointerClick(PointerEventData eventData) { return; }

    public virtual void OnPointerEnter(PointerEventData eventData) { return; }

    public virtual void OnPointerExit(PointerEventData eventData) { return; }
}
