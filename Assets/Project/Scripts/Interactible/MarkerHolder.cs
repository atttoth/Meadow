using UnityEngine;
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
    private LogicEventDispatcher _dispatcher;
    private Image _mainImage;
    private MarkerDirection _direction;

    public MarkerDirection Direction { get { return _direction; } }

    public override void Init(int id, HolderType type)
    {
        base.Init(id, type);
        _dispatcher = new();
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
            _dispatcher.InvokeEventHandler(GameLogicEventType.MARKER_HOLDER_TRIGGERED, new object[] { this, true, 0 });
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (IsAvailable())
        {
            _dispatcher.InvokeEventHandler(GameLogicEventType.MARKER_HOLDER_TRIGGERED, new object[] { this, false, 0 });
        }
    }

    public void OnScroll(PointerEventData eventData)
    {
        if (IsAvailable())
        {
            _dispatcher.InvokeEventHandler(GameLogicEventType.MARKER_HOLDER_TRIGGERED, new object[] { this, false, (int)eventData.scrollDelta.y });
        }
    }

    public void ToggleRayCast(bool value)
    {
        _mainImage.raycastTarget = value;
    }
}
