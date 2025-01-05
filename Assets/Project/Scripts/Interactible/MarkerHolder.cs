using System;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum MarkerDirection
{
    RIGHT,
    LEFT,
    MIDDLE
}

public class MarkerHolder : Holder, IPointerEnterHandler, IPointerExitHandler, IScrollHandler
{
    private event EventHandler<InteractableHolderEventArgs> _interactionEventHandler;
    private Image _mainImage;
    private MarkerDirection _direction;

    public class InteractableHolderEventArgs
    {
        public bool isHoverIn;
        public int scrollDirection;
    }

    public MarkerDirection Direction { get { return _direction; } }

    public override void Init(int id, HolderType type)
    {
        base.Init(id, type);
        _interactionEventHandler += ReferenceManager.Instance.gameLogicManager.OnMarkerHolderInteraction;
        _mainImage = GetComponent<Image>();
        switch (transform.parent.gameObject.tag)
        {
            case "LeftHolder":
                _direction = MarkerDirection.LEFT;
                break;
            case "RightHolder":
                _direction = MarkerDirection.RIGHT;
                break;
            default:
                _direction = MarkerDirection.MIDDLE;
                break;
        }
    }

    private bool IsAvailable()
    {
        foreach (Marker marker in _contentList)
        {
            if (marker.Status == MarkerStatus.PLACED || marker.Status == MarkerStatus.USED)
            {
                return false;
            }
        }
        return true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (IsAvailable())
        {
            InteractableHolderEventArgs args = new();
            args.isHoverIn = true;
            _interactionEventHandler?.Invoke(this, args);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsAvailable())
        {
            InteractableHolderEventArgs args = new();
            args.isHoverIn = false;
            _interactionEventHandler?.Invoke(this, args);
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        InteractableHolderEventArgs args = new();
        args.scrollDirection = (int)eventData.scrollDelta.y;
        _interactionEventHandler?.Invoke(this, args);
    }

    public void ToggleRayCast(bool value)
    {
        if (IsEmpty())
        {
            _mainImage.raycastTarget = value;
        }
    }
}
