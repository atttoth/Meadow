using System;
using UnityEngine;

public class TableLayout
{
    private readonly static float _Y_GAP_BETWEEN_STACKED_CARDS = 60f;
    private readonly static float _X_GAP_BETWEEN_HOLDERS = 27.7F;
    public readonly static float SLIDE_X_DISTANCE_OF_DISPLAY_ICON = 30f;

    private readonly float _tableClosedPosY;
    private readonly float _tableOpenPosY;
    private readonly float _tableWidth;
    private readonly float _primaryHolderWidth;
    private readonly float _secondaryHolderWidth;

    public TableLayout(float tableClosedPosY, float tableOpenPosY, float tableWidth, float primaryHolderWidth, float secondaryHolderWidth)
    {
        _tableClosedPosY = tableClosedPosY;
        _tableOpenPosY = tableOpenPosY;
        _tableWidth = tableWidth;
        _primaryHolderWidth = primaryHolderWidth;
        _secondaryHolderWidth = secondaryHolderWidth;
    }

    public float GetTargetTableViewPosY(bool isTableVisible)
    {
        return isTableVisible ? _tableOpenPosY : _tableClosedPosY;
    }

    public Vector3 GetUpdatedPrimaryHolderPosition(int modifier)
    {
        return new Vector3(0f, _Y_GAP_BETWEEN_STACKED_CARDS * 0.5f * modifier, 0f);
    }

    public Vector2 GetUpdatedPrimaryHolderSize(RectTransform transform, int modifier)
    {
        return new Vector2(transform.sizeDelta.x, transform.sizeDelta.y + (_Y_GAP_BETWEEN_STACKED_CARDS * modifier));
    }

    public Vector2 GetCardTargetPosition(Card card, int contentCount, bool isPlacement, float lastPosX)
    {
        if (isPlacement)
        {
            float startingPosY = Array.Exists(new CardType[] { CardType.Landscape, CardType.Discovery }, type => type == card.Data.cardType) ? 70f : 105f;
            float posY = contentCount < 1 ? startingPosY : startingPosY + _Y_GAP_BETWEEN_STACKED_CARDS * contentCount;
            return new(0f, posY);
        }
        else
        {
            return new(lastPosX, card.hoverTargetY * 2);
        }
    }

    public Vector2 GetPrimaryHolderPosition(RectTransform prevHolderTransform, int direction, float posY)
    {
        float posX = prevHolderTransform ? prevHolderTransform.anchoredPosition.x + ((_primaryHolderWidth + _X_GAP_BETWEEN_HOLDERS) * direction) : 0f;
        return new Vector2(posX, posY);
    }

    public Vector2 GetSecondaryCardHolderPosition(int holderCount, float posY)
    {
        float startingPosX = -((_tableWidth * 0.5f) - ((_secondaryHolderWidth * 0.5f) + _X_GAP_BETWEEN_HOLDERS));
        float posX = startingPosX + ((_secondaryHolderWidth + _X_GAP_BETWEEN_HOLDERS) * holderCount);
        return new(posX, posY);
    }

    public float[] GetPrimaryCardHolderLayout(int numOfHolders)
    {
        float totalWidth = (_primaryHolderWidth * (numOfHolders - 1)) + (_X_GAP_BETWEEN_HOLDERS * (numOfHolders - 1));
        float startingPosX = totalWidth * -0.5f;
        float[] positions = new float[numOfHolders];
        for(int i = 0; i < numOfHolders; i++)
        {
            float position = startingPosX + ((_primaryHolderWidth + _X_GAP_BETWEEN_HOLDERS) * i);
            positions[i] = position;
        }
        return positions;
    }

    public Vector2 GetPrimaryHitAreaSize(int numOfHolders, int status, float posY)
    {
        int count = status == 1 ? numOfHolders : numOfHolders - 1;
        float mod = status == 1 ? numOfHolders - 1 : numOfHolders > 2 ? numOfHolders - 2 : 0;
        float totalWidth = (_primaryHolderWidth * count) + (_X_GAP_BETWEEN_HOLDERS * mod);
        float width = (_tableWidth - totalWidth) * 0.5f;
        return new(width, posY);
    }

    public Vector2 GetSecondaryHitAreaSize(int numOfHolders, int status, float posY)
    {
        int count = status == 1 ? numOfHolders : numOfHolders - 1;
        float totalWidth = (_secondaryHolderWidth * count) + (_X_GAP_BETWEEN_HOLDERS * count);
        float width = (_tableWidth - totalWidth);
        return new(width, posY);
    }

    public Vector2 GetHitAreaPosition(RectTransform transform, float diff)
    {
        int direction = transform.CompareTag("RectLeft") ? -1 : 1;
        float moveValue = diff * 0.5f * direction;
        float posX = transform.anchoredPosition.x + moveValue;
        return new(posX, transform.anchoredPosition.y);
    }
}
