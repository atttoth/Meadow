using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;
using UnityEngine.UI;
using UnityEngine.U2D;

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
    private SpriteAtlas _atlas;
    public bool testOver;

    public void CreateDeck(int index, List<CardData> cardData, SpriteAtlas atlas)
    {
        deckType = (DeckType)index;
        _cards = new();
        _atlas = atlas;
        foreach (CardData data in cardData)
        {
            CreateCardFromCardData(data);
        }
    }

    private void CreateCardFromCardData(CardData data)
    {
        Card card = Instantiate(GameAssets.Instance.cardPrefab, transform).GetComponent<Card>();
        card.Init(data, _atlas.GetSprite(data.ID.ToString()), _atlas.GetSprite("back"));
        _cards.Add(card);
    }

    public Card GetRandomCard()
    {
        Random rand = new();
        int cardIndex = rand.Next(0, _cards.Count - 1);
        Card card = _cards[cardIndex];
        _cards.RemoveAt(cardIndex);
        return card;
    }

    // for testing only!
    public Card GetCardByID(int id, DeckType type)
    {
        Card card = null;
        if (type == deckType && !testOver)
        {
            testOver = true;
            card = _cards.Find(c => c.Data.ID == id);
        }

        if (card == null)
        {
            card = GetRandomCard();
        }
        return card;
    }
}
