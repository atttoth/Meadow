using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using static MarkerHolder;

public class GameLogicController : MonoBehaviour
{
    private BoardController _boardController;
    private CampController _campController;
    private ScreenController _screenController;
    private Delegate[] _logicEventHandlers;

    // resets at every session
    private GameMode _currentGameMode;
    private UserController[] _userControllers;
    private UserController _activeUserController;

    public void Init()
    {
        _boardController = ReferenceManager.Instance.boardController;
        _boardController.Create();
        _campController = ReferenceManager.Instance.campController;
        _campController.Create();
        _screenController = ReferenceManager.Instance.screenController;
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
            (Action<GameTask, MarkerHolder, Marker>)MarkerPlaceHandler,
            (Action<GameTask, HolderType, Marker>)MarkerCancelHandler,
            (Action<GameTask, MarkerAction>)MarkerActionSelectHandler,
            (Action<GameTask, DeckType>)DeckSelectHandler,
            (Action<GameTask, int>)ScoreCollectHandler,
            (Action<GameTask, bool>)HandScreenHandler
        };
    }

    public void OnLogicEvent(object eventType, object[] args)
    {
        new GameTask().ExecHandler(_logicEventHandlers[(int)eventType], args);
    }

    public void StartSession(GameMode gameMode, UserController[] userControllers)
    {
        _currentGameMode = gameMode;
        _userControllers = userControllers;
        new GameTask().ExecHandler((Action<GameTask>)StartGameSetupHandler);
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
            case 0:
                SetNextActiveUserController(true);
                (_activeUserController as PlayerController).EnableTableToggleButton(false);
                _userControllers.ToList().ForEach(controller => controller.ResetCampScoreTokens());
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
                task.StartHandler((Action<GameTask, DeckType, List<Card>>)_boardController.BoardFillHandler, GetActiveDeckType(), new List<Card>());
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
                task.StartDelayMs(1000);
                break;
            case 2:
                SetNextActiveUserController();
                if (_currentGameMode.ActiveUserIndex == 0)
                {
                    Debug.Log("startTurn: " + _activeUserController.userID);
                    _activeUserController.StartTurn();
                    task.StartDelayMs(0);
                }
                else
                {
                    Debug.Log("handSetup: " + _activeUserController.userID);
                    task.StartHandler((Action<GameTask>)NpcHandSetupHandler);
                }
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void NpcRandomTurnActionHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                (_activeUserController as NpcController).SelectRandomMarkerAndMarkerHolder(_boardController.GetAvailableMarkerHolders());
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
                Card card = _boardController.GetSingleSelectedCard();
                task.StartHandler((Action<GameTask, CardHolder, Card>)CardPickHandler, card.transform.parent.GetComponent<CardHolder>(), card);
                break;
            case 4:
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
                (_activeUserController as NpcController).SelectRandomMarkerAndMarkerHolder(_boardController.GetAvailableMarkerHolders(1));
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
                Card groundCard = (_activeUserController as NpcController).SelectInitialGroundCard(_boardController.CreateInitialGroundCards());
                groundCard.gameObject.SetActive(true);
                task.StartHandler((Action<GameTask, CardHolder, Card>)CardPickHandler, groundCard.transform.parent.GetComponent<CardHolder>(), groundCard);
                break;
            case 4:
                task.StartHandler((Action<GameTask>)EndHandSetupHandler);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void NpcEvaluatedTurnActionHandler(GameTask task)
    {
        task.Complete(); //todo
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
                    (_activeUserController as PlayerController).ToggleTurnEndButton(false);
                    (_activeUserController as PlayerController).EnableTableToggleButton(false);
                    _boardController.ToggleRayCastOfMarkerHolders(false);
                    _campController.ToggleRayCastOfMarkerHolders(false);
                    if (holder.holderType == HolderType.BoardMarker)
                    {
                        _boardController.SelectCard(marker, holder);
                    }
                    else if (holder.holderType == HolderType.CampMarker)
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
                    (_activeUserController as PlayerController).ToggleTurnEndButton(true);
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
                    (_activeUserController as PlayerController).ToggleTurnEndButton(true);
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
                task.StartHandler(_screenController.GetCardSelectionToggleHandler(true), _boardController.GetTopCardsOfDeck(deckType));
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
                task.StartHandler(_screenController.GetHandScreenToggleHandler(isToggled), (_activeUserController as PlayerController).UpdateHandScreenButton(isToggled));
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
                    task.StartHandler(_currentGameMode.ModeType == GameModeType.SINGLE_PLAYER_RANDOM 
                        ? (Action<GameTask>)NpcRandomTurnActionHandler 
                        : (Action<GameTask>)NpcEvaluatedTurnActionHandler
                        );
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
                    (_activeUserController as PlayerController).ToggleTurnEndButton(false);
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
                task.StartHandler(_screenController.GetRoundScreenHandler());
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
                    task.StartHandler((Action<GameTask>)_boardController.BoardClearHandler);
                }
                else
                {
                    task.NextState(4);
                }
                break;
            case 3:
                task.StartHandler((Action<GameTask, DeckType, List<Card>>)_boardController.BoardFillHandler, GetActiveDeckType(), new List<Card>());
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
                task.StartHandler(_screenController.GetRoundScreenHandler(true));
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
        if (((_activeUserController as PlayerController).draggingCardType == CardType.Ground && subType == HolderSubType.PRIMARY) || ((_activeUserController as PlayerController).draggingCardType == CardType.Landscape && subType == HolderSubType.SECONDARY))
        {
            (_activeUserController as PlayerController).UpdateActiveCardHolders(subType, hitAreaTag);
        }
        task.Complete();
    }

    private void RowPickHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                if(_activeUserController.userID == 0)
                {
                    _activeUserController.SetMarkerUsed();
                    _screenController.ToggleRowHighlightFrame();
                }
                List<Card> cards = _boardController.GetRowCards();
                _boardController.ToggleBlackOverlayOfCardHolders(false, new int[][] { });
                _boardController.ToggleCardsSelection(false);
                cards.ForEach(card => card.transform.parent.GetComponent<CardHolder>().RemoveItemFromContentList(card));
                task.StartHandler(_activeUserController.GetAddCardToHandHandler(), cards);
                break;
            case 1:
                task.StartHandler((Action<GameTask, DeckType, List<Card>>)_boardController.BoardFillHandler, GetActiveDeckType(), new List<Card>());
                break;
            case 2:
                task.StartDelayMs(500);
                break;
            case 3:
                if (_activeUserController.userID == 0)
                {
                    task.StartHandler(_screenController.GetCardSelectionToggleHandler(true), _boardController.CreateInitialGroundCards());
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
                    (_activeUserController as PlayerController).ToggleTurnEndButton(false);
                    (_activeUserController as PlayerController).ToggleHandScreenHitarea(false);
                    (_activeUserController as PlayerController).EnableTableToggleButton(false);
                }
                _boardController.ToggleBlackOverlayOfCardHolders(false, new int[][] { });
                if (holder == null) // card picked from selection screen
                {
                    task.StartHandler(_screenController.GetCardSelectionToggleHandler(false), _boardController.GetUnselectedCards(card.Data.ID));
                }
                else
                {
                    _activeUserController.SetMarkerUsed();
                    _boardController.ToggleCardsSelection(false);
                    holder.RemoveItemFromContentList(card);
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
                task.StartHandler((Action<GameTask, DeckType, List<Card>>)_boardController.BoardFillHandler, GetActiveDeckType(), new List<Card>());
                break;
            case 4:
                if (_currentGameMode.State == GameState.SETUP)
                {
                    task.StartHandler((Action<GameTask, Card>)_activeUserController.PlaceInitialGroundCardOnTable, card);
                }
                else
                {
                    if (_activeUserController.userID == 0)
                    {
                        (_activeUserController as PlayerController).ToggleTurnEndButton(true);
                        (_activeUserController as PlayerController).ToggleHandScreenHitarea(true);
                        (_activeUserController as PlayerController).EnableTableToggleButton(true);
                        _boardController.ToggleCanInspectFlagOfCards(true);
                        _boardController.ToggleRayCastOfCards(true);
                    }
                    task.StartDelayMs(0);
                }
                break;
            case 5:
                if(_activeUserController.userID == 0 && _currentGameMode.State == GameState.SETUP)
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
                    if (holder && playerController.TryPlaceCard(holder, card))
                    {
                        playerController.HandView.EnableCardsRaycast(false);
                        playerController.CreatePendingCardPlacement(holder, card);
                        playerController.EnableTableToggleButton(false);
                        playerController.TableView.UpdateApproveButton(true);
                        break;
                    }
                }
                playerController.draggingCardType = CardType.None;
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
                task.StartHandler((Action<GameTask, Card>)(_activeUserController as PlayerController).CancelPendingCardPlacement, card);
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
                task.StartHandler((Action<GameTask>)(_activeUserController as PlayerController).ApplyPendingCardPlacementHandler);
                break;
            default:
                (_activeUserController as PlayerController).ToggleTable();
                (_activeUserController as PlayerController).EnableTableToggleButton(true);
                (_activeUserController as PlayerController).EnableTableApproveButton(true);
                task.Complete();
                break;
        }
    }

    public void OnMarkerHolderInteraction(object sender, InteractableHolderEventArgs args)
    {
        PlayerController playerController = (_activeUserController as PlayerController);
        List<Marker> markers = _currentGameMode.State == GameState.SETUP ? new() { playerController.MarkerView.BlankMarker } : playerController.MarkerView.GetRemainingMarkers();
        if (args.scrollDirection == 1 || args.scrollDirection == -1)
        {
            playerController.ShowSelectedMarker(args.scrollDirection, markers);
        }
        else if (args.isHoverIn)
        {
            if(markers.Count > 0)
            {
                _boardController.ShowMarkersAtBoard(sender as MarkerHolder, markers);
                playerController.ShowSelectedMarker(0, markers);
            }
        }
        else
        {
            _boardController.HideMarkersAtBoard(markers);
        }
    }
}
