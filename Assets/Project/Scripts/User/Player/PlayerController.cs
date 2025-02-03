using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using UnityEngine.WSA;
using static PendingActionCreator;

public class PlayerController : UserController<PlayerTableView>
{
    private PlayerHandView _handView;
    private PlayerMarkerView _markerView;
    private PendingActionCreator _pendingActionCreator;
    private Button _tableToggleButton;
    private Button _tableApproveButton;
    private Button _turnEndButton;
    private Button _campToggleButton;
    private List<int> _campScoreTokens;
    private bool _isCampVisible;
    public CardType draggingCardType;

    public void CreatePlayer()
    {
        _tableView = transform.GetChild(1).GetComponent<PlayerTableView>();
        _infoView = _tableView.transform.GetChild(3).GetComponent<InfoView>();
        _handView = transform.GetChild(2).GetComponent<PlayerHandView>();
        _markerView = transform.GetChild(3).GetComponent<PlayerMarkerView>();
        _tableView.Init();
        _infoView.Init();
        _handView.Init();
        _markerView.Init();
        _pendingActionCreator = new PendingActionCreator();

        _tableToggleButton = _tableView.transform.GetChild(2).GetComponent<Button>();
        _tableApproveButton = _tableView.transform.GetChild(1).GetComponent<Button>();

        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        Transform turnEndButtonTransform = transform.GetChild(0);
        turnEndButtonTransform.GetComponent<Image>().sprite = atlas.GetSprite("endTurn_base");
        _turnEndButton = turnEndButtonTransform.GetComponent<Button>();
        SpriteState spriteState = _turnEndButton.spriteState;
        spriteState.selectedSprite = atlas.GetSprite("endTurn_base");
        spriteState.highlightedSprite = atlas.GetSprite("endTurn_highlighted");
        spriteState.pressedSprite = atlas.GetSprite("endTurn_highlighted");
        spriteState.disabledSprite = atlas.GetSprite("endTurn_disabled");
        _turnEndButton.spriteState = spriteState;
        _campToggleButton = _infoView.transform.GetChild(3).GetComponent<Button>();

        _tableToggleButton.onClick.AddListener(() => ToggleTable());
        _tableApproveButton.onClick.AddListener(() =>
        {
            if (_pendingActionCreator.GetNumOfActions() > 0)
            {
                _tableApproveButton.enabled = false;
                _tableView.UpdateApproveButton(false);
                StartEventHandler(GameLogicEventType.APPROVED_PENDING_CARD_PLACED, null);
            }
            else
            {
                ToggleTable();
            }
        });
        _turnEndButton.onClick.AddListener(() => Debug.Log("turn ended"));
        _campToggleButton.onClick.AddListener(() => 
        {
            _isCampVisible = !_isCampVisible;
            StartEventHandler(GameLogicEventType.CAMP_TOGGLED, new GameTaskItemData() { value = _isCampVisible });
            Transform parent = _isCampVisible ? transform.root : _infoView.transform;
            _campToggleButton.transform.SetParent(parent); // place button above camp view in the hierarchy
        });

        _allIconsOfPrimaryHoldersInOrder = new();
        _allIconsOfSecondaryHoldersInOrder = new();
        draggingCardType = CardType.None;
    }

    public PlayerTableView TableView { get { return _tableView; } }

    public PlayerHandView HandView { get { return _handView; } }

    private void ToggleTable()
    {
        _tableView.TogglePanel();
        _handView.ToggleHand();
        _markerView.Fade(_tableView.isTableVisible);
        FadeTurnEndButton(_tableView.isTableVisible);
        StartEventHandler(GameLogicEventType.TABLE_TOGGLED, new GameTaskItemData() { value = _tableView.isTableVisible });
    }

    private void FadeTurnEndButton(bool value)
    {
        float fadeDuration = ReferenceManager.Instance.gameLogicManager.GameSettings.gameUIFadeDuration;
        float targetValue = value ? 0f : 1f;
        DOTween.Sequence().Append(_turnEndButton.GetComponent<Image>().DOFade(targetValue, fadeDuration));
    }

    public void ResetCampScoreTokens()
    {
        _campScoreTokens = new() { 2, 3, 4 };
    }

    public int GetNextCampScoreToken()
    {
        return _campScoreTokens.Count > 0 ? _campScoreTokens.First() : 0;
    }

    public void UpdateCampScoreTokens()
    {
        _campScoreTokens.RemoveAt(0);
    }

    private List<CardIcon[]> GetTopPrimaryIcons() // sorted primary holders (left to right)
    {
        List<CardIcon[]> topIcons = new();
        _allIconsOfPrimaryHoldersInOrder
            .OrderBy(e => _tableView.GetActivePrimaryCardHolderByID(e.Key).transform.GetSiblingIndex())
            .Select(e => e.Value)
            .ToList()
            .ForEach(values =>
            {
                CardIcon[] icons = values[^1].Where(icon => (int)icon > 4).ToArray();
                topIcons.Add(icons);
            });
        
        return topIcons;
    }

