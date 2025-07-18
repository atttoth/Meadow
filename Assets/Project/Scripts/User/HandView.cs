using System.Collections.Generic;
using UnityEngine;

public abstract class HandView : MonoBehaviour
{
    protected List<Card> _cards;

    public List<Card> Cards { get { return _cards; } }

    public virtual void Init()
    {
        _cards = new();
    }

    public abstract void AddCardHandler(GameTask task, List<Card> cards);

    public void AddCard(Card card)
    {
        _cards.Add(card);
    }

    public void RemoveCard(Card card)
    {
        _cards.Remove(card);
    }
}
