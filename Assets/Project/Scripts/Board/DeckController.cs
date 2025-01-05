using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;

[System.Serializable]
public class DeckController : MonoBehaviour
{
    private Dictionary<DeckType, Deck> _decks;
    private Dictionary<int, Transform> _displayDecks;
    private List<Card> _topCards;

    public List<Card> TopCards
    {
        get { return _topCards; }
        set { _topCards = value; }
    }

    public void Init()
    {
        List<CardData> cardDataCollection = ParseDataFromJSON();
        _decks = new();
        for (int index = 0; index < (int)DeckType.NUM_OF_DECKS; index++)
        {
            DeckType deckType = ((DeckType)index);
            SpriteAtlas atlas = GameAssets.Instance.GetAssetByName<SpriteAtlas>(deckType.ToString());
            GameObject obj = Instantiate(GameAssets.Instance.deckPrefab, transform).gameObject;
            obj.transform.SetParent(transform.GetChild(0));
            obj.name = deckType.ToString();
            Deck deck = obj.GetComponent<Deck>();
            List<CardData> list = cardDataCollection.Where(data => (int)data.deckType == index).ToList();
            deck.Init(index, list, atlas);
            _decks.Add((DeckType)index, deck);
        }

        _displayDecks = new();
        for (int i = 0; i < (int)DeckType.NUM_OF_DECKS; i++)
        {
            Transform dummyDeck = transform.GetChild(1).GetChild(i);
            _displayDecks.Add(i, dummyDeck);
        }
    }

    private List<CardData> ParseDataFromJSON()
    {
        List<CardData> collection = new();
        CardData cardData = JsonUtility.FromJson<CardData>(GameAssets.Instance.cardDataJson.text);
        cardData.collection.ToList().ForEach(data =>
        {
            collection.Add(new CardData(
                data.ID,
                data.deckType,
                data.cardType,
                data.requirements,
                data.optionalRequirements,
                data.adjacentRequirements,
                data.icons,
                data.score
                ));
        });
        return collection;
    }

    private Deck GetDeckByDeckType(DeckType deckType)
    {
        return _decks[deckType];
    }

    private Transform GetDisplayDeck(DeckType deckType, int colIndex)
    {
        switch (deckType)
        {
            case DeckType.West:
                return _displayDecks[0];
            case DeckType.East:
                return _displayDecks[3];
            default:
                return colIndex == 1 ? _displayDecks[1] : _displayDecks[2];
        }
    }

    public List<Card> GetCardsReadyToDraw(int emptyHoldersCount, DeckType deckType, int colIndex)
    {
        Deck deck = GetDeckByDeckType(deckType);
        Transform parent = GetDisplayDeck(deckType, colIndex);
        List<Card> cards = new();
        for (int i = 0; i < emptyHoldersCount; i++)
        {
            Card card = deck.GetRandomCard();
            card.transform.SetParent(parent);
            card.transform.position = parent.position;
            card.ToggleRayCast(false);
            cards.Add(card);
        }
        return cards;
    }

    public Card GetCardFromDeck(DeckType deckType)
    {
        return GetDeckByDeckType(deckType).GetRandomCard();
    }

    public void ClearTopCards()
    {
        DeckType deckType = _topCards.First().Data.deckType;
        Deck deck = _decks[deckType];
        _topCards.ForEach(card =>
        {
            card.transform.SetParent(deck.transform);
            card.GetComponent<RectTransform>().anchoredPosition = new(0f, 0f);
            deck.AddCard(card);
        });
        _topCards.Clear();
    }
}
