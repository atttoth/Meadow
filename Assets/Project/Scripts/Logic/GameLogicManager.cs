using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
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

    private void Start()
    {
        SetNewRound();
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
            CardPickHandler,
            PendingCardPlaceHandler,
            ExamineCardHandler,
            ApprovedPendingCardPlaceHandler,
            CancelledPendingCardPlaceHandler,
            MarkerPlaceHandler,
            MarkerCancelHandler,
            MarkerActionSelectHandler,
            DeckSelectHandler,
            ScoreCollectHandler
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
            new GameTask().ExecHandler(_campController.ShowViewSetupHandler);
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

    public void OnLogicEvent(object eventType, GameTaskItemData data)
    {
        new GameTask().ExecHandler(_logicEventHandlers[(int)eventType], data);
    }

    private DeckType GetActiveDeckType()
    {
        return DeckType.South; // changes to North after half-time
    }

    private void SetNewRound()
    {
        //AddTwoSlotsWithRoadsOnSecondaryPage(); debug this holder Init() missing
    }

    private void AddTwoSlotsWithRoadsOnSecondaryPage()
    {
        for (int i = 0; i < 2; i++)
        {
            CardHolder holder = _playerController.TableView.AddHolderOnSecondaryPage();
            Card fakeCard = Instantiate(GameAssets.Instance.cardPrefab, transform).GetComponent<Card>();
            fakeCard.transform.parent = _playerController.transform;
            Image image = fakeCard.GetComponent<Image>();
            image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
            CardIcon[] cardIcons = { CardIcon.Road };
            //fakeCard.Data = new(-1, default, CardType.None, null, null, null, cardIcons, 0);
            holder.AddToContentList(fakeCard);
            //_playerController.CurrentIcons.Add(holder.ID, fakeCard.Data.icons); debug this
        }
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
                    _campController.EnableScoreButtonOfFulfilledIcons(_playerController.GetAdjacentIconPairs());
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

    private void CardPickHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                if (task.Data.holder == null)
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
                hasRemainingMarkers = _playerController.GetRemainingMarkers().Count > 0;
                _boardController.ToggleRayCastOfCards(false);
                _playerController.EnableTableView(false);
                _boardController.ToggleBlackOverlayOfCardHolders(false, new int[][] { });
                _playerController.HandView.MoveCardsHorizontallyInHand(_playerController.IsTableVisible(), false);
                _playerController.HandView.AddCardToHand(task.Data.card);
                task.StartDelayMs(1000);
                break;
            case 3:
                task.Data.deckType = GetActiveDeckType();
                task.StartHandler(_boardController.BoardFillHandler, task.Data);
                break;
            case 4:
                _playerController.HandView.SetCardsReady();
                _playerController.EnableTableView(true);
                _boardController.ToggleRayCastOfCards(true);
                if (hasRemainingMarkers)
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

    private void ExamineCardHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _boardController.ToggleRayCastOfCards(false);
                _overlayController.SetDummy(task.Data.sprite, task.Data.value, task.Data.dummyType);
                _overlayController.EnableDummy(true);
                _overlayController.StartCardShowSequence();
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void PendingCardPlaceHandler(GameTask task)
    {
        _playerController.CreatePendingCardPlacement(task.Data);
        task.Complete();
    }

    private void CancelledPendingCardPlaceHandler(GameTask task)
    {
        _playerController.CancelPendingCardPlacement(task.Data);
        task.Complete();
    }

    private void ApprovedPendingCardPlaceHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                task.Data.cards = _playerController.GetPlacedCards();
                task.Data.targetTransform = _playerController.GetScoreTransform();
                task.StartHandler(_overlayController.CollectCardScoreHandler, task.Data);
                break;
            case 1:
                task.StartHandler(_playerController.UpdateDisplayIconsHandler);
                break;
            case 2:
                task.StartHandler(_playerController.UpdateHandViewHandler);
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

    public bool CanCardBePlaced(CardHolder holder, Card card)
    {
        if (holder.IsEmpty())
        {
            return card.Data.cardType == CardType.Ground && !_playerController.HasHolder(holder.ID);
        }

        if (card.Data.icons[0] == CardIcon.Deer)
        {
            return false;
        }

        List<CardIcon> iconsOfHolder = holder.GetAllIconsOfHolder();
        List<CardIcon> allTableIcons = _playerController.GetAllCurrentIcons();

        CardIcon[] allRequirements = card.Data.requirements;
        CardIcon[] optionalRequirements = card.Data.optionalRequirements;
        if (optionalRequirements.Length > 0)
        {
            List<CardIcon> updatedRequirements = new(allRequirements);
            foreach (CardIcon cardIcon in optionalRequirements)
            {
                updatedRequirements.Add(cardIcon);
            }
            allRequirements = updatedRequirements.ToArray();
        }

        List<CardIcon> adjacentHolderIcons = _playerController.TableView.GetAdjacentHolderIcons(holder);
        if (PassedGlobalRequirements(allTableIcons, allRequirements) && PassedOptionalRequirements(allTableIcons, optionalRequirements))
        {
            CardIcon[] adjacentRequirements = card.Data.adjacentRequirements;
            CardIcon[] requirements = adjacentRequirements.Length < 1 ? allRequirements : adjacentRequirements;
            CardIcon[] holderIcons = adjacentRequirements.Length < 1 ? iconsOfHolder.ToArray() : adjacentHolderIcons.ToArray();
            return PassedTopCardRequirements(requirements, holderIcons);
        }
        else
        {
            return false;
        }
    }

    private bool PassedGlobalRequirements(List<CardIcon> allIcons, CardIcon[] requirements)
    {
        if (requirements.Length < 1)
        {
            return true;
        }

        List<CardIcon> remainingIcons = new(allIcons);
        int count = 0;
        foreach (CardIcon requirement in requirements)
        {
            for (int i = remainingIcons.Count - 1; i >= 0; i--)
            {
                CardIcon icon = remainingIcons[i];
                if (icon == requirement)
                {
                    count++;
                    remainingIcons.Remove(icon);
                    if (count == requirements.Length)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private bool PassedOptionalRequirements(List<CardIcon> allTableIcons, CardIcon[] optionalRequirements)
    {
        if (optionalRequirements.Length < 1)
        {
            return true;
        }

        List<CardIcon[]> list = new();
        if (optionalRequirements.Length == 2)
        {
            list.Add(optionalRequirements);
        }
        else
        {
            CardIcon[] arr1 = new CardIcon[] { optionalRequirements[0], optionalRequirements[1] };
            CardIcon[] arr2 = new CardIcon[] { optionalRequirements[2], optionalRequirements[3] };
            list.Add(arr1);
            list.Add(arr2);
        }


        int counter = 0;
        int value = optionalRequirements.Length == 2 ? 1 : 2;
        foreach (CardIcon icon in allTableIcons)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var arr = list[i];
                if (icon == arr[0] || icon == arr[1])
                {
                    list.Remove(arr);
                    counter++;
                    if (counter == value)
                    {
                        return true;
                    }
                }
            }

        }
        return false;
    }

    private bool PassedTopCardRequirements(CardIcon[] requirements, CardIcon[] iconsOfHolder)
    {
        foreach (CardIcon requirement in requirements)
        {
            foreach (CardIcon icon in iconsOfHolder)
            {
                if (icon == requirement)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
