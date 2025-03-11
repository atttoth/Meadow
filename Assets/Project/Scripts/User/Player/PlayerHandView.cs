using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerHandView : MonoBehaviour
{
    private static readonly int REQUIRED_NUM_OF_DISPOSABLE_CARDS = 2;
    private List<Card> _cards;
    private HandLayout _handLayout;
    private bool _isHandDefault;

    public void Init()
    {
        _cards = new();
        float cardWidth = GameAssets.Instance.cardPrefab.GetComponent<RectTransform>().rect.width;
        _handLayout = new HandLayout(cardWidth);
        _isHandDefault = true;
    }

    public int GetNumberOfCards()
    {
        return _cards.Count;
    }

    public List<CardData> GetDataCollection()
    {
        return _cards.Select(card => card.Data).ToList();
    }

    public void AddCardHandler(GameTask task, Card card)
    {
        switch(task.State)
        {
            case 0:
                float drawSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardDrawSpeedFromBoard;
                _cards.Add(card);
                float[] positions = GetLayoutPositions();
                MoveCardsHorizontallyInHand(positions, _cards.Count <= 10);
                float newCardPosition = positions[^1];
                card.transform.SetParent(transform);
                DOTween.Sequence().Append(card.transform.DOLocalMove(new(newCardPosition, card.hoverOriginY), drawSpeed).SetEase(Ease.InOutBack));
                task.StartDelayMs((int)(drawSpeed * 1000));
                break;
            default:
                _cards.ForEach(card => card.SetCardReadyInHand());
                task.Complete();
                break;
        }
    }

    public void ToggleHand()
    {
        _isHandDefault = !_isHandDefault;
        _cards.ForEach(card => card.ToggleCard());
        MoveCardsHorizontallyInHand(GetLayoutPositions());
    }

    public void RemoveCardFromHand(params object[] args)
    {
        _cards.Remove((Card)args[2]);
    }

    public void RemoveCardFromHandRewind(object[] args)
    {
        _cards.Add((Card)args[2]);
    }

    public float[] GetLayoutPositions()
    {
        return _isHandDefault ? _handLayout.GetDefaultLayout(_cards.Count) : _handLayout.GetSpreadedLayout(_cards.Count);
    }

    public void MoveCardsHorizontallyInHand(float[] positions, bool isRapid = false)
    {
        if (positions.Length < 1)
        {
            return;
        }

        for (int i = 0; i < positions.Length; i++)
        {
            Card card = _cards[i];
            float posX = positions[i];
            card.MoveCardHorizontally(posX, isRapid);
        }
    }

    public void ToggleDisposableFlagOnCards(bool value)
    {
        _cards.ForEach(card =>
        {
            card.isDisposable = value;
            if (!value)
            {
                card.ResetDisposeSelection();
            }
        });
    }

    public void ToggleBehaviorFlagsOnCards(bool value)
    {
        _cards.ForEach(card =>
        {
            card.canMove = value;
            if(!value)
            {
                card.ToggleCanInspectFlag(false);
            }
        });
    }

    public void ResetIsInspectedFlagOnCard()
    {
        _cards.ForEach(card => card.ToggleIsInspectedFlag(false));
    }

    public bool HasDisposableCardsSelected()
    {
        return _cards.Where(card => card.selectedToDispose).ToList().Count == REQUIRED_NUM_OF_DISPOSABLE_CARDS;
    }

    public List<Card> GetDisposableCards()
    {
        List<Card> cards = _cards.Where(card => card.selectedToDispose).ToList();
        for(int i = cards.Count - 1; i >= 0; i--)
        {
            _cards.Remove(cards[i]);
        }
        return cards;
    }

    public Card GetInspectedCard()
    {
        return _cards.Find(card => card.isInspected);
    }
}
