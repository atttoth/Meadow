using System;
using UnityEngine;

public enum GameLogicEventType
{
    CAMP_ICONS_SELECTED,
    CAMP_SCORE_RECEIVED,
    CARD_PICKED,
    CARD_PLACED,
    CARD_EXAMINED,
    APPROVED_PENDING_CARD_PLACED,
    CANCELLED_PENDING_CARD_PLACED,
    MARKER_PLACED,
    MARKER_CANCELLED,
    MARKER_ACTION_SELECTED,
    DECK_SELECTED
}

public class GameLogicEvent : MonoBehaviour
{
    private event EventHandler<GameTaskItemData> _logicEventHandler;

    private void Awake()
    {
        _logicEventHandler += ReferenceManager.Instance.gameLogicManager.OnLogicEvent;
    }

    protected void StartEventHandler(GameLogicEventType type, GameTaskItemData data)
    {
        _logicEventHandler.Invoke(Enum.ToObject(typeof(GameLogicEventType), type), data);
    }
}
