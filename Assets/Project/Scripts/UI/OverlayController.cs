using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static GameTask;

public class OverlayController : GameLogicEvent
{
    private CardInspectionScreen _cardInspectionScreen;
    private MarkerActionScreen _markerActionScreen;
    private DeckSelectionScreen _deckSelectionScreen;
    private ScoreCollectionScreen _scoreCollectionScreen;
    private CardsInHandScreen _cardsInHandScreen;

    public void CreateOverlay()
    {
        _cardInspectionScreen = transform.GetChild(0).GetComponent<CardInspectionScreen>();
        Button screenButton = _cardInspectionScreen.Init();
        screenButton.onClick.AddListener(() => StartEventHandler(GameLogicEventType.CARD_INSPECTION_ENDED, null));

        _markerActionScreen = transform.GetChild(1).GetComponent<MarkerActionScreen>();
        List<Button> actionIconButtons = _markerActionScreen.Init();
        actionIconButtons.ForEach(button => button.onClick.AddListener(() =>
        {
            button.enabled = true;
            StartEventHandler(GameLogicEventType.MARKER_ACTION_SELECTED, new GameTaskItemData() { markerAction = (MarkerAction)button.GetComponent<ScreenDisplayItem>().type });
        }));

        _deckSelectionScreen = transform.GetChild(2).GetComponent<DeckSelectionScreen>();
        List<Button> deckButtons = _deckSelectionScreen.Init();
        deckButtons.ForEach(button => button.onClick.AddListener(() =>
        {
            button.enabled = true;
            StartEventHandler(GameLogicEventType.DECK_SELECTED, new GameTaskItemData() { deckType = (DeckType)button.GetComponent<ScreenDisplayItem>().type });
        }));

        _scoreCollectionScreen = transform.GetChild(3).GetComponent<ScoreCollectionScreen>();
        _scoreCollectionScreen.Init();

        _cardsInHandScreen = transform.GetChild(4).GetComponent<CardsInHandScreen>();
        _cardsInHandScreen.Init();
    }

    public void ToggleMarkerActionScreen(Marker marker)
    {
        _markerActionScreen.ToggleScreen(marker);
    }

    public void CollectCardScoreHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                List<Card> cards = task.Data.cards;
                float cardScoreDelay = ReferenceManager.Instance.gameLogicManager.GameSettings.cardScoreDelay;
                float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardScoreCollectingSpeed;
                int duration = (int)(((cards.Count - 1) * cardScoreDelay + speed) * 1000);
                int i = 0;
                while (cards.Count > 0)
                {
                    float delay = i * cardScoreDelay;
                    Card card = cards.First();
                    cards.RemoveAt(0);
                    Transform scoreTextPrefab = _scoreCollectionScreen.GetScoreTextObject();
                    scoreTextPrefab.SetPositionAndRotation(card.GetScorePosition(), Quaternion.identity);
                    scoreTextPrefab.GetChild(1).GetComponent<TextMeshProUGUI>().text = card.Data.score.ToString();
                    DOTween.Sequence().Append(scoreTextPrefab.DOMove(task.Data.targetTransform.position, speed).SetEase(Ease.InOutQuart).SetDelay(delay)).OnComplete(() =>
                    {
                        StartEventHandler(GameLogicEventType.SCORE_COLLECTED, new GameTaskItemData() { score = card.Data.score });
                        _scoreCollectionScreen.DisposeScoreTextObject(scoreTextPrefab);
                    });
                    i++;
                }
                task.StartDelayMs(duration);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void CollectCampScoreHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardScoreCollectingSpeed;
                int duration = (int)(speed * 1000);
                Transform scoreTextPrefab = _scoreCollectionScreen.GetScoreTextObject();
                scoreTextPrefab.SetPositionAndRotation(task.Data.originTransform.position, Quaternion.identity);
                scoreTextPrefab.GetChild(1).GetComponent<TextMeshProUGUI>().text = task.Data.score.ToString();
                DOTween.Sequence().Append(scoreTextPrefab.DOMove(task.Data.targetTransform.position, speed).SetEase(Ease.InOutQuart)).OnComplete(() =>
                {
                    StartEventHandler(GameLogicEventType.SCORE_COLLECTED, task.Data);
                    _scoreCollectionScreen.DisposeScoreTextObject(scoreTextPrefab);
                });
                task.StartDelayMs(duration);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public GameTaskHandler GetToggleDeckSelectionScreenHandler()
    {
        return _deckSelectionScreen.ToggleDeckSelectionScreenHandler;
    }

    public GameTaskHandler GetCardSelectionToggleHandler(bool isShow)
    {
        return isShow ? _deckSelectionScreen.ShowCardSelectionHandler : _deckSelectionScreen.HideCardSelectionHandler;
    }

    public GameTaskHandler GetCardInspectionScreenHandler(bool isShow)
    {
        return isShow ? _cardInspectionScreen.ShowCardHandler : _cardInspectionScreen.HideCardHandler;
    }

    public GameTaskHandler GetHandScreenToggleHandler(bool isToggled)
    {
        return isToggled ? _cardsInHandScreen.ShowScreenHandler : _cardsInHandScreen.HideScreenHandler;
    }
}
