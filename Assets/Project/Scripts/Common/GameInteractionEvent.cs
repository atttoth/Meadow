using System;
using UnityEngine;

public enum GameEventType
{
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

public class GameInteractionEvent : MonoBehaviour
{
    private event EventHandler<GameTaskItemData> _interactionEventHandler;

    private void Awake()
    {
        _interactionEventHandler += ReferenceManager.Instance.gameLogicManager.OnEvent;
    }

    protected void StartEventHandler(GameEventType type, GameTaskItemData data)
    {
        _interactionEventHandler.Invoke(Enum.ToObject(typeof(GameEventType), type), data);
    }
}
