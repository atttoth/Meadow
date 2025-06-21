using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameLogicController
{
    private readonly BoardController _boardController;
    private readonly CampController _campController;
    private readonly ScreenController _screenController;
    private readonly Delegate[] _logicEventHandlers;

    // resets at every session
    private GameMode _currentGameMode;
    private UserController[] _userControllers;
    private UserController _activeUserController;

    public GameLogicController(BoardController boardController, CampController campController, ScreenController screenController)
    {
        _boardController = boardController;
        _boardController.Create();
        _campController = campController;
        _campController.Create();
        _screenController = screenController;
        _screenController.Create();
        _logicEventHandlers = new Delegate[] {
            (Action<GameTask>)TurnStartHandler,
            (Action<GameTask>)TurnEndHandler,
            (Action<GameTask>)RoundEndHandler,
            (Action<GameTask>)GameEndHandler,
            (Action<GameTask, bool>)TableToggleHandler,
            (Action<GameTask>)CampIconsSelectHandler,
            (Action<GameTask, bool>)CampToggleHandler,
            (Action<GameTask, int, Vector3>)CampScoreReceiveHandler,
            (Action<GameTask, HolderSubType, string>)TableHitAreaHoverOverHandler,
            (Action<GameTask>)RowPickHandler,
            (Action<GameTask, CardHolder, Card>)CardPickHandler,
            (Action<GameTask, CardType>)CardMoveHandler,
            (Action<GameTask, Card, List<RaycastResult>>)PendingCardPlaceHandler,
            (Action<GameTask, Card>)CardInspectionStartHandler,
            (Action<GameTask>)CardInspectionEndHandler,
            (Action<GameTask, int>)CardIconSelectedHandler,
            (Action<GameTask>)CardSelectedForDisposeHandler,
            (Action<GameTask, CardIconItem>)RemovedCardIconHandler,
            (Action<GameTask, List<Card>, Vector3>)ApprovedPendingCardPlaceHandler,
            (Action<GameTask, Card>)CancelledPendingCardPlaceHandler,
            (Action<GameTask, MarkerHolder, bool, int>)MarkerHolderInteractionHandler,
            (Action<GameTask, MarkerHolder, Marker>)MarkerPlaceHandler,
            (Action<GameTask, HolderType, Marker>)MarkerCancelHandler,
            (Action<GameTask, MarkerAction>)MarkerActionSelectHandler,
            (Action<GameTask, DeckType>)DeckSelectHandler,
            (Action<GameTask, int>)ScoreCollectHandler,
            (Action<GameTask, bool>)HandScreenHandler
        };
    }

    public void Execute(int handlerIndex, object[] args)
    {
        Delegate f = handlerIndex > -1 ? _logicEventHandlers[handlerIndex] : (Action<GameTask>)StartGameSetupHandler;
        new GameTask().ExecHandler(f, args);
    }

    public void SetupSession(GameMode gameMode, UserController[] userControllers)
    {
        _currentGameMode = gameMode;
        _userControllers = userControllers;
        _userControllers.ToList().ForEach(controller => controller.ResetCampScoreTokens());
        _screenController.SetupProgressDisplay(_currentGameMode);
        SetNextActiveUserController(true);
        (_activeUserController as PlayerController).EnableTableToggleButton(false);
    }

    private DeckType GetActiveDeckType()
    {
        return _currentGameMode.IsOverHalfTime() ? DeckType.North : DeckType.South;
    }

    private void SetNextActiveUserController(bool isGameStart = false)
    {
        if (!isGameStart)
        {
            _currentGameMode.SetNextActiveUserIndex();
            _activeUserController.IconDisplayView.ToggleActiveUserFrame(false);
        }
        _activeUserController = _userControllers[_currentGameMode.ActiveUserIndex];
        _activeUserController.IconDisplayView.ToggleActiveUserFrame(true);
    }

    private void StartGameSetupHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0: // todo: game intro
                task.StartDelayMs((int)(GameSettings.Instance.GetDuration(Duration.gameUIFadeDuration) * 1000));
                break;
            case 1:
                task.StartHandler((Action<GameTask>)_campController.StartViewSetupHandler);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void CampIconsSelectHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                task.StartHandler((Action<GameTask>)_campController.EndViewSetupHandler);
                break;
            case 1:
                _boardController.Fade(true);
                _campController.Fade(true);
                _userControllers.ToList().ForEach((controller) => controller.Fade(true));
                task.StartDelayMs((int)(GameSettings.Instance.GetDuration(Duration.gameUIFadeDuration) * 1000));
                break;
            case 2:
                task.StartHandler((Action<GameTask, DeckType>)_boardController.BoardFillHandler, GetActiveDeckType());
                break;
            case 3:
                _boardController.EnableRightSideMarkerHoldersForRowPick();
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void EndHandSetupHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                task.StartHandler((Action<GameTask>)_activeUserController.MarkerView.EndHandSetupHandler);
                break;
            case 1:
                task.StartDelayMs((int)(GameSettings.Instance.GetDuration(Duration.waitDelay) * 1000));
                break;
            case 2:
                SetNextActiveUserController();
                if (_currentGameMode.ActiveUserIndex == 0)
                {
                    task.StartHandler(_screenController.GetRoundScreenHandler(), _currentGameMode.CurrentRoundIndex + 1);
                }
                else
                {
                    task.NextState(4);
                }
                break;
            case 3:
                _activeUserController.StartTurn();
                task.NextState(5);
                break;
            case 4:
                task.StartHandler((Action<GameTask>)NpcHandSetupHandler);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void NpcTurnActionHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                (_activeUserController as NpcController).UpdateCampIconPairs(_campController.GetAdjacentIconPairs());
                (_activeUserController as NpcController).SelectAction(_boardController.GetAllCardsWithAvailableMarkerHolders());
                _boardController.ShowMarkersAtBoard((_activeUserController as NpcController).SelectedMarkerHolder, _activeUserController.MarkerView.GetRemainingMarkers());
                task.StartDelayMs(0);
                break;
            case 1:
                task.StartHandler((Action<GameTask>)(_activeUserController as NpcController).ShowMarkerPlacementHandler);
                break;
            case 2:
                task.StartHandler((Action<GameTask, MarkerHolder, Marker>)MarkerPlaceHandler, (_activeUserController as NpcController).SelectedMarkerHolder, (_activeUserController as NpcController).SelectedMarker);
                break;
            case 3:
                Card card = (_activeUserController as NpcController).SelectedCard;
                task.StartHandler((Action<GameTask, CardHolder, Card>)CardPickHandler, card.transform.parent.GetComponent<CardHolder>(), card);
                break;
            case 4:
                (_activeUserController as NpcController).SelectedCard = null;
                (_activeUserController as NpcController).SelectCardToPlace(_boardController.GetAllCardsWithAvailableMarkerHolders());
                task.StartHandler((Action<GameTask>)(_activeUserController as NpcController).PlaceCardOnTableHandler);
                break;
            case 5:
                task.StartHandler((Action<GameTask>)(_activeUserController as NpcController).UpdateDisplayIconsHandler);
                break;
            case 6:
                task.StartHandler((Action<GameTask>)(_activeUserController as NpcController).RegisterScoreHandler);
                break;
            default:
                _activeUserController.EndTurn();
                task.Complete();
                break;
        }
    }

    private void NpcHandSetupHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                (_activeUserController as NpcController).UpdateCampIconPairs(_campController.GetAdjacentIconPairs());
                (_activeUserController as NpcController).SelectRow(_boardController.GetAllCardsWithAvailableMarkerHolders());
                _boardController.ShowMarkersAtBoard((_activeUserController as NpcController).SelectedMarkerHolder, _activeUserController.MarkerView.GetRemainingMarkers());
                task.StartHandler((Action<GameTask>)(_activeUserController as NpcController).ShowMarkerPlacementHandler);
                break;
            case 1:
                task.StartHandler((Action<GameTask, MarkerHolder, Marker>)MarkerPlaceHandler, (_activeUserController as NpcController).SelectedMarkerHolder, (_activeUserController as NpcController).SelectedMarker);
                break;
            case 2:
                task.StartHandler((Action<GameTask>)RowPickHandler);
                break;
            case 3:
                List<Card> groundCards = _boardController.CreateInitialGroundCards();
                (_activeUserController as NpcController).SelectInitialGroundCard(_boardController.GetAllCardsWithAvailableMarkerHolders(), groundCards);
                task.StartHandler(_screenController.GetCardSelectionToggleHandler(true), groundCards, false);
                break;
            case 4:
                Card groundCard = (_activeUserController as NpcController).SelectedCard;
                task.StartHandler((Action<GameTask, CardHolder, Card>)CardPickHandler, groundCard.transform.parent.GetComponent<CardHolder>(), groundCard);
                break;
            case 5:
                task.StartHandler((Action<GameTask>)(_activeUserController as NpcController).PlaceCardOnTableHandler);
                break;
            case 6:
                task.StartHandler((Action<GameTask>)(_activeUserController as NpcController).UpdateDisplayIconsHandler);
                break;
            case 7:
                task.StartHandler((Action<GameTask>)EndHandSetupHandler);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void MarkerPlaceHandler(GameTask task, MarkerHolder holder, Marker marker)
    {
        switch (task.State)
        {
            case 0:
                marker.Status = MarkerStatus.PLACED;
                if (_activeUserController.userID == 0)
                {
                    marker.SetAlpha(true);
                    (_activeUserController as PlayerController).EnableTurnEndButton(false);
                    (_activeUserController as PlayerController).EnableTableToggleButton(false);
                    _boardController.ToggleRayCastOfMarkerHolders(false);
                    _campController.ToggleRayCastOfMarkerHolders(false);
                    if (holder.Data.holderType == HolderType.BoardMarker)
                    {
                        _boardController.SelectCard(marker, holder);
                    }
                    else if (holder.Data.holderType == HolderType.CampMarker)
                    {
                        _campController.ToggleCampAction(true);
                        _screenController.ToggleMarkerActionScreen(marker);
                    }

                    if (_currentGameMode.State == GameState.SETUP)
                    {
                        _screenController.ToggleRowHighlightFrame(_boardController.GetSingleSelectedCard().transform.position.y);
                    }
                }
                else
                {
                    _boardController.SelectCard(marker, holder);
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void MarkerCancelHandler(GameTask task, HolderType type, Marker marker)
    {
        switch (task.State)
        {
            case 0:
                marker.Status = MarkerStatus.NONE;
                marker.SetAlpha(false);
                _boardController.ToggleBlackOverlayOfCardHolders(false, new int[][] { });
                if (type == HolderType.BoardMarker)
                {
                    _boardController.ToggleCardsSelection(false);
                }
                else if (type == HolderType.CampMarker)
                {
                    _campController.ToggleCampAction(false);
                    _screenController.ToggleMarkerActionScreen(null);
                }
                if(_currentGameMode.State == GameState.SETUP)
                {
                    _screenController.ToggleRowHighlightFrame();
                    _boardController.EnableRightSideMarkerHoldersForRowPick();
                }
                else
                {
                    _boardController.ToggleRayCastOfMarkerHolders(true);
                    _campController.ToggleRayCastOfMarkerHolders(true);
                    (_activeUserController as PlayerController).EnableTableToggleButton(true);
                    (_activeUserController as PlayerController).EnableTurnEndButton(true);
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void MarkerActionSelectHandler(GameTask task, MarkerAction markerAction)
    {
        switch (task.State)
        {
            case 0:
                _screenController.ToggleMarkerActionScreen(null);
                _activeUserController.SetMarkerUsed();
                task.StartDelayMs(0);
                break;
            case 1:
                switch (markerAction)
                {
                    case MarkerAction.PICK_ANY_CARD_FROM_BOARD:
                        task.StartHandler((Action<GameTask>)_boardController.EnableAnyCardSelectionHandler);
                        break;
                    case MarkerAction.TAKE_2_ROAD_TOKENS:
                        task.StartHandler((Action<GameTask>)_activeUserController.AddRoadTokensHandler);
                        break;
                    case MarkerAction.PICK_A_CARD_FROM_CHOSEN_DECK:
                        task.StartHandler(_screenController.GetToggleDeckSelectionScreenHandler(), GetActiveDeckType(), true);
                        break;
                    default:
                        task.StartHandler((Action<GameTask>)_activeUserController.AddExtraCardPlacementHandler);
                        break;
                }
                break;
            case 2:
                if (
                    _activeUserController.userID == 0 &&
                    Array.Exists(new[] { MarkerAction.TAKE_2_ROAD_TOKENS, MarkerAction.PLAY_UP_TO_2_CARDS }, action => action == markerAction)) // marker action ends immediately
                {
                    (_activeUserController as PlayerController).EnableTableToggleButton(true);
                    (_activeUserController as PlayerController).EnableTurnEndButton(true);
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void DeckSelectHandler(GameTask task, DeckType deckType)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler(_screenController.GetToggleDeckSelectionScreenHandler(), deckType, false);
                break;
            case 1:
                task.StartHandler(_screenController.GetCardSelectionToggleHandler(true), _boardController.GetRandomCardOfDeck(deckType, 3), _activeUserController.userID == 0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void ScoreCollectHandler(GameTask task, int score)
    {
        switch(task.State)
        {
            case 0:
                _activeUserController.UpdateScore(score);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void HandScreenHandler(GameTask task, bool isToggled)
    {
        switch(task.State)
        {
            case 0:
                task.StartHandler(_screenController.GetHandScreenToggleHandler(isToggled), (_userControllers[0] as PlayerController).UpdateHandScreenButton(isToggled));
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void TurnStartHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0: // reset animation?
                if (_activeUserController.userID == 0 & _currentGameMode.State == GameState.SETUP)
                {
                    _currentGameMode.State = GameState.GAMEPLAY;
                    (_activeUserController as PlayerController).FadeTurnEndButton(true);
                    (_activeUserController as PlayerController).EnableCampButton(true);
                    (_activeUserController as PlayerController).HandView.EnableCardsRaycast(true);
                    (_activeUserController as PlayerController).TableView.EnableTableScroll(true);
                }
                _activeUserController.InfoView.SetMaxCardPlacement(1);
                _activeUserController.InfoView.SetCardPlacement(0);
                _activeUserController.MarkerView.IsMarkerConsumed = false;
                task.StartDelayMs((int)(GameSettings.Instance.GetDuration(Duration.turnStartWaitDuration) * 1000));
                break;
            case 1:
                if(_activeUserController.userID == 0)
                {
                    (_activeUserController as PlayerController).EnableTableToggleButton(true);
                    _boardController.ToggleRayCastOfMarkerHolders(true);
                    _boardController.ToggleCanInspectFlagOfCards(true);
                    _boardController.ToggleRayCastOfCards(true);
                    _campController.ToggleRayCastOfMarkerHolders(true);
                    task.StartDelayMs(0);
                }
                else
                {
                    task.StartHandler((Action<GameTask>)NpcTurnActionHandler);
                }
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void TurnEndHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                if(_activeUserController.userID == 0)
                {
                    (_activeUserController as PlayerController).EnableTurnEndButton(false);
                    (_activeUserController as PlayerController).EnableTableToggleButton(false);
                    _boardController.ToggleRayCastOfMarkerHolders(false);
                    _boardController.ToggleRayCastOfCards(false);
                    _boardController.ToggleCanInspectFlagOfCards(false);
                    _campController.ToggleRayCastOfMarkerHolders(false);
                }
                int nextUserID = _currentGameMode.PeekNextUserID();
                if (_userControllers.ToList().Find(controller => controller.userID == nextUserID).MarkerView.GetRemainingMarkers().Count > 0)
                {
                    SetNextActiveUserController();
                    _activeUserController.StartTurn();
                }
                else if(_currentGameMode.IsGameEnded)
                {
                    _activeUserController.EndGame();
                }
                else
                {
                    _activeUserController.EndRound();
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void RoundEndHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                task.StartHandler(_screenController.GetRoundScreenHandler(), _currentGameMode.CurrentRoundIndex + 2);
                break;
            case 1: // marker reset animation?
                _userControllers.ToList().ForEach(controller => controller.MarkerView.Reset());
                task.StartDelayMs(1000);
                break;
            case 2:
                int prevRoundIndex = _currentGameMode.CurrentRoundIndex;
                _currentGameMode.SetNextRound();
                if(!_currentGameMode.IsOverHalfTime(prevRoundIndex) && _currentGameMode.IsOverHalfTime()) // check if session just reached half-time
                {
                    task.StartHandler((Action<GameTask>)_boardController.BoardChangeHandler);
                }
                else
                {
                    task.StartDelayMs(0);
                }
                break;
            default:
                Debug.Log("round: " + (_currentGameMode.CurrentRoundIndex + 1));
                SetNextActiveUserController();
                _activeUserController.StartTurn();
                task.Complete();
                break;
        }
    }

    private void GameEndHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                task.StartHandler(_screenController.GetRoundScreenHandler(true), _currentGameMode.CurrentRoundIndex + 2);
                break;
            default:
                Debug.Log("finished");
                task.Complete();
                break;
        }
    }

    private void TableToggleHandler(GameTask task, bool value)
    {
        switch(task.State)
        {
            case 0:
                _boardController.ToggleRayCastOfCards(value && _currentGameMode.State != GameState.SETUP);
                _boardController.Fade(value);
                _campController.Fade(value);
                _userControllers.ToList().ForEach(controller =>
                {
                    if(controller.userID > 0)
                    {
                        controller.Fade(value);
                    }
                });
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void CampToggleHandler(GameTask task, bool value)
    {
        switch(task.State)
        {
            case 0:
                if(value)
                {
                    PlayerController controller = _userControllers[0] as PlayerController;
                    _campController.SaveCampScoreToken(controller.GetNextCampScoreToken());
                    _campController.EnableScoreButtonOfFulfilledIcons(controller.GetAdjacentPrimaryIconPairs());
                }
                _campController.ToggleCampView(value);
                _screenController.ToggleProgressScreen(value);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void CampScoreReceiveHandler(GameTask task, int score, Vector3 originPosition)
    {
        switch(task.State)
        {
            case 0:
                _campController.ToggleCampAction(false);
                _activeUserController.UpdateCampScoreTokens();
                task.StartHandler((Action<GameTask, int, Vector3, Vector3>)_screenController.CollectCampScoreHandler, score, originPosition, (_activeUserController as PlayerController).InfoView.scoreTransform.position);
                break;
            default:
                (_activeUserController as PlayerController).ToggleCamp();
                task.Complete();
                break;
        }
    }

    private void TableHitAreaHoverOverHandler(GameTask task, HolderSubType subType, string hitAreaTag)
    {
        (_activeUserController as PlayerController).OnTableHitAreaHover(subType, hitAreaTag);
        task.Complete();
    }

    private void RowPickHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                if (_activeUserController.userID == 0)
                {
                    _activeUserController.SetMarkerUsed();
                    (_activeUserController as PlayerController).HandView.EnableCardsRaycast(false);
                    _screenController.ToggleRowHighlightFrame();
                }
                _boardController.ToggleBlackOverlayOfCardHolders(false, new int[][] { });
                task.StartHandler((Action<GameTask>)_boardController.DrawRandomNorthCardFromDeckHandler);
                break;
            case 1:
                List<Card> cardsOfRow = _boardController.GetRowOfSelectedCards();
                _boardController.ToggleCardsSelection(false);
                cardsOfRow.ForEach(card => card.transform.parent.GetComponent<CardHolder>().Data.RemoveItemFromContentList(card));
                cardsOfRow.Insert(0, _boardController.GetUnselectedCards(null).First()); // include random north card
                task.StartHandler(_activeUserController.GetAddCardToHandHandler(), cardsOfRow);
                break;
            case 2:
                task.StartHandler((Action<GameTask, DeckType>)_boardController.BoardFillHandler, GetActiveDeckType());
                break;
            case 3:
                task.StartDelayMs(500);
                break;
            case 4:
                if (_activeUserController.userID == 0)
                {
                    task.StartHandler(_screenController.GetCardSelectionToggleHandler(true), _boardController.CreateInitialGroundCards(), true);
                }
                else
                {
                    task.StartDelayMs(0);
                }
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void CardPickHandler(GameTask task, CardHolder holder, Card card)
    {
        switch (task.State)
        {
            case 0:
                if (_activeUserController.userID == 0)
                {
                    _boardController.ToggleRayCastOfCards(false);
                    (_activeUserController as PlayerController).EnableTurnEndButton(false);
                    (_activeUserController as PlayerController).ToggleHandScreenHitarea(false);
                    (_activeUserController as PlayerController).EnableTableToggleButton(false);
                    (_activeUserController as PlayerController).HandView.EnableCardsRaycast(false);
                }
                _boardController.ToggleBlackOverlayOfCardHolders(false, new int[][] { });
                if (holder == null) // card picked from selection screen
                {
                    task.StartHandler(_screenController.GetCardSelectionToggleHandler(false), _boardController.GetUnselectedCards(card), _activeUserController.userID == 0);
                }
                else
                {
                    _activeUserController.SetMarkerUsed();
                    _boardController.ToggleCardsSelection(false);
                    holder.Data.RemoveItemFromContentList(card);
                    task.StartDelayMs(0);
                }
                break;
            case 1:
                if (holder == null) // card picked from selection screen
                {
                    _boardController.DisposeUnselectedCards(_currentGameMode.State == GameState.SETUP);
                }
                task.StartDelayMs(0);
                break;
            case 2:
                task.StartHandler(_activeUserController.GetAddCardToHandHandler(), new List<Card>() { card });
                break;
            case 3:
                task.StartHandler((Action<GameTask, DeckType>)_boardController.BoardFillHandler, GetActiveDeckType());
                break;
            case 4:
                if(_activeUserController.userID == 0)
                {
                    if (_currentGameMode.State == GameState.SETUP)
                    {
                        task.StartHandler((Action<GameTask, Card>)(_activeUserController as PlayerController).PlaceInitialCardOnTableHandler, card);
                    }
                    else
                    {
                        (_activeUserController as PlayerController).EnableTurnEndButton(true);
                        (_activeUserController as PlayerController).ToggleHandScreenHitarea(true);
                        (_activeUserController as PlayerController).EnableTableToggleButton(true);
                        (_activeUserController as PlayerController).HandView.EnableCardsRaycast(true);
                        _boardController.ToggleCanInspectFlagOfCards(true);
                        _boardController.ToggleRayCastOfCards(true);
                        task.StartDelayMs(0);
                    }
                }
                else
                {
                    task.NextState(6);
                }
                break;
            case 5:
                if(_currentGameMode.State == GameState.SETUP)
                {
                    task.StartHandler((Action<GameTask>)EndHandSetupHandler);
                }
                else
                {
                    task.StartDelayMs(0);
                }
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void CardMoveHandler(GameTask task, CardType cardType)
    {
        (_activeUserController as PlayerController).ToggleHitArea(cardType);
        task.Complete();
    }

    private void CardInspectionStartHandler(GameTask task, Card card)
    {
        switch (task.State)
        {
            case 0:
                if ((_activeUserController as PlayerController).TableView.isTableVisible)
                {
                    (_activeUserController as PlayerController).UpdateHandCardsStatus(true);
                }
                else
                {
                    (_activeUserController as PlayerController).EnableTableToggleButton(false);
                    _boardController.ToggleRayCastOfCards(false);
                    _boardController.ToggleRayCastOfMarkerHolders(false);
                    _campController.ToggleRayCastOfMarkerHolders(false);
                }
                task.StartHandler(_screenController.GetCardInspectionScreenHandler(true), card, (_activeUserController as PlayerController).TableView.isTableVisible);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void CardInspectionEndHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler(_screenController.GetCardInspectionScreenHandler(false));
                break;
            default:
                if ((_activeUserController as PlayerController).TableView.isTableVisible)
                {
                    (_activeUserController as PlayerController).UpdateHandCardsStatus(false);
                }
                else
                {
                    (_activeUserController as PlayerController).EnableTableToggleButton(true);
                    _boardController.ToggleRayCastOfCards(true);
                    if(!_activeUserController.MarkerView.IsMarkerConsumed)
                    {
                        _boardController.ToggleRayCastOfMarkerHolders(true);
                        _campController.ToggleRayCastOfMarkerHolders(true);
                    }
                }
                task.Complete();
                break;
        }
    }

    private void CardIconSelectedHandler(GameTask task, int iconItemID)
    {
        _screenController.UpdateInspectedCardIconsDisposeStatus(iconItemID);
        _screenController.CheckCardIconRemoveConditions((_activeUserController as PlayerController).HandView.HasDisposableCardsSelected());
        task.Complete();
    }

    private void CardSelectedForDisposeHandler(GameTask task)
    {
        _screenController.CheckCardIconRemoveConditions((_activeUserController as PlayerController).HandView.HasDisposableCardsSelected());
        task.Complete();
    }

    private void RemovedCardIconHandler(GameTask task, CardIconItem item)
    {
        switch(task.State)
        {
            case 0:
                (_activeUserController as PlayerController).DisposeHandCards();
                task.StartHandler(_screenController.GetRemoveIconItemHandler(), item);
                break;
            case 1:
                (_activeUserController as PlayerController).RemoveCardIconItem(item);
                task.StartHandler((Action<GameTask>)(_activeUserController as PlayerController).CenterCardsInHandHandler);
                break;
            case 2:
                task.StartHandler((Action<GameTask>)CardInspectionEndHandler);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void PendingCardPlaceHandler(GameTask task, Card card, List<RaycastResult> raycastResults)
    {
        PlayerController playerController = _activeUserController as PlayerController;
        switch (task.State)
        {
            case 0:
                CardHolder holder = null;
                foreach (RaycastResult result in raycastResults)
                {
                    holder = result.gameObject.GetComponent<CardHolder>();
                    TableCardHitArea hitArea = result.gameObject.GetComponent<TableCardHitArea>();
                    holder = hitArea ? playerController.GetTableCardHolderOfHitArea(hitArea) : holder;
                    if (playerController.TryPlaceCardOnTable(holder, card))
                    {
                        playerController.HandView.EnableCardsRaycast(false);
                        playerController.EnableTableToggleButton(false);
                        playerController.TableView.UpdateApproveButton(true);
                        break;
                    }
                }
                playerController.TableView.TogglePrimaryHitAreas(false);
                playerController.TableView.ToggleSecondaryHitArea(false);

                if (holder == null)
                {
                    if (card.Data.cardType == CardType.Landscape) // unfulfilled icon/road token requirements
                    {
                        playerController.TableView.RemoveEmptyHolder(HolderSubType.SECONDARY);
                    }
                    card.MoveCardBackToHand(playerController.HandView.transform);
                    task.StartDelayMs(0);
                }
                else
                {
                    task.StartHandler((Action<GameTask, CardHolder, Card>)playerController.SnapCardHandler, holder, card);
                }
                break;
            default:
                card.ToggleRayCast(true);
                (_activeUserController as PlayerController).HandView.EnableCardsRaycast(true);
                task.Complete();
                break;
        }
    }

    private void CancelledPendingCardPlaceHandler(GameTask task, Card card)
    {
        switch(task.State)
        {
            case 0:
                card.ToggleRayCast(false);
                (_activeUserController as PlayerController).HandView.EnableCardsRaycast(false);
                task.StartHandler((Action<GameTask, Card>)(_activeUserController as PlayerController).CancelCardPlacement, card);
                break;
            default:
                card.ToggleRayCast(true);
                (_activeUserController as PlayerController).HandView.EnableCardsRaycast(true);
                task.Complete();
                break;
        }
    }

    private void ApprovedPendingCardPlaceHandler(GameTask task, List<Card> cards, Vector3 targetPosition)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler((Action<GameTask, List <Card>, Vector3>)_screenController.CollectCardScoreHandler, cards, targetPosition);
                break;
            case 1:
                task.StartHandler((Action<GameTask>)(_activeUserController as PlayerController).ApplyCardPlacementHandler);
                break;
            default:
                (_activeUserController as PlayerController).ToggleTable();
                (_activeUserController as PlayerController).EnableTableToggleButton(true);
                (_activeUserController as PlayerController).EnableTableApproveButton(true);
                task.Complete();
                break;
        }
    }

    private void MarkerHolderInteractionHandler(GameTask task, MarkerHolder holder, bool isHoverIn, int scrollDirection)
    {
        PlayerController playerController = (_activeUserController as PlayerController);
        List<Marker> markers = _currentGameMode.State == GameState.SETUP ? new() { playerController.MarkerView.BlankMarker } : playerController.MarkerView.GetRemainingMarkers();
        if (scrollDirection == 1 || scrollDirection == -1)
        {
            playerController.ShowSelectedMarker(scrollDirection, markers);
        }
        else if (isHoverIn)
        {
            if (markers.Count > 0)
            {
                _boardController.ShowMarkersAtBoard(holder, markers);
                playerController.ShowSelectedMarker(0, markers);
            }
        }
        else
        {
            _boardController.HideMarkersAtBoard(markers);
        }
        task.Complete();
    }
}
