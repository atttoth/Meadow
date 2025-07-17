using System;

public enum GameLogicEventType
{
    TURN_ENDED,
    TABLE_TOGGLED,
    CAMP_TOGGLED,
    CAMP_SCORE_RECEIVED,
    TABLE_HITAREA_HOVERED_OVER,
    CARD_PICKED,
    CARD_MOVED,
    CARD_PLACED,
    CARD_INSPECTION_STARTED,
    CARD_INSPECTION_ENDED,
    CARD_ICON_CLICKED,
    CARD_SELECTED_FOR_DISPOSE,
    REMOVED_CARD_ICON,
    APPROVED_PENDING_CARD_PLACED,
    CANCELLED_PENDING_CARD_PLACED,
    MARKER_HOLDER_TRIGGERED,
    MARKER_PLACED,
    MARKER_CANCELLED,
    MARKER_ACTION_SELECTED,
    SCORE_COLLECTED,
    HAND_SCREEN_TOGGLED
}

public class GameEventController
{
    private event EventHandler<object[]> _logicEventHandler;

    public GameEventController()
    {
        _logicEventHandler += GameResourceManager.Instance.gameLogicManager.OnLogicEvent;
    }

    public void InvokeEventHandler(object eventType, object[] args)
    {
        Type type = eventType.GetType();
        switch (type)
        {
            case Type value when type == typeof(GameLogicEventType):
                _logicEventHandler.Invoke(Enum.ToObject(typeof(GameLogicEventType), eventType), args);
                break;
            default:
                break;
        }
    }
}
