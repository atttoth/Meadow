using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OverlayController : GameLogicEvent
{
    private CardInspectionScreen _cardInspectionScreen;
    private MarkerActionScreen _markerActionScreen;
    private DeckSelectionScreen _deckSelectionScreen;
    private ScoreCollectionScreen _scoreCollectionScreen;

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
    }

    public void ToggleMarkerActionScreen(Marker marker)
    {
        _markerActionScreen.ToggleScreen(marker);
    }

    public void ShowCardInspectionScreenHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                int duration = (int)(ReferenceManager.Instance.gameLogicManager.GameSettings.cardInspectionFlipDuration * 1000);
                _cardInspectionScreen.ShowCard(task.Data.card);
                task.StartDelayMs(duration);
                break;
            default:
                _cardInspectionScreen.ToggleScreenButton(true);
                task.Complete();
                break;
        }
    }

    public void HideCardInspectionScreenHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _cardInspectionScreen.HideCard();
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void ShowDeckSelectionScreenHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _deckSelectionScreen.ToggleDeckSelectionScreen(true, task.Data.deckType);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void HideDeckSelectionScreenHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _deckSelectionScreen.ToggleDeckSelectionScreen(false, task.Data.deckType);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void ShowCardSelectionHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler(_deckSelectionScreen.ShowCardSelectionHandler, task.Data);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void HideCardSelectionHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler(_deckSelectionScreen.HideCardSelectionHandler, task.Data);
                break;
            default:
                task.Complete();
                break;
        }
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
                    scoreTextPrefab.SetPositionAndRotation(card.GetComponent<Transform>().position, Quaternion.identity);
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
}
