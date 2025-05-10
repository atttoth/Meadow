using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NpcHandView : HandView
{
    public override void PlaceCardFromHandAction(object[] args)
    {
        Card card = (Card)args[3];
        RemoveCard(card);
    }

    public override void AddCardHandler(GameTask task, List<Card> cards)
    {
        switch (task.State)
        {
            case 0:
                int duration = 0;
                float drawSpeed = GameSettings.Instance.GetDuration(Duration.cardDrawSpeedFromBoard);
                if (cards.Count > 1)
                {
                    float drawDelay = GameSettings.Instance.GetDuration(Duration.cardDrawSpeedDelayFromBoard);
                    duration = (int)(((cards.Count - 1) * drawDelay + drawSpeed) * 1000);
                    cards.ForEach(card => AddCard(card));
                    int i = 0;
                    while (cards.Count > 0)
                    {
                        float delay = i * drawDelay;
                        Card card = cards.First();
                        cards.Remove(card);
                        card.transform.SetParent(transform);
                        card.MoveToPositionTween(Vector3.zero, drawSpeed, delay);
                        i++;
                    }
                }
                else
                {
                    Card card = cards.First();
                    duration = (int)(drawSpeed * 1000);
                    AddCard(card);
                    card.transform.SetParent(transform);
                    card.MoveToPositionTween(Vector3.zero, drawSpeed);
                }
                task.StartDelayMs(duration);
                break;
            default:
                _cards.ForEach(card =>
                {
                    card.gameObject.SetActive(false);
                    card.SetCardReadyInHand();
                });
                task.Complete();
                break;
        }
    }

    public Card GetLastCardInHand()
    {
        return _cards[^1];
    }
}
