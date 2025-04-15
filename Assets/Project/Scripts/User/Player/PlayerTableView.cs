using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTableView : TableView
{
    // Primary - ground and observation cards
    private List<CardHolder> _primaryCardHolderPool;
    private Transform _primaryCardHolderPoolContainer;
    private List<TableCardHitArea> _primaryHitAreas; // left and right sides

    // Secondary - landscape and discovery cards
    private List<CardHolder> _secondaryCardHolderPool;
    private Transform _secondaryCardHolderPoolContainer;
    private TableCardHitArea _secondaryHitArea; // right side

    private TextMeshProUGUI _approveButtonText;
    private Image _approveButtonImage;
    private TableLayout _tableLayout;
    public bool isTableVisible;

    public override void Init()
    {
        _primaryTableContentScroll = transform.GetChild(0).GetChild(0).GetComponent<ScrollRect>();
        _primaryCardHolderPoolContainer = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1); // .../TableContents/Primary/Content/CardHolderPool
        _primaryCardHolderContainer = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0); // .../TableContents/PrimaryPage/Content/CardHolders
        _activePrimaryCardHolders = new();

        _secondaryCardHolderPoolContainer = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1); // .../TableContents/Secondary/Content/CardHolderPool
        _secondaryCardHolderContainer = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0); // .../TableContents/SecondaryPage/Content/CardHolders
        _activeSecondaryCardHolders = new();

        _approveButtonText = transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>();
        _approveButtonImage = transform.GetChild(1).GetComponent<Image>();

        CreateCardHolderPools();
        CreateUIHitAreas();
        CreateTableLayout();
        isTableVisible = false;
    }

    private void CreateTableLayout()
    {
        Rect rect = transform.GetComponent<RectTransform>().rect;
        float tableClosedPosY = transform.position.y;
        float tableOpenPosY = tableClosedPosY + rect.height;
        float primaryHolderWidth = GameAssets.Instance.tablePrimaryCardHolderPrefab.GetComponent<RectTransform>().rect.width;
        float secondaryHolderWidth = GameAssets.Instance.tableSecondaryCardHolderPrefab.GetComponent<RectTransform>().rect.width;
        _tableLayout = new TableLayout(tableClosedPosY, tableOpenPosY, rect.width, primaryHolderWidth, secondaryHolderWidth);
    }

    public CardHolder GetActivePrimaryCardHolderByTag(string tagName)
    {
        int index = tagName == "RectLeft" ? 0 : _activePrimaryCardHolders.Count - 1;
        return _activePrimaryCardHolders[index];
    }

    public CardHolder GetActiveSecondaryCardHolder()
    {
        return _activeSecondaryCardHolders[_activeSecondaryCardHolders.Count - 1];
    }

    public CardHolder GetActivePrimaryCardHolderByID(int ID)
    {
        return _activePrimaryCardHolders.Find(holder => holder.ID == ID);
    }

    public void AddEmptyPrimaryHolder(string tag)
    {
        int listIndex = tag == "RectLeft" ? 0 : _activePrimaryCardHolders.Count;
        CardHolder holder = _primaryCardHolderPool[0];
        holder.Init(-1, HolderType.TableCard);
        holder.holderSubType = HolderSubType.PRIMARY;
        holder.transform.SetParent(_primaryCardHolderContainer);
        holder.transform.SetSiblingIndex(listIndex);
        holder.gameObject.SetActive(true);
        RectTransform rect = holder.GetComponent<RectTransform>();
        RectTransform prevHolderTransform = null;
        if (_activePrimaryCardHolders.Count > 0)
        {
            int prevHolderIndex = _activePrimaryCardHolders.Count == 1 ? 0 : tag == "RectLeft" ? 0 : _activePrimaryCardHolders.Count - 1;
            prevHolderTransform = _activePrimaryCardHolders[prevHolderIndex].GetComponent<RectTransform>();
        }
        rect.anchoredPosition = _tableLayout.GetPrimaryHolderPosition(prevHolderTransform, tag == "RectLeft" ? -1 : 1, rect.anchoredPosition.y);
        _primaryCardHolderPool.Remove(holder);
        _activePrimaryCardHolders.Insert(listIndex, holder);
    }

    public void AddEmptySecondaryHolder()
    {
        CardHolder holder = _secondaryCardHolderPool[0];
        holder.Init(-1, HolderType.TableCard);
        holder.holderSubType = HolderSubType.SECONDARY;
        holder.transform.SetParent(_secondaryCardHolderContainer);
        holder.transform.SetSiblingIndex(_activeSecondaryCardHolders.Count);
        holder.gameObject.SetActive(true);
        RectTransform rect = holder.GetComponent<RectTransform>();
        rect.anchoredPosition = _tableLayout.GetSecondaryCardHolderPosition(_activeSecondaryCardHolders.Count, rect.anchoredPosition.y);
        _secondaryCardHolderPool.Remove(holder);
        _activeSecondaryCardHolders.Add(holder);
    }

    private void CreateCardHolderPools()
    {
        _primaryCardHolderPool = new();
        for (int i = 0; i < _MAX_PRIMARY_HOLDER_NUM; i++)
        {
            CardHolder holder = Instantiate(GameAssets.Instance.tablePrimaryCardHolderPrefab, _primaryCardHolderPoolContainer).GetComponent<CardHolder>();
            holder.ID = i;
            holder.holderType = HolderType.TableCard;
            holder.gameObject.SetActive(false);
            _primaryCardHolderPool.Add(holder);
        }

        _secondaryCardHolderPool = new();
        for (int i = 0; i < _MAX_SECONDARY_HOLDER_NUM; i++)
        {
            CardHolder holder = Instantiate(GameAssets.Instance.tableSecondaryCardHolderPrefab, _secondaryCardHolderPoolContainer).GetComponent<CardHolder>();
            holder.ID = _MAX_PRIMARY_HOLDER_NUM + i;
            holder.holderType = HolderType.TableCard;
            holder.gameObject.SetActive(false);
            _secondaryCardHolderPool.Add(holder);
        }
    }

    private void CreateUIHitAreas()
    {
        _primaryHitAreas = new();
        for (int i = 0; i < 2; i++)
        {
            TableCardHitArea primaryHitArea = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).GetChild(i).GetComponent<TableCardHitArea>(); // .../TableContents/Primary/Content/UIHitArea/Hitarea
            primaryHitArea.type = HolderSubType.PRIMARY;
            primaryHitArea.Toggle(false);
            _primaryHitAreas.Add(primaryHitArea);
        }

        _secondaryHitArea = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(2).GetComponent<TableCardHitArea>(); // .../TableContents/Secondary/Content/Hitarea
        _secondaryHitArea.type = HolderSubType.SECONDARY;
        _secondaryHitArea.Toggle(false);
    }

    public void UpdateHitAreaSizeAction(object[] args)
    {
        bool isActionCancelled = (bool)args[1];
        Card card = (Card)args[3];
        CardType cardType = card.Data.cardType;
        if(isActionCancelled)
        {
            if (cardType == CardType.Ground)
            {
                CalculatePrimaryHitAreaSizeAndPosition(cardType, -1);
            }
            else if (cardType == CardType.Landscape)
            {
                CalculateSecondaryHitAreaSizeAndPosition(cardType, -1);
            }
        }
        else
        {
            if (cardType == CardType.Ground && _activePrimaryCardHolders.Count < _MAX_PRIMARY_HOLDER_NUM)
            {
                CalculatePrimaryHitAreaSizeAndPosition(cardType, 1);
            }
            else if (cardType == CardType.Landscape && _activeSecondaryCardHolders.Count < _MAX_SECONDARY_HOLDER_NUM)
            {
                CalculateSecondaryHitAreaSizeAndPosition(cardType, 1);
            }
        }
    }

    private void CalculatePrimaryHitAreaSizeAndPosition(CardType cardType, int status)
    {
        _primaryHitAreas.ForEach(hitArea =>
        {
            RectTransform rect = hitArea.GetComponent<RectTransform>();
            float prevWidth = rect.sizeDelta.x;
            rect.sizeDelta = _tableLayout.GetPrimaryHitAreaSize(_activePrimaryCardHolders.Count, status, rect.sizeDelta.y);
            float diff = prevWidth - rect.sizeDelta.x;
            rect.anchoredPosition = _tableLayout.GetHitAreaPosition(rect, diff);
        });
    }

    private void CalculateSecondaryHitAreaSizeAndPosition(CardType cardType, int status)
    {
        RectTransform rect = _secondaryHitArea.GetComponent<RectTransform>();
        float prevWidth = rect.sizeDelta.x;
        rect.sizeDelta = _tableLayout.GetSecondaryHitAreaSize(_activeSecondaryCardHolders.Count, status, rect.sizeDelta.y);
        float diff = prevWidth - rect.sizeDelta.x;
        rect.anchoredPosition = _tableLayout.GetHitAreaPosition(rect, diff);
    }

    public void TogglePrimaryHitAreas(bool value)
    {
        if(value && _activePrimaryCardHolders.Count == _MAX_PRIMARY_HOLDER_NUM) return;

        _primaryHitAreas.ForEach(hitArea => hitArea.Toggle(value));
    }

    public void ToggleSecondaryHitArea(bool value)
    {
        if (value && _activeSecondaryCardHolders.Count == _MAX_SECONDARY_HOLDER_NUM) return;

        _secondaryHitArea.Toggle(value);
    }

    public void TogglePanel()
    {
        float speed = ReferenceManager.Instance.gameLogicController.GameSettings.tableViewOpenSpeed;
        isTableVisible = !isTableVisible;
        float posY = _tableLayout.GetTargetTableViewPosY(isTableVisible);
        if(isTableVisible)
        {
            _primaryTableContentScroll.verticalNormalizedPosition = 0f;
        }
        transform.DOMoveY(posY, speed).SetEase(Ease.InOutExpo);
    }

    public void UpdateApproveButton(bool isPendingAction)
    {
        _approveButtonText.text = isPendingAction ? "OK" : "Close";
        _approveButtonImage.color = isPendingAction ? Color.green : Color.black;
    }

    public void AdjustHolderVerticallyAction(object[] args)
    {
        CardHolder holder = (CardHolder)args[2];
        if (holder.holderSubType == HolderSubType.PRIMARY)
        {
            bool isActionCancelled = (bool)args[1];
            RectTransform rect = holder.GetComponent<RectTransform>();
            holder.transform.position += _tableLayout.GetUpdatedPrimaryHolderPosition(isActionCancelled ? -1 : 1);
            rect.sizeDelta = _tableLayout.GetUpdatedPrimaryHolderSize(rect, isActionCancelled ? -1 : 1);
        }
    }

    public void RegisterCardPlacementAction(object[] args)
    {
        bool isActionCancelled = (bool)args[1];
        CardHolder holder = (CardHolder)args[2];
        Card card = (Card)args[3];
        if(isActionCancelled)
        {
            holder.RemoveItemFromContentList(card);
        }
        else
        {
            holder.AddItemToContentList(card);
        }
        card.canMove = isActionCancelled;
        card.cardStatus = isActionCancelled ? CardStatus.IN_HAND : CardStatus.PENDING_ON_TABLE;
    }

    public void PositionTableCard(CardHolder holder, Card card, float speed, float[] handCardPositions, Transform parentTransform)
    {
        bool isPlacement = card.cardStatus == CardStatus.PENDING_ON_TABLE;
        Vector2 position;
        if (isPlacement)
        {
            card.transform.SetParent(parentTransform);
            float[] holderPositions = holder.holderSubType == HolderSubType.PRIMARY 
                ? _tableLayout.GetPrimaryCardHolderPositions(_activePrimaryCardHolders.Count) 
                : _tableLayout.GetSecondaryCardHolderPositions(_activeSecondaryCardHolders.Count);
            position = _tableLayout.GetPlacedCardPosition(card.Data.cardType, holder.GetContentListSize() - 1, holderPositions[holder.transform.GetSiblingIndex()]);
        }
        else
        {
            card.transform.SetParent(parentTransform);
            float posX = handCardPositions.Length > 0 ? handCardPositions[^1] : 0f;
            position = _tableLayout.GetCancelledCardPosition(posX, card.hoverTargetY);
        }
        card.PositionCard(position, speed, isPlacement);
    }

    public void RemoveEmptyHolder(HolderSubType holderSubType)
    {
        List<CardHolder> activeCardHolders = holderSubType == HolderSubType.PRIMARY ? _activePrimaryCardHolders : _activeSecondaryCardHolders;
        List<CardHolder> cardHolderPool = holderSubType == HolderSubType.PRIMARY ? _primaryCardHolderPool : _secondaryCardHolderPool;
        Transform cardHolderPoolContainer = holderSubType == HolderSubType.PRIMARY ? _primaryCardHolderPoolContainer : _secondaryCardHolderPoolContainer;
        for (int i = 0; i < activeCardHolders.Count; i++)
        {
            CardHolder holder = activeCardHolders[i];
            if (holder.IsEmpty())
            {
                holder.transform.SetParent(cardHolderPoolContainer);
                holder.gameObject.SetActive(false);
                activeCardHolders.Remove(holder);
                cardHolderPool.Add(holder);
                break;
            }
        }
    }

    public void AlignPrimaryCardHoldersToCenter()
    {
        if(_activePrimaryCardHolders.Count > 0)
        {
            float speed = ReferenceManager.Instance.gameLogicController.GameSettings.tableHolderCenteringSpeed;
            float[] positions = _tableLayout.GetPrimaryCardHolderPositions(_activePrimaryCardHolders.Count);
            for (int i = 0; i < positions.Length; i++)
            {
                Transform holderTransform = _activePrimaryCardHolders[i].transform;
                float posX = positions[i];
                holderTransform.DOLocalMoveX(posX, speed).SetEase(Ease.InOutSine);
            }
        }
    }

    public void AlignSecondaryCardHoldersToLeft()
    {
        float speed = ReferenceManager.Instance.gameLogicController.GameSettings.tableHolderCenteringSpeed;
        for (int i = 0; i < _activeSecondaryCardHolders.Count; i++)
        {
            CardHolder holder = _activeSecondaryCardHolders[i];
            RectTransform rect = holder.GetComponent<RectTransform>();
            Vector2 targetPos = _tableLayout.GetSecondaryCardHolderPosition(i, rect.anchoredPosition.y);
            rect.DOLocalMove(targetPos, speed).SetEase(Ease.InOutSine);
        }
    }
}
