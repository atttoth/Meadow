using System.Collections.Generic;
using UnityEngine;

public class SelectionScreenLayout
{
    private static readonly float GAP = 150f;
    private readonly RectTransform _displayTransform;
    private readonly RectTransform _cardTransform;

    public SelectionScreenLayout(RectTransform displayTransform, RectTransform cardTransform)
    {
        _displayTransform = displayTransform;
        _cardTransform = cardTransform;
    }

    public List<Vector2> GetCenteredPositions(int numOfItems)
    {
        float itemsWidth = ((numOfItems - 1) * GAP) + (numOfItems * _cardTransform.rect.width);
        float startingPosX = (_displayTransform.rect.width - itemsWidth) * 0.5f;
        float posY = _displayTransform.rect.height * 0.5f;
        List<Vector2> positions = new();
        for (int i = 0; i < numOfItems; i++)
        {
            Vector2 position = new(startingPosX + (i * GAP) + (i * _cardTransform.rect.width) + (_cardTransform.rect.width * 0.5f), posY);
            positions.Add(position);
        }
        return positions;
    }

    public float GetPosYOffset()
    {
        return _cardTransform.rect.height + GAP * 0.5f;
    }
}
