using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using Random = System.Random;

public enum DeckType
{
    West,
    South,
    East,
    North,
    NUM_OF_DECKS
}

public class Deck : MonoBehaviour
{
    public DeckType deckType;
    private List<Card> _cards;

    public void Init(int index, List<CardData> cardData)
    {
        deckType = (DeckType)index;
        _cards = new();
        SpriteAtlas atlas = GameResourceManager.Instance.GetAssetByName<SpriteAtlas>(deckType.ToString());
        cardData.ForEach(data =>
        {
            Card card = Instantiate(GameResourceManager.Instance.cardPrefab, transform).GetComponent<Card>();
            card.Init(data, atlas.GetSprite(data.ID.ToString()), atlas.GetSprite("back"));
            AddCard(card);
        });
    }

    public void AddCard(Card card)
    {
        _cards.Add(card);
    }

    public Card GetRandomCard()
    {
        Random rand = new();
        int cardIndex = rand.Next(0, _cards.Count - 1);
        Card card = _cards[cardIndex];
        _cards.Remove(card);
        return card;
    }

    public Card GetCardByID(int cardID)
    {
        Card card = _cards.Find(c => c.Data.ID == cardID);
        _cards.Remove(card);
        return card;
    }
}
