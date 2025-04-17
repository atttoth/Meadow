using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using static PendingActionCreator;

public class PlayerController : UserController
{
    private PendingActionCreator _pendingPlacementActionCreator;
    private Button _tableToggleButton;
    private Button _tableApproveButton;
    private Button _turnEndButton;
    private Button _campToggleButton;
    private HandScreenHitArea _handScreenHitArea;
    private bool _isCampVisible;
    public CardType draggingCardType;

    public override void CreateUser(GameMode gameMode)
    {
        _tableView = transform.GetChild(0).GetComponent<PlayerTableView>();
        _iconDisplayView = _tableView.transform.GetChild(2).GetChild(0).GetComponent<IconDisplayView>();
        _infoView = _tableView.transform.GetChild(3).GetComponent<InfoView>();
        _handView = transform.GetChild(1).GetComponent<PlayerHandView>();
        _markerView = transform.GetChild(2).GetComponent<PlayerMarkerView>();
        (_tableView as PlayerTableView).Init();
        _iconDisplayView.Init();
        _infoView.Init();
        (_handView as PlayerHandView).Init();
        _markerView.Init(gameMode.GetMarkerColorByUserID(userID));
        _pendingPlacementActionCreator = new PendingActionCreator(true);

        _tableToggleButton = _tableView.transform.GetChild(2).GetComponent<Button>();
        _tableApproveButton = _tableView.transform.GetChild(1).GetComponent<Button>();

        SpriteAtlas atlas = GameResourceManager.Instance.Base;
        Transform turnEndButtonTransform = GameObject.Find("GameCanvas").transform.GetChild(2);
        turnEndButtonTransform.GetComponent<Image>().sprite = atlas.GetSprite("endTurn_base");
        _turnEndButton = turnEndButtonTransform.GetComponent<Button>();
        _turnEndButton.GetComponent<CanvasGroup>().alpha = 0f;
        SpriteState turnEndSpriteState = _turnEndButton.spriteState;
        turnEndSpriteState.selectedSprite = atlas.GetSprite("endTurn_base");
        turnEndSpriteState.highlightedSprite = atlas.GetSprite("endTurn_highlighted");
        turnEndSpriteState.pressedSprite = atlas.GetSprite("endTurn_highlighted");
        turnEndSpriteState.disabledSprite = atlas.GetSprite("endTurn_disabled");
        _turnEndButton.spriteState = turnEndSpriteState;

        Transform campToggleButtonTransform = _infoView.transform.GetChild(3);
        campToggleButtonTransform.GetComponent<Image>().sprite = atlas.GetSprite("campfire_base");
        _campToggleButton = campToggleButtonTransform.GetComponent<Button>();
        SpriteState campToggleSpriteState = _campToggleButton.spriteState;
        campToggleSpriteState.selectedSprite = atlas.GetSprite("campfire_base");
        campToggleSpriteState.highlightedSprite = atlas.GetSprite("campfire_highlighted");
        campToggleSpriteState.pressedSprite = atlas.GetSprite("campfire_base");
        campToggleSpriteState.disabledSprite = atlas.GetSprite("campfire_disabled");
        _campToggleButton.spriteState = campToggleSpriteState;

        _tableToggleButton.onClick.AddListener(() => ToggleTable());
        _tableApproveButton.onClick.AddListener(() =>
        {
            if (_pendingPlacementActionCreator.GetNumOfActions() > 0)
            {
                EnableTableApproveButton(false);
                (_tableView as PlayerTableView).UpdateApproveButton(false);
                _dispatcher.InvokeEventHandler(GameLogicEventType.APPROVED_PENDING_CARD_PLACED, new object[] { GetPlacedCardsWithScore(), _infoView.scoreTransform.position });
            }
            else
            {
                ToggleTable();
            }
        });
        _turnEndButton.onClick.AddListener(() => EndTurn());
        ToggleTurnEndButton(false);
        _campToggleButton.onClick.AddListener(() => ToggleCamp());

        _handScreenHitArea = transform.GetChild(3).GetComponent<HandScreenHitArea>();
        _handScreenHitArea.Init();
        ToggleHandScreenHitarea(false);

        _allIconsOfPrimaryHoldersInOrder = new();
        _allIconsOfSecondaryHoldersInOrder = new();
        draggingCardType = CardType.None;
        base.CreateUser(gameMode);
    }

