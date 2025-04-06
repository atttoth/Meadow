using DG.Tweening;

public class NpcHandView : HandView
{
    public override void AddCardHandler(GameTask task, Card card)
    {
        switch (task.State)
        {
            case 0:
                float drawSpeed = ReferenceManager.Instance.gameLogicController.GameSettings.cardDrawSpeedFromBoard;
                AddCard(card);
                card.transform.SetParent(transform);
                DOTween.Sequence().Append(card.transform.DOMove(transform.position, drawSpeed).SetEase(Ease.InOutBack));
                task.StartDelayMs((int)(drawSpeed * 1000));
                break;
            default:
                card.gameObject.SetActive(false);
                task.Complete();
                break;
        }
    }

    public Card GetLastCardInHand()
    {
        return _cards[^1];
    }
}
