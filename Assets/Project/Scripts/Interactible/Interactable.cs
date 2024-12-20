using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class Interactable : GameInteractionEvent, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform parent;
    [HideInInspector] public Vector2 originInParent;
    [HideInInspector] public Image mainImage;
    public int ID;
    public int siblingIndex;

    public virtual void ToggleRayCast(bool value) { return; }

    public virtual void OnBeginDrag(PointerEventData eventData) { return; }

    public virtual void OnDrag(PointerEventData eventData) { return; }

    public virtual void OnEndDrag(PointerEventData eventData) { return; }

    public virtual void OnPointerClick(PointerEventData eventData) { return; }

    public virtual void OnPointerEnter(PointerEventData eventData) { return; }

    public virtual void OnPointerExit(PointerEventData eventData) { return; }
}