    public PlayerTableView TableView { get { return _tableView as PlayerTableView; } }

    public PlayerHandView HandView { get { return _handView as PlayerHandView; } }

    public void ToggleTable(bool isGameSetup = false)
    {
        (_tableView as PlayerTableView).TogglePanel();
        (_handView as PlayerHandView).ToggleHand();
        if(!isGameSetup)
        {
            _markerView.Fade((_tableView as PlayerTableView).isTableVisible);
            FadeTurnEndButton(!(_tableView as PlayerTableView).isTableVisible);
            ToggleHandScreenHitarea(!(_tableView as PlayerTableView).isTableVisible);
        }
        _dispatcher.InvokeEventHandler(GameLogicEventType.TABLE_TOGGLED, new object[] { !(_tableView as PlayerTableView).isTableVisible });
    }

    public void ToggleCamp()
    {
        _isCampVisible = !_isCampVisible;
        _dispatcher.InvokeEventHandler(GameLogicEventType.CAMP_TOGGLED, new object[] { _isCampVisible });
        Transform parent = _isCampVisible ? transform.root : _infoView.transform;
        _campToggleButton.transform.SetParent(parent); // place button above camp view in the hierarchy
    }

    public void FadeTurnEndButton(bool value)
    {
        float targetValue = value ? 1f : 0f;
        DOTween.Sequence().Append(_turnEndButton.GetComponent<CanvasGroup>().DOFade(targetValue, GameSettings.Instance.GetDuration(Duration.gameUIFadeDuration)));
    }

    public void ToggleTurnEndButton(bool value)
    {
        if(value && !_markerView.IsMarkerConsumed)
        {
            return;
        }
        _turnEndButton.enabled = value;
    }

    private List<CardIcon[]> GetTopPrimaryIcons() // sorted primary holders (left to right)
    {
        List<CardIcon[]> topIcons = new();
        _allIconsOfPrimaryHoldersInOrder
            .OrderBy(e => (_tableView as PlayerTableView).GetActivePrimaryCardHolderByID(e.Key).transform.GetSiblingIndex())
            .Select(e => e.Value)
            .ToList()
            .ForEach(values =>
            {
                CardIcon[] icons = values[^1].Where(icon => (int)icon > 4).ToArray();
                topIcons.Add(icons);
            });
        
        return topIcons;
    }

    public void ToggleHitArea(CardType cardType)
    {
        draggingCardType = cardType;
        if(_infoView.HasEnoughCardPlacements())
        {
            if (cardType == CardType.Ground)
            {
                (_tableView as PlayerTableView).TogglePrimaryHitAreas(true);
            }
            else if (cardType == CardType.Landscape)
            {
                (_tableView as PlayerTableView).ToggleSecondaryHitArea(true);
            }
        }
    }

    public CardHolder GetTableCardHolderOfHitArea(TableCardHitArea hitArea)
    {
        if(hitArea.type == HolderSubType.PRIMARY)
        {
            return (_tableView as PlayerTableView).GetActivePrimaryCardHolderByTag(hitArea.tag);
        }
        else
        {
            return (_tableView as PlayerTableView).GetActiveSecondaryCardHolder();
        }
    }

    public void UpdateActiveCardHolders(HolderSubType subType, string hitAreaTag)
    {
        if(subType == HolderSubType.PRIMARY)
        {
            if (string.IsNullOrEmpty(hitAreaTag))
            {
                (_tableView as PlayerTableView).RemoveEmptyHolder(HolderSubType.PRIMARY);
            }
            else
            {
                (_tableView as PlayerTableView).AddEmptyPrimaryHolder(hitAreaTag);
            }
            (_tableView as PlayerTableView).AlignPrimaryCardHoldersToCenter();
        }
        else if(subType == HolderSubType.SECONDARY)
        {
            if (string.IsNullOrEmpty(hitAreaTag))
            {
                (_tableView as PlayerTableView).RemoveEmptyHolder(HolderSubType.SECONDARY);
                (_tableView as PlayerTableView).AlignSecondaryCardHoldersToLeft();
            }
            else
            {
                (_tableView as PlayerTableView).AddEmptySecondaryHolder();
            }
        }
    }

