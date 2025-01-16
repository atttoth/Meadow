using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class PlayerHandView : ViewBase
{
    private Transform _handTransform;
    private List<Card> _cardListInHand;
    public CardType draggingCardType;

    public override void Init()
    {
        _handTransform = GetComponent<Transform>();
        _cardListInHand = new();
        draggingCardType = CardType.None;
    }

    public async void AddCardToHand(Card card)
    {
        float[] endPositions = GetHandLayout();
        Sequence sequence = DOTween.Sequence();

        card.transform.SetParent(_handTransform);
        float endPosition = endPositions.Length > 1 ? endPositions[^1] : endPositions[0];
        Vector3 targetPosition = _cardListInHand.Count < 1 ? new(0f, card.hoverOriginY, 0f) : new(endPosition, card.hoverOriginY, 0f);
        sequence.Append(card.transform.DOLocalMove(targetPosition, ReferenceManager.Instance.gameLogicManager.GameSettings.cardDrawSpeedFromBoard).SetEase(Ease.InOutBack));
        sequence.Play();
        await sequence.AsyncWaitForCompletion();
        card.SavePosition(endPosition);
        _cardListInHand.Add(card);
    }

    public async void ToggleHand(Card currentCard = null)
    {
        List<Task> tasks = new();
        foreach (Card card in _cardListInHand)
        {
            if (currentCard != card)
            {
                float value = card.canHover ? card.hoverTargetY : card.hoverOriginY;
                tasks.Add(card.MoveCardWithAsyncDelay(value));
            }
            card.canHover = !card.canHover;
            card.canMove = !card.canMove;
        }

        await Task.WhenAll(tasks);
        MoveCardsHorizontallyInHand(true, true, true);
    }

    public void SetCardsReady()
    {
        foreach (Card card in _cardListInHand)
        {
            card.SetCardReadyInHand();
        }
    }

    public void RemoveCardFromHand(GameTaskItemData data)
    {
        _cardListInHand.Remove(data.card);
    }

    public void RemoveCardFromHandRewind(GameTaskItemData data)
    {
        _cardListInHand.Add(data.card);
    }

    public void MoveCardsHorizontallyInHand(bool isVisible, bool isTableToggled, bool isUpdating = false)
    {
        if (_cardListInHand.Count < 1)
        {
            return;
        }

        float[] endPositions = isVisible ? GetSpreadedHandLayout() : GetHandLayout(isUpdating);
        foreach (Card card in _cardListInHand)
        {
            float endPosition = endPositions.Length > 1 ? endPositions[card.transform.GetSiblingIndex()] : endPositions[0];
            card.MoveCardHorizontally(endPosition, isTableToggled);
            card.SavePosition(endPosition);
        }
    }

    private float[] GetHandLayout(bool isUpdating = false)
    {
        int layout = isUpdating ? _cardListInHand.Count - 1 : _cardListInHand.Count;
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
        return _cardListInHand.Count switch
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
