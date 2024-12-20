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
    public List<Card> cards;
    private SpriteAtlas _atlas;
    public bool testOver;

    public void CreateDeck(int index, List<CardData> cardData, SpriteAtlas atlas)
    {
        deckType = (DeckType)index;
        cards = new();
        _atlas = atlas;
        foreach (CardData data in cardData)
        {
            CreateCardFromCardData(data);
        }
    }

    private void CreateCardFromCardData(CardData data)
    {
        Card card = Instantiate(GameAssets.Instance.cardPrefab, transform).GetComponent<Card>();
        card.ID = data.ID;
        card.transform.SetParent(transform);
        card.Data = data;
        card.hoverOriginY = -10f;
        card.hoverTargetY = 100f;
        card.canZoom = true;
        card.cardLocationStatus = CardLocationStatus.DEFAULT;
        card.canHover = false;
        card.cardFront = _atlas.GetSprite(data.ID.ToString());
        card.cardBack = _atlas.GetSprite("back");
        card.mainImage = card.GetComponent<Image>();
        card.mainImage.sprite = card.cardBack;
        card.highlightFrame = card.transform.GetChild(0).GetComponent<Image>();
        card.highlightFrame.color = Color.green;
        card.highlightFrame.gameObject.SetActive(false);
        card.drawText = card.transform.GetChild(1).gameObject;
        card.drawText.SetActive(false);
        card.gameObject.SetActive(false);
        cards.Add(card);
    }

    public Card GetRandomCard()
    {
        Random rand = new();
        int cardIndex = rand.Next(0, cards.Count - 1);
        Card card = cards[cardIndex];
        cards.RemoveAt(cardIndex);
        return card;
    }

    // for testing only!
    public Card GetCardByID(int id, DeckType type)
    {
        Card card = null;
        if (type == deckType && !testOver)
        {
            testOver = true;
            card = cards.Find(c => c.Data.ID == id);
        }

        if (card == null)
        {
            card = GetRandomCard();
        }
        return card;
    }
}
