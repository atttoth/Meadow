using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class ScreenController : MonoBehaviour
{
    private GameEventController _eventController;
    private GameTask _waitingTask;
    private CardInspectionScreen _cardInspectionScreen;
    private MarkerActionScreen _markerActionScreen;
    private SelectionScreen _selectionScreen;
    private RowHighlightScreen _rowHighlightScreen;
    private ScoreCollectionScreen _scoreCollectionScreen;
    private GameRoundScreen _gameRoundScreen;
    private CardsInHandScreen _cardsInHandScreen;

    public void Create()
    {
        _eventController = new();
        _cardInspectionScreen = transform.GetChild(0).GetComponent<CardInspectionScreen>();
        Button approveIconRemoveButton = _cardInspectionScreen.Init();
        approveIconRemoveButton.onClick.AddListener(() =>
        {
            approveIconRemoveButton.enabled = false;
            _eventController.InvokeEventHandler(GameLogicEventType.REMOVED_CARD_ICON, new object[] { _cardInspectionScreen.GetDisposedIconItem() });
        });
        _markerActionScreen = transform.GetChild(1).GetComponent<MarkerActionScreen>();
        List<Button> actionIconButtons = _markerActionScreen.Init();
        actionIconButtons.ForEach(button => button.onClick.AddListener(() => _eventController.InvokeEventHandler(GameLogicEventType.MARKER_ACTION_SELECTED, new object[] { (MarkerAction)button.GetComponent<ScreenDisplayItem>().type })));

        _selectionScreen = transform.GetChild(2).GetComponent<SelectionScreen>();
        List<Button>[] itemButtons = _selectionScreen.Init();
        itemButtons[0].ForEach(button => button.onClick.AddListener(() =>
        {
            button.enabled = false;
            _selectionScreen.SelectedDeckType = (DeckType)button.GetComponent<ScreenDisplayItem>().type;
            CancelTaskWait();
        }));
        itemButtons[1].ForEach(button => button.onClick.AddListener(() =>
        {
            _selectionScreen.EnableCardItemButtons(false);
            _selectionScreen.SelectedCardID = button.GetComponent<ScreenDisplayItem>().ID;
            CancelTaskWait();
        }));

        _rowHighlightScreen = transform.GetChild(3).GetComponent<RowHighlightScreen>();
        Button highlightFrameButton = _rowHighlightScreen.Init();
        highlightFrameButton.onClick.AddListener(() =>
        {
            highlightFrameButton.enabled = false;
            ToggleRowHighlightFrame();
            CancelTaskWait();
        });

        _scoreCollectionScreen = transform.GetChild(4).GetComponent<ScoreCollectionScreen>();
        _scoreCollectionScreen.Init();

        _gameRoundScreen = transform.GetChild(5).GetComponent<GameRoundScreen>();
        _gameRoundScreen.Init();

        _cardsInHandScreen = transform.GetChild(6).GetComponent<CardsInHandScreen>();
        _cardsInHandScreen.Init();
    }

    public void SetupProgressDisplay(GameMode gameMode)
    {
        _gameRoundScreen.Setup(gameMode);
    }

    public void ToggleProgressScreen(bool value)
    {
        _gameRoundScreen.ToggleProgressUI(value);
    }

    public void ToggleMarkerActionScreen(Marker marker)
    {
        _markerActionScreen.ToggleScreen(marker);
    }

    public void SetSelectedCardID(int cardID)
    {
        _selectionScreen.SelectedCardID = cardID;
    }

    public int GetSelectedCardID()
    {
        return _selectionScreen.SelectedCardID;
    }

    private void CancelTaskWait()
    {
        _waitingTask.CancelWait();
        _waitingTask = null;
    }

    private void SelectionWaitHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _waitingTask = task;
                task.StartDelayMs(0, true);
                break;
            case 1:
                task.Wait();
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void RowSelectionHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                task.StartHandler((Action<GameTask>)SelectionWaitHandler);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void CardSelectionActionHandler(GameTask task, DeckType activeDeckType, List<Card>[] cardsByDeck, bool isPlayerSelection)
    {
        switch(task.State)
        {
            case 0:
                task.StartHandler((Action<GameTask, DeckType, bool>)_selectionScreen.ToggleDeckSelectionScreenHandler, activeDeckType, true);
                break;
            case 1:
                task.StartHandler((Action<GameTask>)SelectionWaitHandler);
                break;
            case 2:
                task.StartHandler((Action<GameTask, DeckType, bool>)_selectionScreen.ToggleDeckSelectionScreenHandler, _selectionScreen.SelectedDeckType, false);
                break;
            case 3:
                task.StartHandler((Action<GameTask, List<Card>[], bool>)CardSelectionHandler, cardsByDeck, isPlayerSelection);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void CardSelectionHandler(GameTask task, List<Card>[] cardsByDeck, bool isPlayerSelection)
    {
        switch (task.State)
        {
            case 0:
                List<Card> cards = cardsByDeck[(int)_selectionScreen.SelectedDeckType];
                task.StartHandler((Action<GameTask, List<Card>, bool>)_selectionScreen.ShowCardSelectionHandler, cards, isPlayerSelection);
                break;
            case 1:
                if(isPlayerSelection)
                {
                    task.StartHandler((Action<GameTask>)SelectionWaitHandler);
                }
                else
                {
                    task.StartDelayMs(0);
                }
                break;
            case 2:
                List<Card> unselectedCards = cardsByDeck[(int)_selectionScreen.SelectedDeckType].Where(card => card.Data.ID != _selectionScreen.SelectedCardID).ToList();
                task.StartHandler((Action<GameTask, List<Card>, bool>)_selectionScreen.HideCardSelectionHandler, unselectedCards, isPlayerSelection);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void CollectCardScoreHandler(GameTask task, List<Card> cards, Vector3 targetPosition)
    {
        switch (task.State)
        {
            case 0:
                SpriteAtlas atlas = GameResourceManager.Instance.Base;
                float cardScoreDelay = GameSettings.Instance.GetDuration(Duration.cardScoreDelay);
                float speed = GameSettings.Instance.GetDuration(Duration.cardScoreCollectingSpeed);
                int duration = (int)(((cards.Count - 1) * cardScoreDelay + speed) * 1000);
                int i = 0;
                while (cards.Count > 0)
                {
                    float delay = i * cardScoreDelay;
                    Card card = cards.First();
                    cards.RemoveAt(0);
                    Transform scoreTextPrefab = _scoreCollectionScreen.GetScoreTextObject();
                    scoreTextPrefab.GetComponent<Image>().sprite = atlas.GetSprite("score_" + card.Data.score.ToString());
                    card.CardIconItemsView.ToggleScoreItem(false);
                    scoreTextPrefab.SetPositionAndRotation(card.CardIconItemsView.GetScoreItemPosition(), Quaternion.identity);
                    DOTween.Sequence().Append(scoreTextPrefab.DOMove(targetPosition, speed).SetEase(Ease.InOutQuart).SetDelay(delay)).OnComplete(() =>
                    {
                        _eventController.InvokeEventHandler(GameLogicEventType.SCORE_COLLECTED, new object[] { card.Data.score });
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

    public void CollectCampScoreHandler(GameTask task, int score, Vector3 originPosition, Vector3 targetPosition)
    {
        switch (task.State)
        {
            case 0:
                float speed = GameSettings.Instance.GetDuration(Duration.cardScoreCollectingSpeed);
                int duration = (int)(speed * 1000);
                Transform scoreTextPrefab = _scoreCollectionScreen.GetScoreTextObject();
                scoreTextPrefab.SetPositionAndRotation(originPosition, Quaternion.identity);
                scoreTextPrefab.GetChild(1).GetComponent<TextMeshProUGUI>().text = score.ToString();
                DOTween.Sequence().Append(scoreTextPrefab.DOMove(targetPosition, speed).SetEase(Ease.InOutQuart)).OnComplete(() =>
                {
                    _eventController.InvokeEventHandler(GameLogicEventType.SCORE_COLLECTED, new object[] { score });
                    _scoreCollectionScreen.DisposeScoreTextObject(scoreTextPrefab);
                });
                task.StartDelayMs(duration);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void UpdateInspectedCardIconsDisposeStatus(int iconItemID)
    {
        _cardInspectionScreen.SelectIconItemOnCard(iconItemID);
    }

    public void CheckCardIconRemoveConditions(bool hasEnoughDisposableCards)
    {
        _cardInspectionScreen.UpdateIconRemoveButtonStatus(hasEnoughDisposableCards);
    }

    public void ToggleRowHighlightFrame(float posY = 0f)
    {
        _rowHighlightScreen.Toggle(posY);
    }

    public Delegate GetRemoveIconItemHandler()
    {
        return (Action<GameTask, CardIconItem>)_cardInspectionScreen.RemoveIconItemHandler;
    }

    public Delegate GetCardSelectionToggleHandler(bool isShow)
    {
        return isShow ? (Action<GameTask, List<Card>, bool>)_selectionScreen.ShowCardSelectionHandler : (Action<GameTask, List<Card>, bool>)_selectionScreen.HideCardSelectionHandler;
    }

    public Delegate GetCardInspectionScreenHandler(bool isShow)
    {
        return isShow ? (Action<GameTask, Card, bool>)_cardInspectionScreen.ShowCardHandler : (Action<GameTask>)_cardInspectionScreen.HideCardHandler;
    }

    public Delegate GetRoundScreenHandler(bool isGameFinished = false)
    {
        return isGameFinished ? (Action<GameTask, int>)_gameRoundScreen.ShowGameFinishedScreenHandler : (Action<GameTask, int>)_gameRoundScreen.ShowNextRoundScreenHandler;
    }

    public Delegate GetHandScreenToggleHandler(bool isToggled)
    {
        return isToggled ? (Action<GameTask, List<CardData>>)_cardsInHandScreen.ShowScreenHandler : (Action<GameTask, List<CardData>>)_cardsInHandScreen.HideScreenHandler;
    }
}
