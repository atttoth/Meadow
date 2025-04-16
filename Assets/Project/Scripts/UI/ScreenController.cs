using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class ScreenController : GameLogicEvent
{
    private CardInspectionScreen _cardInspectionScreen;
    private MarkerActionScreen _markerActionScreen;
    private SelectionScreen _selectionScreen;
    private RowHighlightScreen _rowHighlightScreen;
    private ScoreCollectionScreen _scoreCollectionScreen;
    private GameRoundScreen _gameRoundScreen;
    private CardsInHandScreen _cardsInHandScreen;

    public void Create()
    {
        _cardInspectionScreen = transform.GetChild(0).GetComponent<CardInspectionScreen>();
        Button approveIconRemoveButton = _cardInspectionScreen.Init();
        approveIconRemoveButton.onClick.AddListener(() =>
        {
            approveIconRemoveButton.enabled = false;
            StartEventHandler(GameLogicEventType.REMOVED_CARD_ICON, new object[] { _cardInspectionScreen.GetDisposedIconItem() });
        });
        _markerActionScreen = transform.GetChild(1).GetComponent<MarkerActionScreen>();
        List<Button> actionIconButtons = _markerActionScreen.Init();
        actionIconButtons.ForEach(button => button.onClick.AddListener(() =>
        {
            button.enabled = true;
            StartEventHandler(GameLogicEventType.MARKER_ACTION_SELECTED, new object[] { (MarkerAction)button.GetComponent<ScreenDisplayItem>().type });
        }));

        _selectionScreen = transform.GetChild(2).GetComponent<SelectionScreen>();
        List<Button> deckButtons = _selectionScreen.Init();
        deckButtons.ForEach(button => button.onClick.AddListener(() =>
        {
            button.enabled = true;
            StartEventHandler(GameLogicEventType.DECK_SELECTED, new object[] { (DeckType)button.GetComponent<ScreenDisplayItem>().type });
        }));

        _rowHighlightScreen = transform.GetChild(3).GetComponent<RowHighlightScreen>();
        Button highlightFrameButton = _rowHighlightScreen.Init();
        highlightFrameButton.onClick.AddListener(() =>
        {
            highlightFrameButton.enabled = true;
            StartEventHandler(GameLogicEventType.ROW_PICKED, new object[0]);
        });

        _scoreCollectionScreen = transform.GetChild(4).GetComponent<ScoreCollectionScreen>();
        _scoreCollectionScreen.Init();

        _gameRoundScreen = transform.GetChild(5).GetComponent<GameRoundScreen>();
        _gameRoundScreen.Init();

        _cardsInHandScreen = transform.GetChild(6).GetComponent<CardsInHandScreen>();
        _cardsInHandScreen.Init();
    }

    public void ToggleMarkerActionScreen(Marker marker)
    {
        _markerActionScreen.ToggleScreen(marker);
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
                        StartEventHandler(GameLogicEventType.SCORE_COLLECTED, new object[] { card.Data.score });
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
                    StartEventHandler(GameLogicEventType.SCORE_COLLECTED, new object[] { score });
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

    public Delegate GetToggleDeckSelectionScreenHandler()
    {
        return (Action<GameTask, DeckType, bool>)_selectionScreen.ToggleDeckSelectionScreenHandler;
    }

    public Delegate GetCardSelectionToggleHandler(bool isShow)
    {
        return isShow ? (Action<GameTask, List<Card>>)_selectionScreen.ShowCardSelectionHandler : (Action<GameTask, List<Card>>)_selectionScreen.HideCardSelectionHandler;
    }

    public Delegate GetCardInspectionScreenHandler(bool isShow)
    {
        return isShow ? (Action<GameTask, Card, bool>)_cardInspectionScreen.ShowCardHandler : (Action<GameTask>)_cardInspectionScreen.HideCardHandler;
    }

    public Delegate GetRoundScreenHandler(bool isGameFinished = false)
    {
        return isGameFinished ? (Action<GameTask>)_gameRoundScreen.ShowGameFinishedScreenHandler : (Action<GameTask>)_gameRoundScreen.ShowNextRoundScreenHandler;
    }

    public Delegate GetHandScreenToggleHandler(bool isToggled)
    {
        return isToggled ? (Action<GameTask, List<CardData>>)_cardsInHandScreen.ShowScreenHandler : (Action<GameTask, List<CardData>>)_cardsInHandScreen.HideScreenHandler;
    }
}
