using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.U2D;
using System.Threading.Tasks;

[System.Serializable]
public class DeckManager : MonoBehaviour
{
   private Dictionary<DeckType, Deck> _decks;
   private Dictionary<int, Transform> _dummyDecks;
   private List<CardData> _cardDataCollection;
   private Dictionary<DeckType, SpriteAtlas> _atlasList;

    public void CreateAtlasList()
    {
        _atlasList = new();
        for(int i = 0; i < 4; i++)
        {
            DeckType deckType = (DeckType)i;
            _atlasList.Add(deckType, GameAssets.Instance.GetAssetByName<SpriteAtlas>(deckType.ToString()));
        }
    }

    public void CreateDecks()
    {
        _cardDataCollection = ParseDataFromJSON();
        _decks = new();
        for(int index = 0; index < 4; index++)
        {
            GameObject obj = Instantiate(GameAssets.Instance.deckPrefab, transform).gameObject;
            obj.transform.SetParent(transform.GetChild(0));
            DeckType deckType = ((DeckType)index);
            obj.name = deckType.ToString();
            Deck deck = obj.GetComponent<Deck>();
            List<CardData> list = GetCardDataListByIndex(index);
            SpriteAtlas atlas = _atlasList[deckType];
            deck.CreateDeck(index, list, atlas);
            _decks.Add((DeckType)index, deck);
        }
    }

    public void CreateDummyDecks()
    {
        _dummyDecks = new();
        for(int i = 0; i < 4; i++)
        {
            Transform dummyDeck = transform.GetChild(1).GetChild(i);
            _dummyDecks.Add(i, dummyDeck);
        }
    }

    private List<CardData> GetCardDataListByIndex(int index)
    {
        List<CardData> list = new();
        foreach(CardData data in _cardDataCollection)
        {
            if((int)data.deckType == index)
            {
                list.Add(data);
            }
        }
        return list;
    }

    private List<CardData> ParseDataFromJSON()
    {
        List<CardData> collection = new();
        CardData cardData = JsonUtility.FromJson<CardData>(GameAssets.Instance.cardDataJson.text);

        foreach (CardData data in cardData.collection)
        {
            collection.Add(new CardData(
                data.ID,
                data.deckType,
                data.cardType,
                data.requirements,
                data.optionalRequirements,
                data.adjacentRequirements,
                data.icons,
                data.value
                ));
        }
        return collection;
    }

    public Deck GetDeckByDeckType(DeckType deckType)
    {
        return _decks[deckType];
    }

    public Transform GetDummyByDeck(DeckType deckType, int colIndex)
    {
        switch (deckType)
        {
            case DeckType.West:
                return _dummyDecks[0];
            case DeckType.East:
                return _dummyDecks[3];
            default:
                return colIndex == 1 ? _dummyDecks[1] : _dummyDecks[2];
        }
    }

    public List<Card> GetNewCardsFromDeck(int emptyHolders, DeckType deckType, int colIndex)
    {
        Deck deck = GetDeckByDeckType(deckType);
        Transform parent = GetDummyByDeck(deckType, colIndex);
        List<Card> cards = new();
        for(int i = 0; i < emptyHolders; i++)
        {
            Card card = deck.GetRandomCard();
            card.transform.SetParent(parent);
            card.transform.position = parent.position;
            card.ToggleRayCast(false);
            cards.Add(card);
        }
        return cards;
    }

    public List<Card> GetCardsReadyToDraw(int emptyHoldersCount, DeckType deckType, int colIndex)
    {
        return GetNewCardsFromDeck(emptyHoldersCount, deckType, colIndex);
    }
}

[Serializable]
public class CardData
{
    public CardData[] collection;
    public int ID;
    public DeckType deckType;
    public CardType cardType;
    public CardIcon[] requirements;
    public CardIcon[] optionalRequirements;
    public CardIcon[] adjacentRequirements;
    public CardIcon[] icons;
    public int value;

    public CardData(int ID, DeckType deckType, CardType cardType,
        CardIcon[] requirements, CardIcon[] optionalRequirements, CardIcon[] adjacentRequirements, CardIcon[] icons, int value)
    {
        this.ID = ID;
        this.deckType = deckType;
        this.cardType = cardType;
        this.requirements = requirements;
        this.optionalRequirements = optionalRequirements;
        this.adjacentRequirements = adjacentRequirements;
        this.icons = icons;
        this.value = value;
    }
}
