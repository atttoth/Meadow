using System;
using UnityEngine;

public enum GameLogicEventType
{
    TABLE_TOGGLED,
    CAMP_ICONS_SELECTED,
    CAMP_TOGGLED,
    CAMP_SCORE_RECEIVED,
    TABLE_HITAREA_HOVERED_OVER,
    CARD_PICKED,
    CARD_MOVED,
    CARD_PLACED,
    CARD_INSPECTION_STARTED,
    CARD_INSPECTION_ENDED,
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
    private event EventHandler<GameTaskItemData> _logicEventHandler;

    private void Start()
    {
        _logicEventHandler += ReferenceManager.Instance.gameLogicManager.OnLogicEvent;
    }

    protected void StartEventHandler(GameLogicEventType type, GameTaskItemData data)
    {
        _logicEventHandler.Invoke(Enum.ToObject(typeof(GameLogicEventType), type), data);
    }
}
