using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using static GameTask;
using static MarkerHolder;

public class GameLogicManager : MonoBehaviour
{
    private GameSettings _gameSettings;
    private BoardController _boardController;
    private CampController _campController;
    private PlayerController _playerController;
    private OverlayController _overlayController;
    private GameTaskHandler[] _logicEventHandlers;
    public bool hasRemainingMarkers;

    private void Awake()
    {
        InitGame();
    }

    private void InitGame()
    {
        _gameSettings = new GameSettings();
        _boardController = ReferenceManager.Instance.boardController;
        _campController = ReferenceManager.Instance.campController;
        _playerController = ReferenceManager.Instance.playerController;
        _overlayController = ReferenceManager.Instance.overlayController;
        _logicEventHandlers = new GameTaskHandler[] {
            TableToggleHandler,
            CampIconsSelectHandler,
            CampToggleHandler,
            CampScoreReceiveHandler,
            TableHitAreaHoverOverHandler,
            CardPickHandler,
            CardMoveHandler,
            PendingCardPlaceHandler,
            CardInspectionStartHandler,
            CardInspectionEndHandler,
            ApprovedPendingCardPlaceHandler,
            CancelledPendingCardPlaceHandler,
            MarkerPlaceHandler,
            MarkerCancelHandler,
            MarkerActionSelectHandler,
            DeckSelectHandler,
            ScoreCollectHandler,
            HandScreenHandler
        };

        _boardController.CreateBoard();
        _campController.CreateCamp();
        _playerController.CreatePlayer();
        _overlayController.CreateOverlay();
    }

