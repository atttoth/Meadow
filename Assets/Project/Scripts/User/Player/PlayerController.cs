using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using static PendingActionCreator;

public class PlayerController : ControllerBase<PlayerTableView, PlayerHandView, PlayerMarkerView, PlayerInfoView>
{
    private PendingActionCreator _pendingActionCreator;
    private Button _tableToggleButton;
    private Button _tableApproveButton;
    private Button _tablePagerButton;
    private Button _turnEndButton;
    private Button _campToggleButton;
    private Dictionary<int, CardIcon[][]> _allIconsOfHoldersInOrder; //as cards are stacked in order
    private List<int> _campScoreTokens;
    private bool _isCampVisible;

    public override void Init(PlayerTableView tableView, PlayerHandView handView, PlayerMarkerView markerView, PlayerInfoView infoView)
    {
        base.Init(tableView, handView, markerView, infoView);
        _tableView.Init();
        _handView.Init();
        _markerView.Init();
        _infoView.Init();
        _pendingActionCreator = new PendingActionCreator();

        _tableToggleButton = _tableView.transform.GetChild(3).GetComponent<Button>();
        _tableApproveButton = _tableView.transform.GetChild(2).GetComponent<Button>();
        _tablePagerButton = _tableView.transform.GetChild(1).GetComponent<Button>();

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
        _campToggleButton = infoView.transform.GetChild(3).GetComponent<Button>();

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
        _tablePagerButton.onClick.AddListener(() => _tableView.SwitchTableContent());
        _turnEndButton.onClick.AddListener(() => Debug.Log("turn ended"));
        _campToggleButton.onClick.AddListener(() => 
        {
            _isCampVisible = !_isCampVisible;
            StartEventHandler(GameLogicEventType.CAMP_TOGGLED, new GameTaskItemData() { value = _isCampVisible });
            Transform parent = _isCampVisible ? transform.root : infoView.transform;
            _campToggleButton.transform.SetParent(parent); // place button above camp view in the hierarchy
        });

        _allIconsOfHoldersInOrder = new();
    }

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

    public bool HasHolder(int ID)
    {
        return _allIconsOfHoldersInOrder.ContainsKey(ID);
    }

    public List<CardIcon> GetAllCurrentIcons()
    {
        List<CardIcon> allCurrentIcons = new();
        foreach (CardIcon[][] items in _allIconsOfHoldersInOrder.Values)
        {
            allCurrentIcons.AddRange(items[^1]);
            if (items.Length > 1)
            {
                allCurrentIcons.AddRange(items[0]);
            }
        }
        return allCurrentIcons;
    }

    private List<CardIcon[]> GetTopIcons() // sorted holders (left to right)
    {
        List<CardIcon[]> topIcons = new();
        _allIconsOfHoldersInOrder
            .OrderBy(e => _tableView.GetActiveCardHolderByID(e.Key).transform.GetSiblingIndex())
            .Select(e => e.Value)
            .ToList()
            .ForEach(values =>
            {
                CardIcon[] icons = values[^1].Where(icon => (int)icon > 4).ToArray();
                topIcons.Add(icons);
            });
        
        return topIcons;
    }

    public PlayerTableView GetTableView()
    {
        return _tableView;
    }

    public PlayerHandView GetHandView()
    {
        return _handView;
    }

    public bool IsTableVisible()
    {
        return _tableView.isTableVisible;
    }

    public CardHolder GetLatestTableCardHolderByTag(string tag)
    {
        int index = tag == "RectLeft" ? 0 : _tableView.GetActiveCardHoldersAmount() - 1;
        return _tableView.GetActiveCardHolderByIndex(index);
    }

    public void UpdateTableCardUI(string tag = null)
    {
        if (string.IsNullOrEmpty(tag))
        {
            _tableView.RemoveHolder();
        }
        else
        {
            _tableView.AddHolder(tag);
        }
        _tableView.CenterCardHolders();
    }

    public void EnableTableView(bool value)
    {
        _tableToggleButton.interactable = value;
    }

    public List<List<CardIcon>> GetAdjacentIconPairs() 
    {
        List<List<CardIcon>> pairs = new();
        List<CardIcon[]> topIcons = GetTopIcons();
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

    private void UpdateCurrentIconsOfHolder(GameTaskItemData data)
    {
        int ID = data.holder.ID;
        CardIcon[][] items = _allIconsOfHoldersInOrder[ID];
        _allIconsOfHoldersInOrder.Remove(ID);

        List<CardIcon[]> updatedItems = new();
        foreach (CardIcon[] item in items)
        {
            updatedItems.Add(item);
        }
        updatedItems.Add(data.card.Data.icons);

        _allIconsOfHoldersInOrder.Add(ID, updatedItems.ToArray());
    }

    private void UpdateCurrentIconsOfHolderRewind(GameTaskItemData data)
    {
        int ID = data.holder.ID;
        CardIcon[][] items = _allIconsOfHoldersInOrder[ID];
        _allIconsOfHoldersInOrder.Remove(ID);

        if (data.card.Data.cardType != CardType.Ground)
        {
            List<CardIcon[]> updatedItems = new();
            for (int i = 0; i < items.Length - 1; i++)
            {
                updatedItems.Add(items[i]);
            }
            _allIconsOfHoldersInOrder.Add(ID, updatedItems.ToArray());
        }
    }

    public void CreateEntryForCurrentIcons(GameTaskItemData data)
    {
        if (data.card.Data.cardType != CardType.Ground || _allIconsOfHoldersInOrder.Count == 10)
        {
            return;
        }
        int ID = data.holder.ID;
        _allIconsOfHoldersInOrder.Add(ID, new CardIcon[][] { });
    }

    public void CreateEntryForCurrentIconsRewind(GameTaskItemData data)
    {
        if (data.card.Data.cardType != CardType.Ground)
        {
            return;
        }
        _allIconsOfHoldersInOrder.Remove(data.holder.ID);
        UpdateTableCardUI();
    }

    public void CreatePendingCardPlacement(GameTaskItemData data)
    {
        _tableToggleButton.enabled = false;
        data.targetTransform = _handView.transform;
        PendingActionItem[] postActionItems = new PendingActionItem[] {
            _infoView.IncrementNumberOfCardPlacements,
            _handView.RemoveCardFromHand,
            _tableView.StackCard,
            _tableView.ExpandHolderVertically,
            _tableView.UpdateUIHitAreaSize,
            CreateEntryForCurrentIcons,
            UpdateCurrentIconsOfHolder
        };
        PendingActionItem[] prevActionItems = new PendingActionItem[] {
            UpdateCurrentIconsOfHolderRewind,
            _tableView.UpdateUIHitAreaSizeRewind,
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

    public Transform GetScoreTransform()
    {
        return _infoView.scoreTransform;
    }

    public List<Card> GetPlacedCards()
    {
        return _pendingActionCreator.GetDataCollection()
                    .Select(data => data.card)
                    .Where(card => card.Data.cardType != CardType.Ground)
                    .OrderBy(card => card.transform.parent.GetSiblingIndex())
                    .ToList();
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
                List<GameTaskItemData> dataCollection = _pendingActionCreator.GetDataCollection();
                dataCollection
                    .Select(data => data.card)
                    .Where(card => card.cardActionStatus == CardActionStatus.PENDING_ON_TABLE)
                    .OrderBy(card => card.transform.parent.GetSiblingIndex())
                    .ToList()
                    .ForEach(card => _tableView.PrepareDisplayIcon(card));

                dataCollection.ForEach(data => data.card.cardActionStatus = CardActionStatus.DEFAULT);
                task.StartDelayMs(0);
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
