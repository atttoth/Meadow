using UnityEngine;

public class UserControllerLayout
{
    //   ______________
    //  |User-1  User-2|
    //  |              |
    //  |              |
    //  |User-3  User-0|
    //   --------------

    public UserControllerLayout()
    {

    }

    public Vector2[] GetNpcControllerScreenPositionByUserID(int userID)
    {
        switch(userID)
        {
            case 1: return new Vector2[] { new(-1320f, 900f), new(-155f, -75f) };
            case 2: return new Vector2[] { new(0f, 900f), new(110f, -75f) };
            default: return new Vector2[] { new(-1320f, -100f), new(-155f, 75f) };
        }
    }
}