    public GameSettings GameSettings { get { return _gameSettings; } }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E)) // for testing
        {
            _playerController.ResetCampScoreTokens();
            _playerController.EnableTableView(false);
            _boardController.ToggleRayCastOfMarkerHolders(false);
            _campController.ToggleRayCastOfMarkerHolders(false);
            //new GameTask().ExecHandler(_campController.ShowViewSetupHandler);
            new GameTask().ExecHandler(TestHandler);
        }

        if (!hasRemainingMarkers && Input.GetKeyDown(KeyCode.R)) // for testing
        {
            _playerController.ResetMarkers(); // make markers disappear in a pattern?
            _playerController.EnableTableView(true);
            _boardController.ToggleRayCastOfMarkerHolders(true);
            _campController.ToggleRayCastOfMarkerHolders(true);
        }

        if(Input.GetKeyDown(KeyCode.S)) // for testing
        {
            _campController.DisposeCampForRound();
        }
    }

    private void TestHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                task.Data.deckType = GetActiveDeckType();
                task.StartHandler(_boardController.BoardFillHandler, task.Data);
                break;
            default:
                _playerController.EnableTableView(true);
                _boardController.ToggleRayCastOfCards(true);
                _boardController.ToggleRayCastOfMarkerHolders(true);
                _campController.ToggleRayCastOfMarkerHolders(true);
                task.Complete();
                break;
        }
    }

    public void OnLogicEvent(object eventType, GameTaskItemData data)
    {
        new GameTask().ExecHandler(_logicEventHandlers[(int)eventType], data);
    }

    private DeckType GetActiveDeckType()
    {
        return DeckType.South; // changes to North after half-time
    }

    public void MarkerPlaceHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                Marker marker = task.Data.marker;
                MarkerHolder holder = (MarkerHolder)task.Data.holder;
                marker.AdjustAlpha(true);
                _playerController.EnableTableView(false);
                _boardController.ToggleRayCastOfMarkerHolders(false);
                _campController.ToggleRayCastOfMarkerHolders(false);
                if (holder.holderType == HolderType.BoardMarker)
                {
                    _boardController.SelectCard(marker, holder);
                }
                else if (holder.holderType == HolderType.CampMarker)
                {
                    _campController.ToggleCampAction(true);
                    _overlayController.ToggleMarkerActionScreen(marker);
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void MarkerCancelHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _boardController.ToggleBlackOverlayOfCardHolders(false, new int[][] { });
                HolderType type = task.Data.holder.holderType;
                if (type == HolderType.BoardMarker)
                {
                    _boardController.ToggleCardsSelection(false);
                }
                else if (type == HolderType.CampMarker)
                {
                    _campController.ToggleCampAction(false);
                    _overlayController.ToggleMarkerActionScreen(null);
                }
                _boardController.ToggleRayCastOfMarkerHolders(true);
                _campController.ToggleRayCastOfMarkerHolders(true);
                _playerController.GetRemainingMarkers().ForEach(marker => marker.AdjustAlpha(false));
                _playerController.EnableTableView(true);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void MarkerActionSelectHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _overlayController.ToggleMarkerActionScreen(null);
                _playerController.SetMarkerUsed();
                task.StartDelayMs(0);
                break;
            case 1:
                switch (task.Data.markerAction)
                {
                    case MarkerAction.PICK_ANY_CARD_FROM_BOARD:
                        task.StartHandler(_boardController.EnableAnyCardSelectionHandler);
                        break;
                    case MarkerAction.TAKE_2_ROAD_TOKENS:
                        task.StartHandler(_playerController.AddRoadTokensHandler);
                        break;
                    case MarkerAction.PICK_A_CARD_FROM_CHOSEN_DECK:
                        task.Data.deckType = GetActiveDeckType();
                        task.StartHandler(_overlayController.ShowDeckSelectionScreenHandler, task.Data);
                        break;
                    default:
                        task.StartHandler(_playerController.AddExtraCardPlacementHandler);
                        break;
                }
                break;
            case 2:
                if (Array.Exists(new[] { MarkerAction.TAKE_2_ROAD_TOKENS, MarkerAction.PLAY_UP_TO_2_CARDS }, markerAction => markerAction == task.Data.markerAction)) // marker action ends immediately
                {
                    _boardController.ToggleRayCastOfMarkerHolders(true);
                    _campController.ToggleRayCastOfMarkerHolders(true);
                    _playerController.EnableTableView(true);
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void DeckSelectHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler(_overlayController.HideDeckSelectionScreenHandler, task.Data);
                break;
            case 1:
                task.Data.cards = _boardController.GetTopCardsOfDeck(task.Data.deckType);
                task.StartHandler(_overlayController.ShowCardSelectionHandler, task.Data);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void ScoreCollectHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                _playerController.UpdateScore(task.Data.score);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void HandScreenHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                bool isToggled = task.Data.value;
                task.Data.dataCollection = _playerController.UpdateHandScreenButton(isToggled);
                task.StartHandler(_overlayController.GetHandScreenToggleHandler(isToggled), task.Data);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void TableToggleHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                bool value = task.Data.value;
                _boardController.ToggleRayCastOfCards(!value);
                _boardController.ToggleRayCastOfMarkerHolders(!value);
                _boardController.Fade(value);
                _campController.ToggleRayCastOfMarkerHolders(!value);
                _campController.Fade(value);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void CampIconsSelectHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler(_campController.StartViewSetupHandler);
                break;
            case 1:
                task.Data.deckType = GetActiveDeckType();
                task.StartHandler(_boardController.BoardFillHandler, task.Data);
                break;
            default:
                _playerController.EnableTableView(true);
                _boardController.ToggleRayCastOfCards(true);
                _boardController.ToggleRayCastOfMarkerHolders(true);
                _campController.ToggleRayCastOfMarkerHolders(true);
                task.Complete();
                break;
        }
    }

    private void CampToggleHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                bool value = task.Data.value;
                if(value)
                {
                    _campController.SaveCampScoreToken(_playerController.GetNextCampScoreToken());
                    _campController.EnableScoreButtonOfFulfilledIcons(_playerController.GetAdjacentPrimaryIconPairs());
                }
                _campController.ToggleCampView(value);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void CampScoreReceiveHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                _campController.ToggleCampAction(false);
                _playerController.UpdateCampScoreTokens();
                task.Data.targetTransform = _playerController.GetScoreTransform();
                task.StartHandler(_overlayController.CollectCampScoreHandler, task.Data);
                break;
            default:
                _campController.ToggleCampView(false);
                task.Complete();
                break;
        }
    }

    private void TableHitAreaHoverOverHandler(GameTask task)
    {
        HolderSubType subType = task.Data.subType;
        if ((_playerController.draggingCardType == CardType.Ground && subType == HolderSubType.PRIMARY) || (_playerController.draggingCardType == CardType.Landscape && subType == HolderSubType.SECONDARY))
        {
            _playerController.UpdateActiveCardHolders(subType, task.Data.hitAreaTag);
        }
        task.Complete();
    }

    private void CardPickHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _boardController.ToggleRayCastOfCards(false);
                _playerController.ToggleHandScreenHitarea(false);
                _playerController.EnableTableView(false);
                _boardController.ToggleBlackOverlayOfCardHolders(false, new int[][] { });
                if (task.Data.holder == null) // card-pick from deck selection action
                {
                    task.Data.cards = _boardController.GetUnselectedTopCardsOfDeck(task.Data.card.ID);
                    task.StartHandler(_overlayController.HideCardSelectionHandler, task.Data);
                }
                else
                {
                    _boardController.ToggleCardsSelection(false);
                    task.Data.holder.RemoveItemFromContentList(task.Data.card);
                    _playerController.SetMarkerUsed();
                    task.StartDelayMs(0);
                }
                break;
            case 1:
                if (task.Data.holder == null)
                {
                    _boardController.DisposeTopCards();
                }
                task.StartDelayMs(0);
                break;
            case 2:
                task.StartHandler(_playerController.AddCardToHandHandler, task.Data);
                break;
            case 3:
                task.Data.deckType = GetActiveDeckType();
                task.StartHandler(_boardController.BoardFillHandler, task.Data);
                break;
            case 4:
                _playerController.ToggleHandScreenHitarea(true);
                _playerController.EnableTableView(true);
                _boardController.ToggleRayCastOfCards(true);
                if (_playerController.GetRemainingMarkers().Count > 0)
                {
                    _boardController.ToggleRayCastOfMarkerHolders(true);
                    _campController.ToggleRayCastOfMarkerHolders(true);
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void CardMoveHandler(GameTask task)
    {
        _playerController.ToggleHitArea(task.Data.card.Data.cardType);
        task.Complete();
    }

    private void CardInspectionStartHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                if(!_playerController.TableView.isTableVisible)
                {
                    _playerController.EnableTableView(false);
                    _boardController.ToggleRayCastOfCards(false);
                    _boardController.ToggleRayCastOfMarkerHolders(false);
                    _campController.ToggleRayCastOfMarkerHolders(false);
                }
                task.StartHandler(_overlayController.ShowCardInspectionScreenHandler, task.Data);
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
                task.StartHandler(_overlayController.HideCardInspectionScreenHandler);
                break;
            default:
                if(!_playerController.TableView.isTableVisible)
                {
                    _playerController.EnableTableView(true);
                    _boardController.ToggleRayCastOfCards(true);
                    _boardController.ToggleRayCastOfMarkerHolders(true);
                    _campController.ToggleRayCastOfMarkerHolders(true);
                }
                task.Complete();
                break;
        }
    }

    private void PendingCardPlaceHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                List<RaycastResult> raycastResults = task.Data.raycastResults;
                Card card = task.Data.card;
                CardHolder holder = null;
                foreach (RaycastResult result in raycastResults)
                {
                    holder = result.gameObject.GetComponent<CardHolder>();
                    TableCardHitArea hitArea = result.gameObject.GetComponent<TableCardHitArea>();
                    holder = hitArea ? _playerController.GetTableCardHolderOfHitArea(hitArea) : holder;
                    if (holder && _playerController.CanCardBePlaced(holder, card))
                    {
                        task.Data.raycastResults = null;
                        task.Data.pendingCardDataID = card.Data.ID;
                        task.Data.holder = holder;
                        _playerController.CreatePendingCardPlacement(task.Data);
                        break;
                    }
                }

                if (holder == null)
                {
                    if (card.Data.cardType == CardType.Landscape) // unfulfilled icon/road token requirements
                    {
                        _playerController.TableView.RemoveEmptyHolder(HolderSubType.SECONDARY);
                    }
                    card.MoveCardBackToHand(_playerController.HandView.transform);
                    task.StartDelayMs(0);
                }
                else
                {
                    task.Data.value = true;
                    task.StartHandler(_playerController.SnapCardHandler, task.Data);
                }
                _playerController.draggingCardType = CardType.None;
                _playerController.TableView.TogglePrimaryHitAreas(false);
                _playerController.TableView.ToggleSecondaryHitArea(false);
                break;
            default:
                task.Data.card.ToggleRayCast(true);
                task.Complete();
                break;
        }
    }

    private void CancelledPendingCardPlaceHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                task.Data.card.ToggleRayCast(false);
                task.Data.value = false;
                task.StartHandler(_playerController.SnapCardHandler, task.Data);
                _playerController.CancelPendingCardPlacement(task.Data);
                break;
            default:
                task.Data.card.ToggleRayCast(true);
                task.Complete();
                break;
        }
    }

    private void ApprovedPendingCardPlaceHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                task.Data.cards = _playerController.GetPlacedCardsWithScore();
                task.Data.targetTransform = _playerController.GetScoreTransform();
                task.StartHandler(_overlayController.CollectCardScoreHandler, task.Data);
                break;
            case 1:
                task.StartHandler(_playerController.UpdateDisplayIconsHandler);
                break;
            case 2:
                task.StartHandler(_playerController.CenterCardsInHandHandler);
                break;
            default:
                _playerController.ApplyPendingCardPlacement();
                task.Complete();
                break;
        }
    }

    public void OnMarkerHolderInteraction(object sender, InteractableHolderEventArgs args)
    {
        List<Marker> markers = _playerController.GetRemainingMarkers();
        if (args.scrollDirection == 1 || args.scrollDirection == -1)
        {
            _playerController.ShowSelectedMarker(args.scrollDirection, markers);
        }
        else if (args.isHoverIn)
        {
            _boardController.ShowMarkersAtBoard(sender as MarkerHolder, markers);
            _playerController.ShowSelectedMarker(0, markers);
        }
        else
        {
            _boardController.HideMarkersAtBoard(markers);
        }
    }
}
