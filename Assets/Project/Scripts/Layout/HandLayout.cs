
public class HandLayout
{
    private readonly float _cardWidth;

    public HandLayout(float cardWidth)
    {
        _cardWidth = cardWidth;
    }

    public float[] GetDefaultLayout(int numOfCards)
    {
        float totalWidth;
        float space;
        if(numOfCards > 10)
        {
            totalWidth = 0f;
            space = 0f;
        }
        else if (numOfCards > 5)
        {
            totalWidth = _cardWidth * 4;
            space = totalWidth / (numOfCards - 1);
        }
        else
        {
            totalWidth = (_cardWidth * (numOfCards - 1)) + (numOfCards - 1);
            space = _cardWidth;
        }

        float startingPosX = totalWidth * -0.5f;
        float[] positions = new float[numOfCards];
        for (int i = 0; i < numOfCards; i++)
        {
            float position = startingPosX + (space * i);
            positions[i] = position;
        }
        return positions;
    }

    public float[] GetSpreadedLayout(int numOfCards)
    {
        float totalWidth;
        float space;
        float gap = 5f;
        if (numOfCards > 10)
        {
            totalWidth = (_cardWidth + gap) * 9;
            space = totalWidth / (numOfCards - 1);
        }
        else
        {
            totalWidth = (_cardWidth * (numOfCards - 1)) + (gap * (numOfCards - 1));
            space = _cardWidth + gap;
        }

        float startingPosX = totalWidth * -0.5f;
        float[] positions = new float[numOfCards];
        for (int i = 0; i < numOfCards; i++)
        {
            float position = startingPosX + (space * i);
            positions[i] = position;
        }
        return positions;
    }
}