    public void EnableTableToggleButton(bool value)
    {
        _tableToggleButton.enabled = value;
    }

    public void EnableTableApproveButton(bool value)
    {
        _tableApproveButton.enabled = value;
    }

    public List<List<CardIcon>> GetAdjacentPrimaryIconPairs() 
    {
        List<List<CardIcon>> pairs = new();
        List<CardIcon[]> topIcons = GetTopPrimaryIcons();
        if (topIcons.Count < 2)
        {
            return pairs;
        }

        for (int i = 0; i < topIcons.Count - 1; i++) // create pairs for every posible adjacent icon combinations
        {
            CardIcon[] icons1 = topIcons[i];
            CardIcon[] icons2 = topIcons[i + 1];
            int length = icons1.Length * icons2.Length;
            CardIcon[][] adjacentIcons = new CardIcon[][] { icons1, icons2 };
            adjacentIcons.OrderBy(icons => icons.Length).Reverse();
            int index1 = 0;
            int index2 = 0;
            for (int j = 0; j < length; j++)
            {
                CardIcon icon1 = adjacentIcons[0][index1];
                CardIcon icon2 = adjacentIcons[1][index2];
                if (icon1 != icon2) // ignore same icon pairs
                {
                    List<CardIcon> pair = new() { icon1, icon2 };
                    pairs.Add(pair);
                }
                index1++;
                if(index1 > adjacentIcons[0].Length - 1)
                {
                    index1 = 0;
                    index2++;
                }
            }
        }
        return pairs;
    }

    public void CenterCardsInHandHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                (_handView as PlayerHandView).MoveCardsHorizontallyInHand((_handView as PlayerHandView).GetLayoutPositions());
                task.StartDelayMs(500);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public override void PlaceInitialGroundCardOnTable(GameTask task, Card card)
    {
        switch(task.State)
        {
            case 0:
                (_handView as PlayerHandView).EnableCardsRaycast(false);
                ToggleTable(true);
                task.StartDelayMs(2000);
                break;
            case 1:
                UpdateActiveCardHolders(HolderSubType.PRIMARY, "RectLeft");
                CardHolder holder = (_tableView as PlayerTableView).GetActivePrimaryCardHolderByID(0);
                CreatePendingCardPlacement(holder, card);
                task.StartHandler((Action<GameTask, CardHolder, Card>)SnapCardHandler, holder, card);
                break;
            case 2:
                task.StartDelayMs(1000);
                break;
            case 3:
                task.StartHandler((Action<GameTask>)ApplyPendingCardPlacementHandler);
                break;
            default:
                ToggleTable(true);
                (_handView as PlayerHandView).EnableCardsRaycast(true);
                task.Complete();
                break;
        }
    }

    private void UpdateCurrentIconsOfHolderAction(object[] args)
    {
        bool isActionCancelled = (bool)args[1];
        CardHolder holder = (CardHolder)args[2];
        Card card = (Card)args[3];
        Dictionary<int, CardIcon[][]> collection = holder.holderSubType == HolderSubType.PRIMARY ? _allIconsOfPrimaryHoldersInOrder : _allIconsOfSecondaryHoldersInOrder;
        int ID = holder.ID;
        CardIcon[][] items = collection[ID];
        collection.Remove(ID);

        List<CardIcon[]> updatedItems = new();
        if(isActionCancelled)
        {
            if (!Array.Exists(new[] { CardType.Ground, CardType.Landscape }, cardType => cardType == card.Data.cardType))
            {
                for (int i = 0; i < items.Length - 1; i++)
                {
                    updatedItems.Add(items[i]);
                }
                collection.Add(ID, updatedItems.ToArray());
            }
        }
        else
        {
            foreach (CardIcon[] item in items)
            {
                updatedItems.Add(item);
            }
            updatedItems.Add(card.Data.icons);
            collection.Add(ID, updatedItems.ToArray());
        }
    }

