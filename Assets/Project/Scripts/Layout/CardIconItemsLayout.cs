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
        float startingPosY = 0f;
        Vector2[] positions = new Vector2[numOfIcons];
        float posX = 0f;
        for (int i = 0; i < numOfIcons; i++)
        {
            float posY = startingPosY - ((iconDimension + (GAP * 0.5f)) * i);
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
