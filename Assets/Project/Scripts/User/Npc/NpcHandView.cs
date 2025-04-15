using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NpcHandView : HandView
{
    public override void AddCardHandler(GameTask task, List<Card> cards)
    {
        switch (task.State)
        {
            case 0:
                int duration = 0;
                float drawSpeed = ReferenceManager.Instance.gameLogicController.GameSettings.cardDrawSpeedFromBoard;
                Vector3 parentPosition = transform.parent.GetComponent<RectTransform>().position;
                if (cards.Count > 1)
                {
                    float drawDelay = ReferenceManager.Instance.gameLogicController.GameSettings.cardDrawSpeedDelayFromBoard;
                    duration = (int)(((cards.Count - 1) * drawDelay + drawSpeed) * 1000);
                    cards.ForEach(card => AddCard(card));
                    int i = 0;
                    while (cards.Count > 0)
                    {
                        float delay = i * drawDelay;
                        Card card = cards.First();
                        cards.Remove(card);
                        card.transform.SetParent(transform);
                        DOTween.Sequence().Append(card.transform.DOLocalMove(parentPosition, drawSpeed).SetDelay(delay).SetEase(Ease.InOutBack));
                        i++;
                    }
                }
                else
                {
                    Card card = cards.First();
                    duration = (int)(drawSpeed * 1000);
                    AddCard(card);
                    card.transform.SetParent(transform);
                    DOTween.Sequence().Append(card.transform.DOLocalMove(parentPosition, drawSpeed).SetEase(Ease.InOutBack));
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
