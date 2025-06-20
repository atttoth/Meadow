using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerHandView : HandView
{
    private static readonly int REQUIRED_NUM_OF_DISPOSABLE_CARDS = 2;
    private HandLayout _handLayout;
    private bool _isHandDefault;

    public override void Init()
    {
        base.Init();
        float cardWidth = GameResourceManager.Instance.cardPrefab.GetComponent<RectTransform>().rect.width;
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

    public void PlaceCardFromHandAction(Card card, bool isActionCancelled = false)
    {
        if (isActionCancelled)
        {
            AddCard(card);
        }
        else
        {
            RemoveCard(card);
        }
    }

    public override void AddCardHandler(GameTask task, List<Card> cards)
    {
        switch(task.State)
        {
            case 0:
                int duration = 0;
                float drawSpeed = GameSettings.Instance.GetDuration(Duration.cardDrawSpeedFromBoard);
                float posY = 45f;
                if (cards.Count > 1)
                {
                    float drawDelay = GameSettings.Instance.GetDuration(Duration.cardDrawSpeedDelayFromBoard);
                    duration = (int)(((cards.Count - 1) * drawDelay + drawSpeed) * 1000);
                    cards.ForEach(card => AddCard(card));
                    float[] positions = GetLayoutPositions();
                    int i = 0;
                    while (cards.Count > 0)
                    {
                        float delay = i * drawDelay;
                        Card card = cards.First();
                        cards.Remove(card);
                        float posX = positions[i];
                        card.transform.SetParent(transform);
                        card.MoveToPositionTween(new(posX, posY, 0f), drawSpeed, delay);
                        i++;
                    }
                }
                else
                {
                    Card card = cards.First();
                    duration = (int)(drawSpeed * 1000);
                    AddCard(card);
                    float[] positions = GetLayoutPositions();
                    MoveCardsHorizontallyInHand(positions, _cards.Count <= 10);
                    float posX = positions[^1];
                    card.transform.SetParent(transform);
                    card.MoveToPositionTween(new(posX, posY, 0f), drawSpeed);
                }
                task.StartDelayMs(duration);
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
            card.MoveCardHorizontallyTween(posX, isRapid);
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
        bool flag = !value;
        _cards.ForEach(card =>
        {
            card.canMove = flag;
            card.ToggleCanInspectFlag(flag);
        });
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

    public void EnableCardsRaycast(bool value)
    {
        _cards.ForEach(card => card.ToggleRayCast(value));
    }
}
