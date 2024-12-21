using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static Card;
using static GameTask;
using static MarkerHolder;

public class GameLogicManager : MonoBehaviour
{
    private GameSettings _gameSettings;
    private BoardManager _boardManager;
    private CampManager _campManager;
    private DeckManager _deckManager;
    private PlayerManager _playerManager;
    private OverlayManager _overlayManager;
    GameTaskHandler[] _eventHandlers;
    public bool isBoardFilled;
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
        _boardManager = ReferenceManager.Instance.boardManager;
        _campManager = ReferenceManager.Instance.campManager;
        _deckManager = ReferenceManager.Instance.deckManager;
        _playerManager = ReferenceManager.Instance.playerManager;
        _overlayManager = ReferenceManager.Instance.overlayManager;
        _eventHandlers = new GameTaskHandler[] {
            CardPickHandler,
            PendingCardPlaceHandler,
            ExamineCardHandler,
            ApprovedPendingCardPlaceHandler,
            CancelledPendingCardPlaceHandler,
            MarkerPlaceHandler,
            MarkerCancelHandler
        };

        _boardManager.CreateBoard();
        _campManager.CreateCamp();
        _deckManager.CreateAtlasList();
        _deckManager.CreateDecks();
        _deckManager.CreateDummyDecks();
        _playerManager.CreatePlayer();
        _overlayManager.CreateOverlay();
    }

    public GameSettings GameSettings { get { return _gameSettings; } }

    private void Update()
    {
        if (!isBoardFilled && Input.GetKeyDown(KeyCode.E)) // for testing
        {
            new GameTask().ExecHandler(InitialBoardFillHandler);
        }

        if(!hasRemainingMarkers && Input.GetKeyDown(KeyCode.R)) // for testing
        {
            _playerManager.Controller.ResetMarkers(); // make markers disappear in a pattern?
            _boardManager.ToggleMarkerHolders(true);
            _campManager.ToggleMarkerHolders(true);
        }
    }

    public void OnEvent(object eventType, GameTaskItemData data)
    {
        new GameTask().ExecHandler(_eventHandlers[(int)eventType], data);
    }

    private void InitialBoardFillHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                isBoardFilled = true;
                _playerManager.Controller.EnableTableView(false);
                SaveTargetHoldersAndCards();
                task.StartHandler(_boardManager.BoardFillHandler);
                break;
            default:
                _playerManager.Controller.EnableTableView(true);
                EnableRayTargetOInteractables(true);
                task.Complete();
                break;
        }
    }

    private void SaveTargetHoldersAndCards()
    {
        List<CardHolder> holders = new() { };
        List<Card> cards = new() { };
        for (int colIndex = 0; colIndex < (int)DeckType.NUM_OF_DECKS; colIndex++)
        {
            DeckType deckType = colIndex switch
            {
                0 => DeckType.West,
                1 or 2 => GetActiveDeckType(),
                _ => DeckType.East,
            };
            List<CardHolder> emptyHolders = _boardManager.GetEmptyCardHoldersByColumn(colIndex);
            if (emptyHolders.Count > 0)
            {
                emptyHolders.Reverse();
                List<Card> selectedCards = _deckManager.GetCardsReadyToDraw(emptyHolders.Count, deckType, colIndex);
                for (int i = 0; i < emptyHolders.Count; i++)
                {
                    CardHolder emptyHolder = emptyHolders[i];
                    Card card = selectedCards[i];
                    holders.Add(emptyHolder);
                    cards.Add(card);
                }
            }
        }
        _boardManager.PrepareCardDrawing(holders, cards);
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
        for(int i = 0; i < 2; i++)
        {
            CardHolder holder = _playerManager.Controller.GetTableView().AddHolderOnSecondaryPage();
            Card fakeCard = Instantiate(GameAssets.Instance.cardPrefab, transform).GetComponent<Card>();
            fakeCard.transform.parent = _playerManager.transform;
            Image image = fakeCard.GetComponent<Image>();
            image.color = new Color(image.color.r, image.color.g, image.color.b, 0f);
            CardIcon[] cardIcons = { CardIcon.Road };
            //fakeCard.Data = new(-1, default, CardType.None, null, null, null, cardIcons, 0);
            holder.AddToContentList(fakeCard);
            //_playerManager.Controller.CurrentIcons.Add(holder.ID, fakeCard.Data.icons); debug this
        }
    }

    public void EnableRayTargetOInteractables(bool value)
    {
        _boardManager.ToggleRayTargetOfCardsAndHolders(value);
    }

    public void MarkerPlaceHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                Marker marker = task.Data.marker;
                MarkerHolder holder = (MarkerHolder)task.Data.holder;
                marker.AdjustAlpha(true);
                _playerManager.Controller.EnableTableView(false);
                _boardManager.ToggleMarkerHolders(false);
                _campManager.ToggleMarkerHolders(false);
                switch (holder.holderType)
                {
                    case HolderType.BoardMarker:
                        _boardManager.SelectCard(marker, holder);
                        task.StartDelayMs(0);
                        break;
                    case HolderType.CampMarker:
                        _campManager.PlaceMarkerToCamp(holder, marker);
                        task.Data.sprite = marker.GetActionIcon();
                        task.Data.dummyType = DummyType.ACTION_ICON;
                        task.Data.isSpecialMarker = marker.ID == 5;
                        //_campManager.EnableCamp(true);
                        task.StartHandler(ExamineCardHandler); // camp handler?
                        break;
                }
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void MarkerCancelHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                _boardManager.EnableOverlayOfCardHolders(false, new int[] { });
                _boardManager.DeSelectCards();
                _boardManager.ToggleMarkerHolders(true);
                _campManager.ToggleMarkerHolders(true);
                _playerManager.Controller.GetRemainingMarkers().ForEach(marker => marker.AdjustAlpha(false));
                _playerManager.Controller.EnableTableView(true);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void CardPickHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                _boardManager.DeSelectCards();
                task.Data.holder.RemoveItemFromContentList(task.Data.card);
                _playerManager.Controller.SetMarkerUsed();
                hasRemainingMarkers = _playerManager.Controller.GetRemainingMarkers().Count > 0;
                EnableRayTargetOInteractables(false);
                _playerManager.Controller.EnableTableView(false);
                _boardManager.EnableOverlayOfCardHolders(false, new int[] { });
                _playerManager.Controller.GetHandView().MoveCardsHorizontallyInHand(_playerManager.Controller.IsTableVisible(), false);
                _playerManager.Controller.GetHandView().AddCardToHand(task.Data.card);
                task.StartDelayMs(1000);
                break;
            case 1:
                SaveTargetHoldersAndCards();
                task.StartHandler(_boardManager.BoardFillHandler);
                break;
            case 2:
                _playerManager.Controller.GetHandView().SetCardsReady();
                _playerManager.Controller.EnableTableView(true);
                EnableRayTargetOInteractables(true);
                if (hasRemainingMarkers)
                {
                    _boardManager.ToggleMarkerHolders(true);
                    _campManager.ToggleMarkerHolders(true);
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void ExamineCardHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                EnableRayTargetOInteractables(false);
                _overlayManager.SetDummy(task.Data.sprite, task.Data.needToRotate, task.Data.dummyType);
                _overlayManager.EnableDummy(true);
                switch (task.Data.dummyType)
                {
                    case DummyType.CARD:
                        _overlayManager.StartCardShowSequence();
                        break;
                    default:
                        if (task.Data.isSpecialMarker)
                        {
                            _overlayManager.ShowActionIcon(task.Data.isSpecialMarker);
                        }
                        else
                        {
                            _overlayManager.ShowActionIcon();
                        }
                        break;
                }
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void PendingCardPlaceHandler(GameTask task)
    {
        _playerManager.Controller.CreatePendingCardPlacement(task.Data);
        task.Complete();
    }

    private void CancelledPendingCardPlaceHandler(GameTask task)
    {
        _playerManager.Controller.CancelPendingCardPlacement(task.Data);
        task.Complete();
    }

    private void ApprovedPendingCardPlaceHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler(_playerManager.Controller.UpdateScoreHandler);
                break;
            case 1:
                task.StartHandler(_playerManager.Controller.UpdateDisplayIconsHandler);
                break;
            case 2:
                _playerManager.Controller.UpdateHandView();
                task.StartDelayMs(500);
                break;
            default:
                _playerManager.Controller.ApplyPendingCardPlacement();
                task.Complete();
                break;
        }
    }

    public void OnMarkerHolderInteraction(object sender, InteractableHolderEventArgs args)
    {
        List<Marker> markers = _playerManager.Controller.GetRemainingMarkers();
        if (args.scrollDirection == 1 || args.scrollDirection == -1)
        {
            _playerManager.Controller.ShowSelectedMarker(args.scrollDirection, markers);
        }
        else if (args.isHoverIn)
        {
            _boardManager.ShowMarkersAtBoard(sender as MarkerHolder, markers);
            _playerManager.Controller.ShowSelectedMarker(0, markers);
        }
        else
        {
            _boardManager.HideMarkersAtBoard(sender as MarkerHolder, markers);
        }
    }

    public bool CanCardBePlaced(CardHolder holder, Card card)
    {
        if (holder.IsEmpty())
        {
            return card.Data.cardType == CardType.Ground && !_playerManager.Controller.HasHolder(holder.ID);
        }

        if (card.Data.icons[0] == CardIcon.Deer)
        {
            return false;
        }

        List<CardIcon> iconsOfHolder = holder.GetAllIconsOfHolder();
        List<CardIcon> allTableIcons = _playerManager.Controller.GetAllCurrentIcons();

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
        
        List<CardIcon> adjacentHolderIcons = _playerManager.Controller.GetTableView().GetAdjacentHolderIcons(holder);
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
        if(optionalRequirements.Length < 1)
        {
            return true;
        }

        List<CardIcon[]> list = new();
        if(optionalRequirements.Length == 2)
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
            for(int i = 0; i < list.Count; i++)
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
            foreach(CardIcon icon in iconsOfHolder)
            {
                if(icon == requirement)
                {
                    return true;
                }
            }
        }
        return false;
    }
}