    private void UpdateCurrentIconsEntryAction(object[] args)
    {
        bool isActionCancelled = (bool)args[1];
        CardHolder holder = (CardHolder)args[2];
        Card card = (Card)args[3];
        if(isActionCancelled)
        {
            if (card.Data.cardType == CardType.Ground)
            {
                _allIconsOfPrimaryHoldersInOrder.Remove(holder.ID);
                UpdateActiveCardHolders(holder.holderSubType, null);
            }
            else if (card.Data.cardType == CardType.Landscape)
            {
                _allIconsOfSecondaryHoldersInOrder.Remove(holder.ID);
                UpdateActiveCardHolders(holder.holderSubType, null);
            }
        }
        else
        {
            if (card.Data.cardType == CardType.Ground)
            {
                _allIconsOfPrimaryHoldersInOrder.Add(holder.ID, new CardIcon[][] { });
            }
            else if (card.Data.cardType == CardType.Landscape)
            {
                _allIconsOfSecondaryHoldersInOrder.Add(holder.ID, new CardIcon[][] { });
            }
        }
    }

    public void CreatePendingCardPlacement(CardHolder holder, Card card)
    {
        PendingActionFunction[] actionFunctions = new PendingActionFunction[] {
            _infoView.UpdateNumberOfCardPlacementsAction,
            _infoView.UpdateRoadTokensAction,
            UpdateCurrentIconsEntryAction,
            (_handView as PlayerHandView).PlaceCardFromHandAction,
            (_tableView as PlayerTableView).RegisterCardPlacementAction,
            (_tableView as PlayerTableView).AdjustHolderVerticallyAction,
            (_tableView as PlayerTableView).UpdateHitAreaSizeAction,
            UpdateCurrentIconsOfHolderAction
        };
        _pendingPlacementActionCreator.Create(actionFunctions, actionFunctions.Reverse().ToArray(), card.Data.ID, false, holder, card);
    }

