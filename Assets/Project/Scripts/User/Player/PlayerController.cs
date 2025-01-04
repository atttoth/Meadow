using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using static PendingActionCreator;
using UnityEngine.U2D;

public class PlayerController : ControllerBase<PlayerTableView, PlayerHandView, PlayerMarkerView, PlayerInfoView>
{
    private PendingActionCreator _pendingActionCreator;
    private Button _tableToggleButton;
    private Button _tableApproveButton;
    private Button _tablePagerButton;
    private Button _turnEndButton;
    private Dictionary<int, CardIcon[][]> _allIconsOfHoldersInOrder; //as cards are stacked in order

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
        
        _tableToggleButton.onClick.AddListener(() =>
        {
            _tableView.TogglePanel();
            _handView.ToggleHand();
        });
        _tableApproveButton.onClick.AddListener(() =>
        {
            if (_pendingActionCreator.GetNumOfActions() > 0)
            {
                _tableApproveButton.enabled = false;
                _tableView.UpdateApproveButton(false);
                StartEventHandler(GameEventType.APPROVED_PENDING_CARD_PLACED, null);
            }
            else
            {
                _tableView.TogglePanel();
                _handView.ToggleHand();
            }
        });
        _tablePagerButton.onClick.AddListener(() => _tableView.SwitchTableContent());
        _turnEndButton.onClick.AddListener(() => Debug.Log("turn ended"));

        _allIconsOfHoldersInOrder = new();
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

    public void UpdateHandViewHandler(GameTask task)
    {
        switch(task.State)
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
        switch(task.State)
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
        data.handTransform = _handView.transform;
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

    public void UpdateScoreHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                List<Card> cards = _pendingActionCreator.GetDataCollection()
                    .Select(data => data.card)
                    .Where(card => card.Data.cardType != CardType.Ground)
                    .OrderBy(card => card.transform.parent.GetSiblingIndex())
                    .ToList();

                float cardScoreDelay = ReferenceManager.Instance.gameLogicManager.GameSettings.cardScoreDelay;
                float cardScoreCollectingSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardScoreCollectingSpeed;
                int duration = (int)(((cards.Count - 1) * cardScoreDelay + cardScoreCollectingSpeed) * 1000);
                int i = 0;
                while (cards.Count > 0)
                {
                    float delay = i * cardScoreDelay;
                    Card card = cards.First();
                    cards.RemoveAt(0);
                    _infoView.CollectScoreOfCard(delay, card);
                    i++;
                }
                task.StartDelayMs(duration);
                break;
            default:
                task.Complete();
                break;
        }
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
                int duration1 = _tableView.SetDisplayIconsHorizontalPosition();
                task.StartDelayMs(duration1);
                break;
            case 1:
                int duration2 = _tableView.ChangeDisplayIcons();
                task.StartDelayMs(duration2);
                break;
            case 2:
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
        _tableView.TogglePanel();
        _handView.ToggleHand();
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
