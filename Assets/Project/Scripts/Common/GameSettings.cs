using UnityEngine;

public enum GameSpeed
{ 
    LEVEL1, LEVEL2, LEVEL3, LEVEL4, LEVEL5
}

public enum Duration
{
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
    overlayScreenFadeDuration,
    npcMarkerPlacementDuration
 }

public class GameSettings : MonoBehaviour
{
    private static GameSettings _instance;
    private float _gameSpeedMod;

    public static GameSettings Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = Object.Instantiate(Resources.Load<GameSettings>("GameSettings"));
            }
            return _instance;
        }
    }

    public void SetGameSpeed(GameSpeed level)
    {
        _gameSpeedMod = 1f - ((int)level * 0.1f);
    }

    public float GetDuration(Duration duration)
    {
        float value = GetValueBy(duration) * _gameSpeedMod;
        return value > 0f ? value : 0f;
    }

    private float GetValueBy(Duration duration)
    {
        switch(duration)
        {
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
            case Duration.overlayScreenFadeDuration: return 1f;
            case Duration.npcMarkerPlacementDuration: return 0.5f;
            default: return 0f;
        }
    }
}