    public void CancelPendingCardPlacement(GameTask task, Card card)
    {
        switch(task.State)
        {
            case 0:
                if (!_pendingPlacementActionCreator.TryCancel(card.Data.ID))
                {
                    task.Complete();
                    return;
                }

                if (_pendingPlacementActionCreator.GetNumOfActions() == 0)
                {
                    (_tableView as PlayerTableView).UpdateApproveButton(false);
                    _tableToggleButton.enabled = true;
                }
                task.StartHandler((Action<GameTask, CardHolder, Card>)SnapCardHandler, null, card);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void ApplyPendingCardPlacementHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                task.StartHandler((Action<GameTask>)UpdateDisplayIconsHandler);
                break;
            case 1:
                task.StartHandler((Action<GameTask>)CenterCardsInHandHandler);
                break;
            case 2:
                _pendingPlacementActionCreator.Dispose();
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void SnapCardHandler(GameTask task, CardHolder holder, Card card)
    {
        switch(task.State)
        {
            case 0:
                float speed = GameSettings.Instance.GetDuration(Duration.cardSnapSpeed);
                float[] positions = (_handView as PlayerHandView).GetLayoutPositions();
                Transform parentTransform = card.cardStatus == CardStatus.PENDING_ON_TABLE ? _tableView.transform.GetChild(0).transform : _handView.transform;
                (_handView as PlayerHandView).MoveCardsHorizontallyInHand(positions);
                (_tableView as PlayerTableView).PositionTableCard(holder, card, speed, positions, parentTransform);
                task.StartDelayMs((int)(speed * 1000));
                break;
            default:
                if(card.cardStatus == CardStatus.PENDING_ON_TABLE)
                {
                    card.transform.SetParent(holder.transform);
                }
                task.Complete();
                break;
        }
    }

    private List<Card> GetPlacedCardsWithScore()
    {
        List<object[]> dataCollection = _pendingPlacementActionCreator.GetDataCollection();
        List<Card> primaryTableCards = dataCollection
            .Select(data => (Card)data[3])
            .Where(card => card.transform.parent.GetComponent<CardHolder>().holderSubType == HolderSubType.PRIMARY && card.Data.cardType != CardType.Ground)
            .OrderBy(card => card.transform.parent.GetSiblingIndex())
            .ToList();

        List<Card> secondaryTableCards = dataCollection
            .Select(data => (Card)data[3])
            .Where(card => card.transform.parent.GetComponent<CardHolder>().holderSubType == HolderSubType.SECONDARY)
            .OrderBy(card => card.transform.parent.GetSiblingIndex())
            .ToList();
        primaryTableCards.AddRange(secondaryTableCards); // collect score of primary table first
        return primaryTableCards;
    }

    private void UpdateDisplayIconsHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                List<object[]> dataCollection = _pendingPlacementActionCreator.GetDataCollection();
                List<Card> primaryTableCards = new();
                for(int i = 0; i < dataCollection.Count; i++)
                {
                    CardHolder holder = (CardHolder)dataCollection[i][2];
                    Card card = (Card)dataCollection[i][3];
                    if(holder.holderSubType == HolderSubType.PRIMARY && holder.IsTopCardOfHolder(card)) // filter top cards of primary table holders
                    {
                        primaryTableCards.Add(card);
                    }
                    card.cardStatus = CardStatus.USED;
                }

                if(primaryTableCards.Count > 0)
                {
                    task.StartHandler((Action<GameTask, List<Card>, List<CardHolder>>)_iconDisplayView.UpdateIcons, primaryTableCards, _tableView.ActivePrimaryCardHolders);
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

    public void ShowSelectedMarker(int value, List<Marker> markers)
    {
        if (markers[0].ID == MarkerView.BLANK_MARKER_ID)
        {
            markers[0].gameObject.SetActive(true);
        }
        else
        {
            Marker currentMarker = _markerView.GetCurrentMarker(value);
            markers.ForEach(marker => marker.gameObject.SetActive(marker == currentMarker));
        }
    }

    public void ToggleHandScreenHitarea(bool value)
    {
        bool status = value && (_handView as PlayerHandView).GetNumberOfCards() > 10;
        _handScreenHitArea.Toggle(status);
    }

    public List<CardData> UpdateHandScreenButton(bool isToggled)
    {
        List<CardData> dataCollection = (_handView as PlayerHandView).GetDataCollection();
        _handScreenHitArea.transform.SetParent(isToggled ? transform.root : transform);
        _handScreenHitArea.SetupHitAreaImage(dataCollection.Last());
        _handScreenHitArea.ToggleHitAreaImage(isToggled);
        _handView.gameObject.SetActive(!isToggled);
        return isToggled ? dataCollection : null;
    }

    public void UpdateHandCardsStatus(bool isInspectionStarted)
    {
        if(isInspectionStarted)
        {
            _handView.transform.SetParent(transform.root);
        }
        else
        {
            _handView.transform.SetParent(transform);
            _handView.transform.SetSiblingIndex(2);
        }
        (_handView as PlayerHandView).ToggleDisposableFlagOnCards(isInspectionStarted);
        (_handView as PlayerHandView).ToggleBehaviorFlagsOnCards(isInspectionStarted);
    }

    public void DisposeHandCards()
    {
        List<Card> cards = (_handView as PlayerHandView).GetDisposableCards();
        cards.ForEach(card => Destroy(card.gameObject)); // play card destroy anim
    }

    public void RemoveCardIconItem(CardIconItem item) //todo update icon layout on card
    {
        Card card = (_handView as PlayerHandView).GetInspectedCard();
        card.RemoveRequirementsFromCardData(item);
        card.CardIconItemsView.DeleteIconItemByID(item.ID);
        card.CardIconItemsView.PositionRequiredIconItems();
    }
}
