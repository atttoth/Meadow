
public class RoundScreenLayout
{
    private static readonly float AVATAR_WIDTH = 26f;
    private readonly float _sectionWidth;

    public RoundScreenLayout(float sectionWidth)
    {
        _sectionWidth = sectionWidth;
    }

    public float GetAvatarPositionX(int roundIndex, int index, int numOfRounds, int numOfUsers)
    {
        float totalWidth = numOfRounds * _sectionWidth;
        float startingPosXOfAvatars = totalWidth * -0.5f;
        float sectionOffset = roundIndex * _sectionWidth;
        float avatarPosX = AVATAR_WIDTH * index;
        float remainingSpace = _sectionWidth - (AVATAR_WIDTH * numOfUsers);
        float offset = numOfUsers == 2 ? -26 : numOfUsers == 3 ? -13 : 0; //todo: avoid hardcoded value?
        return startingPosXOfAvatars + sectionOffset + avatarPosX + remainingSpace + offset;
    }
}
