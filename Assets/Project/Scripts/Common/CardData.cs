using System;

[Serializable]
public class ParsedCardData
{
    public CardData[] collection;
}

[Serializable]
public class CardData
{
    public int ID;
    public DeckType deckType;
    public CardType cardType;
    public CardIcon[] requirements;
    public CardIcon[] optionalRequirements;
    public CardIcon[] adjacentRequirements;
    public CardIcon[] icons;
    public int score;
}
