using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DeckController : MonoBehaviour
{
    private static readonly List<int> INITIAL_GROUND_CARD_IDS = new() { 217, 218 };
    private List<CardData>[] _cardDataByDeckType;
    private List<CardData> _initialGroundCardData;
    private Dictionary<DeckType, Deck> _decks;
    private Dictionary<int, Transform> _displayDecks;

    public List<CardData> InitialGroundCardData { get  { return _initialGroundCardData; } }

    public void Init()
    {
        _cardDataByDeckType = new List<CardData>[] { new(), new(), new(), new() };
        ParseDataFromJSON().ForEach(data => _cardDataByDeckType[(int)data.deckType].Add(data));
        _initialGroundCardData = _cardDataByDeckType[(int)DeckType.East].Where(data => Array.Exists(INITIAL_GROUND_CARD_IDS.ToArray(), ID => ID == data.ID)).ToList();
        _initialGroundCardData.ForEach(data => _cardDataByDeckType[(int)DeckType.East].Remove(data));

        _displayDecks = new();
        for (int i = -1; i < (int)DeckType.NUM_OF_DECKS; i++)
        {
            Transform dummyDeck = transform.GetChild(1).GetChild(i + 1);
            _displayDecks.Add(i, dummyDeck);
        }
        CreateDecks();
    }

    public void CreateDecks()
    {
        _decks = new();
        for (int index = 0; index < _cardDataByDeckType.Length; index++)
        {
            DeckType deckType = ((DeckType)index);
            Transform deckPrefab = Instantiate(GameResourceManager.Instance.deckPrefab, transform);
            deckPrefab.SetParent(transform.GetChild(0));
            deckPrefab.name = deckType.ToString();
            Deck deck = deckPrefab.GetComponent<Deck>();
            deck.Init(index, _cardDataByDeckType[index]);
            _decks.Add(deckType, deck);
        }
    }

    private List<CardData> ParseDataFromJSON()
    {
        ParsedCardData parsedData = JsonUtility.FromJson<ParsedCardData>(GameResourceManager.Instance.cardDataJson.text);
        return parsedData.collection.ToList();
    }

    public Deck GetDeckByDeckType(DeckType deckType)
    {
        return _decks[deckType];
    }

    public Transform GetDisplayDeckTransform(int colIndex)
    {
        return _displayDecks[colIndex];
    }

    public Card GetCardFromDeck(DeckType deckType, int cardID = -1)
    {
        Deck deck = GetDeckByDeckType(deckType);
        if(cardID > -1)
        {
            return deck.GetCardByID(cardID);
        }
        else
        {
            return deck.GetRandomCard();
        }
    }
}