    public bool IsTableVisible()
    {
        return _tableView.isTableVisible;
    }

    public void ToggleHitArea(CardType cardType)
    {
        draggingCardType = cardType;
        if (cardType == CardType.Ground)
        {
            _tableView.TogglePrimaryHitAreas(true);
        }
        else if (cardType == CardType.Landscape)
        {
            _tableView.ToggleSecondaryHitArea(true);
        }
    }

    public CardHolder GetTableCardHolderOfHitArea(TableCardHitArea hitArea)
    {
        if(hitArea.type == HolderSubType.PRIMARY)
        {
            return _tableView.GetActivePrimaryCardHolderByTag(hitArea.tag);
        }
        else
        {
            return _tableView.GetActiveSecondaryCardHolder();
        }
    }

    public void UpdateActiveCardHolders(HolderSubType subType, string hitAreaTag)
    {
        if(subType == HolderSubType.PRIMARY)
        {
            if (string.IsNullOrEmpty(hitAreaTag))
            {
                _tableView.RemoveEmptyHolder(HolderSubType.PRIMARY);
            }
            else
            {
                _tableView.AddEmptyPrimaryHolder(hitAreaTag);
            }
            _tableView.CenterPrimaryCardHolders();
        }
        else if(subType == HolderSubType.SECONDARY)
        {
            if (string.IsNullOrEmpty(hitAreaTag))
            {
                _tableView.RemoveEmptyHolder(HolderSubType.SECONDARY);
                _tableView.AlignSecondaryCardHoldersToLeft();
            }
            else
            {
                _tableView.AddEmptySecondaryHolder();
            }
        }
    }

