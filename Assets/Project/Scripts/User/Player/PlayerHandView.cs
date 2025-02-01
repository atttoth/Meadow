using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerHandView : MonoBehaviour
{
    private List<Card> _cards;

    public void Init()
    {
        _cards = new();
    }

    public void AddCardHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                MoveCardsHorizontallyInHand(false, false);
                float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardDrawSpeedFromBoard;
                Card card = task.Data.card;
                float[] endPositions = GetHandLayout();
                card.transform.SetParent(transform);
                float endPosition = endPositions.Length > 1 ? endPositions[^1] : endPositions[0];
                Vector3 targetPosition = _cards.Count < 1 ? new(0f, card.hoverOriginY, 0f) : new(endPosition, card.hoverOriginY, 0f);
                DOTween.Sequence().Append(card.transform.DOLocalMove(targetPosition, speed).SetEase(Ease.InOutBack));
                card.SavePosition(endPosition);
                _cards.Add(card);
                task.StartDelayMs((int)(speed * 1000));
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void SetCardsReady()
    {
        _cards.ForEach(card => card.SetCardReadyInHand());
    }

    public void ToggleHand()
    {
        _cards.ForEach(card => card.ToggleCard());
        MoveCardsHorizontallyInHand(true, true, true);
    }

    public void RemoveCardFromHand(GameTaskItemData data)
    {
        _cards.Remove(data.card);
    }

    public void RemoveCardFromHandRewind(GameTaskItemData data)
    {
        _cards.Add(data.card);
    }

    public void MoveCardsHorizontallyInHand(bool isVisible, bool isTableToggled, bool isUpdating = false)
    {
        if (_cards.Count < 1)
        {
            return;
        }

        float[] endPositions = isVisible ? GetSpreadedHandLayout() : GetHandLayout(isUpdating);
        foreach (Card card in _cards)
        {
            float endPosition = endPositions.Length > 1 ? endPositions[card.transform.GetSiblingIndex()] : endPositions[0];
            card.MoveCardHorizontally(endPosition, isTableToggled);
            card.SavePosition(endPosition);
        }
    }

    private float[] GetHandLayout(bool isUpdating = false)
    {
        int layout = isUpdating ? _cards.Count - 1 : _cards.Count;
        return layout switch
        {
            1 => new float[] { -80, 80 },
            2 => new float[] { -160, 0, 160 },
            3 => new float[] { -240, -80, 80, 240 },
            4 => new float[] { -320, -160, 0, 160, 320 },
            5 => new float[] { -200, -120, -40, 40, 120, 200 },
            6 => new float[] { -240, -160, -80, 0, 80, 160, 240 },
            7 => new float[] { -280, -200, -120, -40, 40, 120, 200, 280 },
            8 => new float[] { -320, -240, -160, -80, 0, 80, 160, 240, 320 },
            _ => new float[] { 0 },
        };
    }

    private float[] GetSpreadedHandLayout()
    {
        return _cards.Count switch
        {
            2 => new float[] { -80, 80 },
            3 => new float[] { -160, 0, 160 },
            4 => new float[] { -240, -80, 80, 240 },
            5 => new float[] { -320, -160, 0, 160, 320 },
            6 => new float[] { -400, -240, -80, 80, 240, 400 },
            7 => new float[] { -480, -320, -160, 0, 160, 320, 480 },
            8 => new float[] { -560, -400, -240, -80, 80, 240, 400, 560 },
            9 => new float[] { -640, -480, -320, -160, 0, 160, 320, 480, 640 },
            10 => new float[] { -720, -560, -400, -240, -80, 80, 240, 400, 560, 720 },
            11 => new float[] { -800, -640, -480, -320, -160, 0, 160, 320, 480, 640, 800 },
            12 => new float[] { -880, -720, -560, -400, -240, -80, 80, 240, 400, 560, 720, 880 },
            13 => new float[] { -600, -500, -400, -300, -200, -100, 0, 100, 200, 300, 400, 500, 600 },
            _ => new float[] { 0 },
        };
    }
}
