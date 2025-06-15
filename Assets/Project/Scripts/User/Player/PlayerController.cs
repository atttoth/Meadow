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
        _markerView.Init(gameMode.CurrentUserColors[userID]);
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
        EnableTurnEndButton(false);
        _campToggleButton.onClick.AddListener(() => ToggleCamp());
        EnableCampButton(false);
        _handScreenHitArea = transform.GetChild(3).GetComponent<HandScreenHitArea>();
        _handScreenHitArea.Init();
        ToggleHandScreenHitarea(false);
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

    public void EnableTurnEndButton(bool value)
    {
        if(value && !_markerView.IsMarkerConsumed)
        {
            return;
        }
        _turnEndButton.enabled = value;
    }

    public void EnableCampButton(bool value)
    {
        _campToggleButton.enabled = value;
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
            return (_tableView as PlayerTableView).GetPrimaryCardHolderByTag(hitArea.tag);
        }
        else
        {
            return (_tableView as PlayerTableView).GetLastSecondaryCardHolder();
        }
    }

    public override void UpdateCardHolders(HolderSubType subType, string hitAreaTag)
    {
        if(subType == HolderSubType.PRIMARY)
        {
            if (string.IsNullOrEmpty(hitAreaTag))
            {
                (_tableView as PlayerTableView).RemoveEmptyHolder(HolderSubType.PRIMARY);
            }
            else
            {
                (_tableView as PlayerTableView).AddNewPrimaryHolder(hitAreaTag);
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
                (_tableView as PlayerTableView).AddNewSecondaryHolder();
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
        return CreateAdjacentIconPairs(_tableView.GetTopPrimaryIcons());
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

    public void PlaceInitialCardOnTableHandler(GameTask task, Card card)
    {
        switch(task.State)
        {
            case 0:
                (_handView as PlayerHandView).EnableCardsRaycast(false);
                ToggleTable(true);
                task.StartDelayMs(2000);
                break;
            case 1:
                UpdateCardHolders(HolderSubType.PRIMARY, "RectLeft");
                CardHolder holder = (_tableView as PlayerTableView).GetPrimaryCardHolderByID(0);
                ExecuteCardPlacement(new object[] { card.Data.ID, false, holder.Data, card });
                task.StartHandler((Action<GameTask, CardHolder, Card>)SnapCardHandler, holder, card);
                break;
            case 2:
                task.StartDelayMs(1000);
                break;
            case 3:
                task.StartHandler((Action<GameTask>)ApplyCardPlacementHandler);
                break;
            default:
                ToggleTable(true);
                (_handView as PlayerHandView).EnableCardsRaycast(true);
                task.Complete();
                break;
        }
    }

    public override void ExecuteCardPlacement(object[] args)
    {
        PendingActionFunction[] actionFunctions = new PendingActionFunction[] {
            _infoView.UpdateNumberOfCardPlacementsAction,
            _infoView.UpdateRoadTokensAction,
            (_handView as PlayerHandView).PlaceCardFromHandAction,
            (_tableView as PlayerTableView).RegisterCardPlacementAction,
            (_tableView as PlayerTableView).AdjustHolderVerticallyAction,
            (_tableView as PlayerTableView).UpdateHitAreaSizeAction,
            (_tableView as PlayerTableView).UpdateHolderIconsAction
        };
        PendingActionFunction[] cancelledActionFunctions = new PendingActionFunction[] {
            _infoView.UpdateNumberOfCardPlacementsAction,
            _infoView.UpdateRoadTokensAction,
            (_tableView as PlayerTableView).UpdateHitAreaSizeAction,
            (_tableView as PlayerTableView).AdjustHolderVerticallyAction,
            (_tableView as PlayerTableView).RegisterCardPlacementAction,
            (_handView as PlayerHandView).PlaceCardFromHandAction,
            RemoveHolderAction,
            (_tableView as PlayerTableView).UpdateHolderIconsAction
        };
        _pendingPlacementActionCreator.Create(actionFunctions, cancelledActionFunctions, args);
    }

    public void CancelCardPlacement(GameTask task, Card card)
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

    public void ApplyCardPlacementHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                List<object[]> dataCollection = _pendingPlacementActionCreator.GetDataCollection();
                List<Card> primaryTableCards = new();
                for (int i = 0; i < dataCollection.Count; i++)
                {
                    HolderData holderData = (HolderData)dataCollection[i][2];
                    Card card = (Card)dataCollection[i][3];
                    if (holderData.holderSubType == HolderSubType.PRIMARY && holderData.IsTopCardOfHolder(card)) // filter top cards of primary table holders
                    {
                        primaryTableCards.Add(card);
                    }
                    card.cardStatus = CardStatus.USED;
                }
                task.StartHandler(GetUpdateDisplayIconsHandler(), primaryTableCards, _tableView.ActiveState.PrimaryCardHolderDataCollection);
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

    private void RemoveHolderAction(object[] args)
    {
        HolderData holderData = (HolderData)args[2];
        Card card = (Card)args[3];
        if (card.Data.cardType == CardType.Ground)
        {
            UpdateCardHolders(holderData.holderSubType, null);
        }
        else if (card.Data.cardType == CardType.Landscape)
        {
            UpdateCardHolders(holderData.holderSubType, null);
        }
    }

    private List<Card> GetPlacedCardsWithScore()
    {
        List<object[]> dataCollection = _pendingPlacementActionCreator.GetDataCollection();
        List<Card> primaryTableCards = dataCollection
            .Select(data => (Card)data[3])
            .Where(card => card.transform.parent.GetComponent<CardHolder>().Data.holderSubType == HolderSubType.PRIMARY && card.Data.cardType != CardType.Ground)
            .OrderBy(card => card.transform.parent.GetSiblingIndex())
            .ToList();

        List<Card> secondaryTableCards = dataCollection
            .Select(data => (Card)data[3])
            .Where(card => card.transform.parent.GetComponent<CardHolder>().Data.holderSubType == HolderSubType.SECONDARY)
            .OrderBy(card => card.transform.parent.GetSiblingIndex())
            .ToList();
        primaryTableCards.AddRange(secondaryTableCards); // collect score of primary table first
        return primaryTableCards;
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
        _handScreenHitArea.EnableFakeCard(isToggled ? dataCollection.Last() : null);
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
