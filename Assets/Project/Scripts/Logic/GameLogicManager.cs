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
    private Delegate[] _logicEventHandlers;
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
        _logicEventHandlers = new Delegate[] {
            (Action<GameTask, bool>)TurnEndedHandler,
            (Action<GameTask, bool>)TableToggleHandler,
            (Action<GameTask>)CampIconsSelectHandler,
            (Action<GameTask, bool>)CampToggleHandler,
            (Action<GameTask, int, Vector3>)CampScoreReceiveHandler,
            (Action<GameTask, HolderSubType, string>)TableHitAreaHoverOverHandler,
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
            (Action<GameTask, HolderType>)MarkerCancelHandler,
            (Action<GameTask, MarkerAction>)MarkerActionSelectHandler,
            (Action<GameTask, DeckType>)DeckSelectHandler,
            (Action<GameTask, int>)ScoreCollectHandler,
            (Action<GameTask, bool>)HandScreenHandler
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
            //new GameTask().ExecHandler((Action<GameTask>)_campController.ShowViewSetupHandler);
            new GameTask().ExecHandler((Action<GameTask>)TestHandler);
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
                task.StartHandler((Action<GameTask, DeckType, List<Card>>)_boardController.BoardFillHandler, GetActiveDeckType(), new List<Card>());
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

    public void OnLogicEvent(object eventType, object[] args)
    {
        new GameTask().ExecHandler(_logicEventHandlers[(int)eventType], args);
    }

    private DeckType GetActiveDeckType()
    {
        return DeckType.South; // changes to North after half-time
    }

    public void MarkerPlaceHandler(GameTask task, MarkerHolder holder, Marker marker)
    {
        switch (task.State)
        {
            case 0:
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

    private void MarkerCancelHandler(GameTask task, HolderType type)
    {
        switch (task.State)
        {
            case 0:
                _boardController.ToggleBlackOverlayOfCardHolders(false, new int[][] { });
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

    private void MarkerActionSelectHandler(GameTask task, MarkerAction markerAction)
    {
        switch (task.State)
        {
            case 0:
                _overlayController.ToggleMarkerActionScreen(null);
                _playerController.SetMarkerUsed();
                task.StartDelayMs(0);
                break;
            case 1:
                switch (markerAction)
                {
                    case MarkerAction.PICK_ANY_CARD_FROM_BOARD:
                        task.StartHandler((Action<GameTask>)_boardController.EnableAnyCardSelectionHandler);
                        break;
                    case MarkerAction.TAKE_2_ROAD_TOKENS:
                        task.StartHandler((Action<GameTask>)_playerController.AddRoadTokensHandler);
                        break;
                    case MarkerAction.PICK_A_CARD_FROM_CHOSEN_DECK:
                        task.StartHandler(_overlayController.GetToggleDeckSelectionScreenHandler(), GetActiveDeckType(), true);
                        break;
                    default:
                        task.StartHandler((Action<GameTask>)_playerController.AddExtraCardPlacementHandler);
                        break;
                }
                break;
            case 2:
                if (Array.Exists(new[] { MarkerAction.TAKE_2_ROAD_TOKENS, MarkerAction.PLAY_UP_TO_2_CARDS }, action => action == markerAction)) // marker action ends immediately
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

    private void DeckSelectHandler(GameTask task, DeckType deckType)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler(_overlayController.GetToggleDeckSelectionScreenHandler(), deckType, false);
                break;
            case 1:
                task.StartHandler(_overlayController.GetCardSelectionToggleHandler(true), _boardController.GetTopCardsOfDeck(deckType));
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
                _playerController.UpdateScore(score);
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
                task.StartHandler(_overlayController.GetHandScreenToggleHandler(isToggled), _playerController.UpdateHandScreenButton(isToggled));
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void TurnEndedHandler(GameTask task, bool isEndedByPlayer)
    {
        task.Complete();
    }

    private void TableToggleHandler(GameTask task, bool value)
    {
        switch(task.State)
        {
            case 0:
                _boardController.ToggleRayCastOfCards(value);
                _boardController.ToggleRayCastOfMarkerHolders(value);
                _boardController.Fade(value);
                _campController.ToggleRayCastOfMarkerHolders(value);
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
                task.StartHandler((Action<GameTask>)_campController.StartViewSetupHandler);
                break;
            case 1:
                task.StartHandler((Action<GameTask, DeckType, List<Card>>)_boardController.BoardFillHandler, GetActiveDeckType(), new List<Card>());
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

    private void CampToggleHandler(GameTask task, bool value)
    {
        switch(task.State)
        {
            case 0:
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

    private void CampScoreReceiveHandler(GameTask task, int score, Vector3 originPosition)
    {
        switch(task.State)
        {
            case 0:
                _campController.ToggleCampAction(false);
                _playerController.UpdateCampScoreTokens();
                task.StartHandler((Action<GameTask, int, Vector3, Vector3>)_overlayController.CollectCampScoreHandler, score, originPosition, _playerController.GetScorePosition());
                break;
            default:
                _playerController.ToggleCamp();
                task.Complete();
                break;
        }
    }

    private void TableHitAreaHoverOverHandler(GameTask task, HolderSubType subType, string hitAreaTag)
    {
        if ((_playerController.draggingCardType == CardType.Ground && subType == HolderSubType.PRIMARY) || (_playerController.draggingCardType == CardType.Landscape && subType == HolderSubType.SECONDARY))
        {
            _playerController.UpdateActiveCardHolders(subType, hitAreaTag);
        }
        task.Complete();
    }

    private void CardPickHandler(GameTask task, CardHolder holder, Card card)
    {
        switch (task.State)
        {
            case 0:
                _boardController.ToggleRayCastOfCards(false);
                _playerController.ToggleHandScreenHitarea(false);
                _playerController.EnableTableView(false);
                _boardController.ToggleBlackOverlayOfCardHolders(false, new int[][] { });
                if (holder == null) // card-pick from deck selection action
                {
                    task.StartHandler(_overlayController.GetCardSelectionToggleHandler(false), _boardController.GetUnselectedTopCardsOfDeck(card.Data.ID));
                }
                else
                {
                    _boardController.ToggleCardsSelection(false);
                    holder.RemoveItemFromContentList(card);
                    _playerController.SetMarkerUsed();
                    task.StartDelayMs(0);
                }
                break;
            case 1:
                if (holder == null)
                {
                    _boardController.DisposeTopCards();
                }
                task.StartDelayMs(0);
                break;
            case 2:
                task.StartHandler((Action<GameTask, Card>)_playerController.AddCardToHandHandler, card);
                break;
            case 3:
                task.StartHandler((Action<GameTask, DeckType, List<Card>>)_boardController.BoardFillHandler, GetActiveDeckType(), new List<Card>());
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

    private void CardMoveHandler(GameTask task, CardType cardType)
    {
        _playerController.ToggleHitArea(cardType);
        task.Complete();
    }

    private void CardInspectionStartHandler(GameTask task, Card card)
    {
        switch (task.State)
        {
            case 0:
                if (_playerController.IsTableVisible())
                {
                    _playerController.UpdateHandCardsStatus(true);
                }
                else
                {
                    _playerController.EnableTableView(false);
                    _boardController.ToggleRayCastOfCards(false);
                    _boardController.ToggleRayCastOfMarkerHolders(false);
                    _campController.ToggleRayCastOfMarkerHolders(false);
                }
                task.StartHandler(_overlayController.GetCardInspectionScreenHandler(true), card, _playerController.IsTableVisible());
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
                task.StartHandler(_overlayController.GetCardInspectionScreenHandler(false));
                break;
            default:
                if(_playerController.IsTableVisible())
                {
                    _playerController.UpdateHandCardsStatus(false);
                }
                else
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

    private void CardIconSelectedHandler(GameTask task, int iconItemID)
    {
        _overlayController.UpdateInspectedCardIconsDisposeStatus(iconItemID);
        _overlayController.CheckCardIconRemoveConditions(_playerController.HandView.HasDisposableCardsSelected());
        task.Complete();
    }

    private void CardSelectedForDisposeHandler(GameTask task)
    {
        _overlayController.CheckCardIconRemoveConditions(_playerController.HandView.HasDisposableCardsSelected());
        task.Complete();
    }

    private void RemovedCardIconHandler(GameTask task, CardIconItem item)
    {
        switch(task.State)
        {
            case 0:
                _playerController.DisposeHandCardsHandler();
                task.StartHandler(_overlayController.GetRemoveIconItemHandler(), item);
                break;
            case 1:
                _playerController.RemoveCardIconItem(item);
                task.StartHandler((Action<GameTask>)_playerController.CenterCardsInHandHandler);
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
        switch(task.State)
        {
            case 0:
                CardHolder holder = null;
                foreach (RaycastResult result in raycastResults)
                {
                    holder = result.gameObject.GetComponent<CardHolder>();
                    TableCardHitArea hitArea = result.gameObject.GetComponent<TableCardHitArea>();
                    holder = hitArea ? _playerController.GetTableCardHolderOfHitArea(hitArea) : holder;
                    if (holder && _playerController.CanCardBePlaced(holder, card))
                    {
                        _playerController.CreatePendingCardPlacement(holder, card);
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
                    task.StartHandler((Action<GameTask, CardHolder, Card>)_playerController.SnapCardHandler, holder, card);
                }
                _playerController.draggingCardType = CardType.None;
                _playerController.TableView.TogglePrimaryHitAreas(false);
                _playerController.TableView.ToggleSecondaryHitArea(false);
                break;
            default:
                card.ToggleRayCast(true);
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
                task.StartHandler((Action<GameTask, Card>)_playerController.CancelPendingCardPlacement, card);
                break;
            default:
                card.ToggleRayCast(true);
                task.Complete();
                break;
        }
    }

    private void ApprovedPendingCardPlaceHandler(GameTask task, List<Card> cards, Vector3 targetPosition)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler((Action<GameTask, List <Card>, Vector3>)_overlayController.CollectCardScoreHandler, cards, targetPosition);
                break;
            case 1:
                task.StartHandler((Action<GameTask>)_playerController.UpdateDisplayIconsHandler);
                break;
            case 2:
                task.StartHandler((Action<GameTask>)_playerController.CenterCardsInHandHandler);
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
            if(markers.Count > 0)
            {
                _boardController.ShowMarkersAtBoard(sender as MarkerHolder, markers);
                _playerController.ShowSelectedMarker(0, markers);
            }
        }
        else
        {
            _boardController.HideMarkersAtBoard(markers);
        }
    }
}
