using UnityEngine;

public class IconDisplayLayout
{
    private readonly static float DISPLAY_ICON_WIDTH = 60f;

    public Vector2 GetNewIconPosition(float startingPosX, float posY, int modifier, int slideDirectionValue)
    {
        float posX = startingPosX - DISPLAY_ICON_WIDTH * modifier;
        posX -= DISPLAY_ICON_WIDTH * 0.5f * slideDirectionValue;
        return new(posX, posY);
    }

    public Vector2 GetExistingIconPosition(float startingPosX, float posY, int slideDirectionValue)
    {
        float posX = startingPosX - DISPLAY_ICON_WIDTH * 0.5f * slideDirectionValue;
        return new(posX, posY);
    }
}
