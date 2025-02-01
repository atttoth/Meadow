using System;
using UnityEngine;

public class TableLayout
{
    private static float _Y_GAP_BETWEEN_STACKED_CARDS = 60f;
    public static float SLIDE_X_DISTANCE_OF_DISPLAY_ICON = 30f;

    private float _tableClosedPosY;
    private float _tableOpenPosY;

    public TableLayout(float closedPosY, float openPosY)
    {
        _tableClosedPosY = closedPosY;
        _tableOpenPosY = openPosY;
    }

    public float GetTargetTableViewPosY(bool isTableVisible)
    {
        return isTableVisible ? _tableOpenPosY : _tableClosedPosY;
    }

    public Vector3 GetUpdatedPrimaryHolderPosition(int modifier)
    {
        return new Vector3(0f, _Y_GAP_BETWEEN_STACKED_CARDS * 0.5f * modifier, 0f);
    }

    public Vector2 GetUpdatedPrimaryHolderSize(RectTransform rect, int modifier)
    {
        return new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + (_Y_GAP_BETWEEN_STACKED_CARDS * modifier));
    }

    public Vector2 GetCardTargetPosition(Card card, int contentCount, Transform handTransform)
    {
        if (handTransform == null)
        {
            float startingPosY = Array.Exists(new CardType[] { CardType.Landscape, CardType.Discovery }, type => type == card.Data.cardType) ? 70f : 105f;
            float posY = contentCount < 1 ? startingPosY : startingPosY + _Y_GAP_BETWEEN_STACKED_CARDS * contentCount;
            return new(0f, posY);
        }
        else
        {
            card.transform.SetParent(handTransform);
            return new(card.prevPosition.x, card.prevPosition.y);
        }
    }

    public float[] GetPrimaryCardHolderLayout(int holderCount)
    {
        return holderCount switch
        {
            2 => new float[] { -95, 95 },
            3 => new float[] { -190, 0, 190 },
            4 => new float[] { -285, -95, 95, 285 },
            5 => new float[] { -380, -190, 0, 190, 380 },
            6 => new float[] { -475, -285, -95, 95, 285, 475 },
            7 => new float[] { -570, -380, -190, 0, 190, 380, 570 },
            8 => new float[] { -665, -475, -285, -95, 95, 285, 475, 665 },
            9 => new float[] { -760, -570, -380, -190, 0, 190, 380, 570, 760 },
            10 => new float[] { -855, -665, -475, -285, -95, 95, 285, 475, 665, 855 },
            _ => new float[] { 0 },
        };
    }
}
