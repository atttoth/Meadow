using System;
using UnityEngine;

public enum GameLogicEventType
{
    TURN_STARTED,
    TURN_ENDED,
    ROUND_ENDED,
    GAME_ENDED,
    TABLE_TOGGLED,
    CAMP_ICONS_SELECTED,
    CAMP_TOGGLED,
    CAMP_SCORE_RECEIVED,
    TABLE_HITAREA_HOVERED_OVER,
    ROW_PICKED,
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
    MARKER_PLACED,
    MARKER_CANCELLED,
    MARKER_ACTION_SELECTED,
    DECK_SELECTED,
    SCORE_COLLECTED,
    HAND_SCREEN_TOGGLED
}

public class GameLogicEvent : MonoBehaviour
{
    private event EventHandler<object[]> _logicEventHandler;

    private void Start()
    {
        _logicEventHandler += ReferenceManager.Instance.gameLogicController.OnLogicEvent;
    }

    protected void StartEventHandler(GameLogicEventType type, object[] args)
    {
        _logicEventHandler.Invoke(Enum.ToObject(typeof(GameLogicEventType), type), args);
    }
}
