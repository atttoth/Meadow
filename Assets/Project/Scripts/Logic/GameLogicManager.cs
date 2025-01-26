using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
            CardPickHandler,
            PendingCardPlaceHandler,
            CardInspectionStartHandler,
            CardInspectionEndHandler,
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
        return DeckType.North; // changes to North after half-time
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
                task.Data.cards = _playerController.GetPlacedCardsWithScore();
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
        List<CardIcon> primaryTableIcons = _playerController.GetAllCurrentIcons(HolderSubType.PRIMARY);
        List<CardIcon> mainRequirements = card.Data.requirements.ToList();

        if (holder.holderSubType == HolderSubType.PRIMARY)
        {
            if (card.Data.cardType == CardType.Ground)
            {
                if(holder.IsEmpty())
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }

            List<CardIcon> holderIcons = holder.GetAllIconsOfHolder();
            if (holderIcons.Contains(CardIcon.Deer))
            {
                return false;
            }

            if(mainRequirements.Contains(CardIcon.AllDifferent)) // only occurs at card ID 195
            {
                List<CardIcon> allTableIcons = new(primaryTableIcons);
                allTableIcons.AddRange(_playerController.GetAllCurrentIcons(HolderSubType.SECONDARY));
                return GetDistinctTableIcons(allTableIcons) >= mainRequirements.Where(icon => icon == CardIcon.AllDifferent).ToList().Count;
            }

            List<CardIcon> optionalRequirements = card.Data.optionalRequirements.ToList();
            List<CardIcon> adjacentRequirements = card.Data.adjacentRequirements.ToList();
            bool mainGlobalCondition = PassedGlobalRequirements(primaryTableIcons, mainRequirements);
            bool optionalGlobalCondition = PassedOptionalGlobalRequirements(primaryTableIcons, optionalRequirements);
            bool adjacentGlobalCondition = PassedSingleRequirement(primaryTableIcons, adjacentRequirements);

            if (mainGlobalCondition && optionalGlobalCondition && adjacentGlobalCondition) // check for combined requirement types
            {
                List<List<CardIcon>> adjacentHolderIcons = _playerController.TableView.GetAdjacentHolderIcons(holder);
                if (optionalRequirements.Count > 0 && adjacentRequirements.Count > 0)
                {
                    List<CardIcon[]> pairs = CreateIconPairsFromRequirements(optionalRequirements);
                    for (int i = adjacentRequirements.Count - 1; i >= 0; i--) // add optional icon to main requirements that is not present in both adjacent and optional requirements
                    {
                        CardIcon adjacentIcon = adjacentRequirements[i];
                        for(int j = pairs.Count - 1; j >= 0; j--)
                        {
                            CardIcon[] pair = pairs[j];
                            if(!pair.Contains(adjacentIcon))
                            {
                                mainRequirements.AddRange(pair);
                                pairs.RemoveAt(j);
                            }
                        }
                    }
                    return PassedAdjacentIconRequirements(adjacentHolderIcons, adjacentRequirements) || PassedSingleRequirement(holderIcons, mainRequirements);
                }
                else if (optionalRequirements.Count > 0)
                {
                    mainRequirements.AddRange(optionalRequirements);
                    return PassedSingleRequirement(holderIcons, mainRequirements);
                }
                else if(adjacentRequirements.Count > 0 && mainRequirements.Count == 0)
                {
                    return PassedAdjacentIconRequirements(adjacentHolderIcons, adjacentRequirements);
                }
                else if (adjacentRequirements.Count > 0)
                {
                    return PassedAdjacentIconRequirements(adjacentHolderIcons, adjacentRequirements) || PassedSingleRequirement(holderIcons, mainRequirements);
                }
                else
                {
                    return PassedSingleRequirement(holderIcons, mainRequirements);
                }
            }
            else
            {
                return false;
            }
        }
        else
        {
            int numOfIcons = mainRequirements.Count;
            mainRequirements = mainRequirements.Where(icon => icon != CardIcon.RoadToken).ToList();
            if(!_playerController.HasEnoughRoadTokens(numOfIcons - mainRequirements.Count) || !Array.Exists(new CardType[] { CardType.Landscape, CardType.Discovery }, type => type == card.Data.cardType))
            {
                return false;
            }

            primaryTableIcons.AddRange(_playerController.GetAllCurrentIcons(HolderSubType.SECONDARY)); // expand primary icons with secondary icons
            if (holder.IsEmpty() && card.Data.cardType == CardType.Landscape)
            {
                if(mainRequirements.Contains(CardIcon.AllMatching)) // only occurs at card ID 172
                {
                    return GetMostCommonTableIconsCount(primaryTableIcons) >= mainRequirements.Where(icon => icon == CardIcon.AllMatching).ToList().Count;
                }
                else if(card.Data.requirements.Length == 1) // card has only road token requirement
                {
                    return true;
                }
            }

            if(PassedGlobalRequirements(primaryTableIcons, mainRequirements))
            {
                if(card.Data.cardType == CardType.Landscape)
                {
                    return true;
                }
                else
                {
                    return PassedSingleRequirement(holder.GetAllIconsOfHolder(), mainRequirements);
                }
            }
            else
            {
                return false;
            }
        }
    }

    private int GetMostCommonTableIconsCount(List<CardIcon> allTableIcons)
    {
        if(allTableIcons.Count == 0)
        {
            return 0;
        }
        else
        {
            return allTableIcons.GroupBy(icon => icon).Select(g => new { Icon = g.Key, Count = g.Count() }).ToList().Max(g => g.Count);
        }
    }

    private int GetDistinctTableIcons(List<CardIcon> allTableIcons)
    {
        return allTableIcons.Distinct().ToList().Count;
    }

    private List<CardIcon[]> CreateIconPairsFromRequirements(List<CardIcon> requirements)
    {
        List<CardIcon[]> pairs = new();
        for (int i = 0; i < requirements.Count; i++)
        {
            if (i % 2 == 0)
            {
                CardIcon icon1 = requirements[i];
                CardIcon icon2 = requirements[i + 1];
                CardIcon[] pair = new CardIcon[] { icon1, icon2 };
                pairs.Add(pair);
            }
        }
        return pairs;
    }

    private bool PassedGlobalRequirements(List<CardIcon> allTableIcons, List<CardIcon> requirements)
    {
        if (requirements.Count < 1)
        {
            return true;
        }
        
        List<CardIcon> allIcons = new(allTableIcons);
        List<CardIcon> remainingRequirements = new(requirements);
        for (int i = remainingRequirements.Count - 1; i >= 0; i--)
        {
            CardIcon requirement = remainingRequirements[i];
            for(int j = allIcons.Count - 1; j >= 0; j--)
            {
                if(requirement == allIcons[j])
                {
                    remainingRequirements.RemoveAt(i);
                    allIcons.RemoveAt(j);
                    break;
                }
            }
            if(remainingRequirements.Count == 0)
            {
                return true;
            }
        }
        return false;
    }

    private bool PassedOptionalGlobalRequirements(List<CardIcon> allTableIcons, List<CardIcon> requirements)
    {
        if(requirements.Count < 2)
        {
            return true;
        }

        List<CardIcon> allIcons = new(allTableIcons);
        List<CardIcon[]> pairs = CreateIconPairsFromRequirements(requirements);
        for (int i = allIcons.Count - 1; i >= 0; i--)
        {
            CardIcon cardIcon = allIcons[i];
            for(int j = pairs.Count - 1; j >= 0; j--)
            {
                CardIcon[] pair = pairs[j];
                if(Array.Exists(pair, icon => icon == cardIcon))
                {
                    pairs.Remove(pair);
                    allIcons.RemoveAt(i);
                    break;
                }
            }
            if(pairs.Count < 1)
            {
                return true;
            }
        }
        return false;
    }

    private bool PassedAdjacentIconRequirements(List<List<CardIcon>> adjacentHolderIcons, List<CardIcon> requirements)
    {
        for(int i = 0; i < adjacentHolderIcons.Count; i++)
        {
            List<CardIcon> icons = adjacentHolderIcons[i];
            for(int j = 0; j < icons.Count; j++)
            {
                if(Array.Exists(requirements.ToArray(), icon => icon == icons[j]))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool PassedSingleRequirement(List<CardIcon> holderIcons, List<CardIcon> requirements)
    {
        if (requirements.Count < 1)
        {
            return true;
        }

        foreach (CardIcon requirement in requirements)
        {
            foreach (CardIcon icon in holderIcons)
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