    public void EnableTableView(bool value)
    {
        _tableToggleButton.interactable = value;
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

    public void UpdateHandViewHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _handView.MoveCardsHorizontallyInHand(IsTableVisible(), false, true);
                task.StartDelayMs(500);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void AddExtraCardPlacementHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _infoView.SetMaxCardPlacement(2);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void AddRoadTokensHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                _infoView.AddRoadTokens(2);
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void AddCardToHandHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                task.StartHandler(_handView.AddCardHandler, task.Data);
                break;
            default:
                _handView.SetCardsReady();
                task.Complete();
                break;
        }
    }

    private void UpdateCurrentIconsOfHolder(GameTaskItemData data)
    {
        CardHolder holder = (CardHolder)data.holder;
        Dictionary<int, CardIcon[][]> collection = holder.holderSubType == HolderSubType.PRIMARY ? _allIconsOfPrimaryHoldersInOrder : _allIconsOfSecondaryHoldersInOrder;
        int ID = holder.ID;
        CardIcon[][] items = collection[ID];
        collection.Remove(ID);

        List<CardIcon[]> updatedItems = new();
        foreach (CardIcon[] item in items)
        {
            updatedItems.Add(item);
        }
        updatedItems.Add(data.card.Data.icons);

        collection.Add(ID, updatedItems.ToArray());
    }

    private void UpdateCurrentIconsOfHolderRewind(GameTaskItemData data)
    {
        CardHolder holder = (CardHolder)data.holder;
        Dictionary<int, CardIcon[][]> collection = holder.holderSubType == HolderSubType.PRIMARY ? _allIconsOfPrimaryHoldersInOrder : _allIconsOfSecondaryHoldersInOrder;
        int ID = holder.ID;
        CardIcon[][] items = collection[ID];
        collection.Remove(ID);
        if(!Array.Exists(new[] { CardType.Ground, CardType.Landscape }, cardType => cardType == data.card.Data.cardType))
        {
            List<CardIcon[]> updatedItems = new();
            for (int i = 0; i < items.Length - 1; i++)
            {
                updatedItems.Add(items[i]);
            }
            collection.Add(ID, updatedItems.ToArray());
        }
    }

    public void CreateEntryForCurrentIcons(GameTaskItemData data)
    {
        if (data.card.Data.cardType == CardType.Ground)
        {
            _allIconsOfPrimaryHoldersInOrder.Add(data.holder.ID, new CardIcon[][] { });
        }
        else if(data.card.Data.cardType == CardType.Landscape)
        {
            _allIconsOfSecondaryHoldersInOrder.Add(data.holder.ID, new CardIcon[][] { });
        }
    }

    public void CreateEntryForCurrentIconsRewind(GameTaskItemData data)
    {
        if (data.card.Data.cardType == CardType.Ground)
        {
            _allIconsOfPrimaryHoldersInOrder.Remove(data.holder.ID);
            CardHolder holder = (CardHolder)data.holder;
            UpdateActiveCardHolders(holder.holderSubType, null);
        }
        else if(data.card.Data.cardType == CardType.Landscape)
        {
            _allIconsOfSecondaryHoldersInOrder.Remove(data.holder.ID);
            CardHolder holder = (CardHolder)data.holder;
            UpdateActiveCardHolders(holder.holderSubType, null);
        }
    }

    public void CreatePendingCardPlacement(GameTaskItemData data)
    {
        _tableToggleButton.enabled = false;
        PendingActionItem[] postActionItems = new PendingActionItem[] {
            _infoView.IncrementNumberOfCardPlacements,
            _handView.RemoveCardFromHand,
            _tableView.StackCard,
            _tableView.ExpandHolderVertically,
            _tableView.UpdatePrimaryHitAreaSize,
            CreateEntryForCurrentIcons,
            UpdateCurrentIconsOfHolder
        };
        PendingActionItem[] prevActionItems = new PendingActionItem[] {
            UpdateCurrentIconsOfHolderRewind,
            _tableView.UpdatePrimaryHitAreaSizeRewind,
            _tableView.ExpandHolderVerticallyRewind,
            _tableView.StackCardRewind,
            CreateEntryForCurrentIconsRewind,
            _handView.RemoveCardFromHandRewind,
            _infoView.DecrementNumberOfCardPlacements
        };
        _tableView.UpdateApproveButton(true);
        _pendingActionCreator.Create(postActionItems, prevActionItems, data);
    }

    public void CancelPendingCardPlacement(GameTaskItemData data)
    {
        _pendingActionCreator.Cancel(data);
        if (_pendingActionCreator.GetNumOfActions() == 0)
        {
            _tableView.UpdateApproveButton(false);
            _tableToggleButton.enabled = true;
        }
    }

    public void SnapCardHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                CardHolder holder = (CardHolder)task.Data.holder;
                float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardPlacementSpeed;
                Transform handTransform = task.Data.value ? null : _handView.transform;
                int contentCount = task.Data.value ? holder.GetContentListSize() - 1 : -1;
                _tableView.PositionTableCard(task.Data.card, contentCount, speed, handTransform);
                task.StartDelayMs((int)(speed * 1000));
                break;
            default:
                task.Complete();
                break;
        }
    }

    public Transform GetScoreTransform()
    {
        return _infoView.scoreTransform;
    }

    public List<Card> GetPlacedCardsWithScore()
    {
        List<GameTaskItemData> dataCollection = _pendingActionCreator.GetDataCollection();
        List<Card> primaryTableCards = dataCollection
            .Select(data => data.card)
            .Where(card => card.transform.parent.GetComponent<CardHolder>().holderSubType == HolderSubType.PRIMARY && card.Data.cardType != CardType.Ground)
            .OrderBy(card => card.transform.parent.GetSiblingIndex())
            .ToList();

        List<Card> secondaryTableCards = dataCollection
            .Select(data => data.card)
            .Where(card => card.transform.parent.GetComponent<CardHolder>().holderSubType == HolderSubType.SECONDARY)
            .OrderBy(card => card.transform.parent.GetSiblingIndex())
            .ToList();
        primaryTableCards.AddRange(secondaryTableCards); // collect score of primary table first
        return primaryTableCards;
    }

    public void UpdateScore(int score)
    {
        _infoView.RegisterScore(score);
    }
        
    public void UpdateDisplayIconsHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                List<Card> allPlacedCards = _pendingActionCreator.GetDataCollection()
                    .Select(data => data.card)
                    .Where(card => card.cardStatus == CardStatus.PENDING_ON_TABLE)
                    .ToList();

                allPlacedCards.ForEach(card => card.cardStatus = CardStatus.USED);
                List<Card> primaryTableCards = allPlacedCards.Where(card => card.transform.parent.GetComponent<CardHolder>().holderSubType == HolderSubType.PRIMARY).ToList();
                
                if(primaryTableCards.Count > 0)
                {
                    primaryTableCards
                    .OrderBy(card => card.transform.parent.GetSiblingIndex())
                    .ToList()
                    .ForEach(card => _tableView.PrepareDisplayIcon(card));
                    task.StartDelayMs(0);
                }
                else
                {
                    task.Complete();
                }
                break;
            case 1:
                task.StartHandler(_tableView.SetDisplayIconsHorizontalPositionHandler);
                break;
            case 2:
                task.StartHandler(_tableView.ChangeDisplayIconsHandler);
                break;
            case 3:
                _tableView.ReOrderDisplayIconsHierarchy();
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void ApplyPendingCardPlacement()
    {
        _pendingActionCreator.Dispose();
        _tableToggleButton.enabled = true;
        _tableApproveButton.enabled = true;
        ToggleTable();
    }

    public List<Marker> GetRemainingMarkers()
    {
        return _markerView.GetRemainingMarkers();
    }

    public void ShowSelectedMarker(int value, List<Marker> markers)
    {
        Marker currentMarker = _markerView.GetCurrentMarker(value);
        markers.ForEach(marker => marker.gameObject.SetActive(marker == currentMarker));
    }

    public void SetMarkerUsed()
    {
        _markerView.SetPlacedMarkerToUsed();
    }

    public void ResetMarkers()
    {
        _markerView.Reset();
    }
}
