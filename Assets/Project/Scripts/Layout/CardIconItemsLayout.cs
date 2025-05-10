using UnityEngine;

public class CardIconItemsLayout
{
    private readonly static float GAP = 5f;

    public Vector2[] GetTopIconItemPositions(int numOfIcons, float iconDimension, bool isGroundIcon = false)
    {
        float startingPosX = 0f;
        Vector2[] positions = new Vector2[numOfIcons];
        float posY = isGroundIcon ? -150f : 0f;
        for (int i = 0; i < numOfIcons; i++)
        {
            float posX = startingPosX + ((iconDimension + GAP) * i);
            positions[i] = new(posX, posY);
        }
        return positions;
    }

    public Vector2[] GetRequiredIconItemPositions(int numOfIcons, float iconDimension)
    {
        int maxRow = 4; // 4 icons per col
        float startingPosX = numOfIcons > maxRow ? -iconDimension * 0.5f - GAP : 0f;
        float startingPosY = 0f;
        Vector2[] positions = new Vector2[numOfIcons];
        int rowIndex = 0;
        int colIndex = 0;
        for (int i = 0; i < numOfIcons; i++)
        {
            float posX = startingPosX + (colIndex * (iconDimension + GAP));
            float posY = startingPosY - ((iconDimension + (GAP * 0.5f)) * rowIndex);
            rowIndex++;
            if(rowIndex == maxRow)
            {
                rowIndex = 0;
                colIndex++;
            }
            positions[i] = new(posX, posY);
        }
        return positions;
    }

    public Vector2 GetScoreItemPosition()
    {
        float posX = -45f;
        float posY = -80f;
        return new(posX, posY);
    }
}
