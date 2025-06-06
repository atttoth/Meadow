
public enum GameSpeed
{ 
    SPEED1, SPEED2, SPEED3, SPEED4, SPEED5
}

public enum Duration
{
    waitDelay,
    turnStartWaitDuration,
    gameUIFadeDuration,
    campIconFadeDuration,
    campIconGroupPositionSpeed,
    campIconSinglePositionSpeed,
    campIconPositionDelay,
    cardDrawSpeedFromDeck,
    cardDrawDelayFromDeck,
    cardRotationSpeedOnBoard,
    cardDrawSpeedFromBoard,
    cardDrawSpeedDelayFromBoard,
    cardScoreDelay,
    cardScoreCollectingSpeed,
    cardsInHandScreenFadeSpeed,
    fakeCardFadeDelay,
    fakeCardFadeSpeed,
    fakeCardIconItemDelete,
    cardInspectionFlipDuration,
    displayIconHorizontalSlideSpeed,
    displayIconVerticalSlideSpeed,
    tableViewOpenSpeed,
    tableHolderCenteringSpeed,
    cardSnapSpeed,
    npcMarkerPlacementDuration
 }

public class GameSettings
{
    private static GameSettings _instance;
    private float _gameSpeedMod;

    public static GameSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new GameSettings();
            }
            return _instance;
        }
    }

    public void SetGameSpeed(GameSpeed speed)
    {
        _gameSpeedMod = 1f - ((int)speed * 0.1f);
    }

    public float GetDuration(Duration duration)
    {
        return GetValue(duration) * _gameSpeedMod;
    }

    private float GetValue(Duration duration)
    {
        switch(duration)
        {
            case Duration.waitDelay: return 1f;
            case Duration.turnStartWaitDuration: return 1f;
            case Duration.gameUIFadeDuration: return 0.5f;
            case Duration.campIconFadeDuration: return 0.8f;
            case Duration.campIconGroupPositionSpeed: return 0.6f;
            case Duration.campIconSinglePositionSpeed: return 0.6f;
            case Duration.campIconPositionDelay: return 0.5f;
            case Duration.cardDrawSpeedFromDeck: return 0.8f;
            case Duration.cardDrawDelayFromDeck: return 0.2f;
            case Duration.cardRotationSpeedOnBoard: return 0.8f;
            case Duration.cardDrawSpeedFromBoard: return 1f;
            case Duration.cardDrawSpeedDelayFromBoard: return 0.2f;
            case Duration.cardScoreDelay: return 0.5f;
            case Duration.cardScoreCollectingSpeed: return 0.8f;
            case Duration.cardsInHandScreenFadeSpeed: return 0.2f;
            case Duration.fakeCardFadeDelay: return 0.05f;
            case Duration.fakeCardFadeSpeed: return 0.1f;
            case Duration.fakeCardIconItemDelete: return 1f;
            case Duration.cardInspectionFlipDuration: return 1f;
            case Duration.displayIconHorizontalSlideSpeed: return 0.5f;
            case Duration.displayIconVerticalSlideSpeed: return 0.5f;
            case Duration.tableViewOpenSpeed: return 0.5f;
            case Duration.tableHolderCenteringSpeed: return 0.3f;
            case Duration.cardSnapSpeed: return 0.3f;
            case Duration.npcMarkerPlacementDuration: return 0.5f;
            default: return 0f;
        }
    }
}
