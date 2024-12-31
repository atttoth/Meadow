using System;

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
    public int score;

    public CardData(
        int ID,
        DeckType deckType,
        CardType cardType,
        CardIcon[] requirements,
        CardIcon[] optionalRequirements,
        CardIcon[] adjacentRequirements,
        CardIcon[] icons,
        int score
        )
    {
        this.ID = ID;
        this.deckType = deckType;
        this.cardType = cardType;
        this.requirements = requirements;
        this.optionalRequirements = optionalRequirements;
        this.adjacentRequirements = adjacentRequirements;
        this.icons = icons;
        this.score = score;
    }
}
