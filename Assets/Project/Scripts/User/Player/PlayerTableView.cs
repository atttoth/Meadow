using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTableView : TableView
{
    // Primary - ground and observation cards
    private List<CardHolder> _primaryCardHolders;
    private List<CardHolder> _primaryCardHolderPool;
    private Transform _primaryCardHolderContainer;
    private Transform _primaryCardHolderPoolContainer;

    // Secondary - landscape and discovery cards
    private List<CardHolder> _secondaryCardHolders;
    private List<CardHolder> _secondaryCardHolderPool;
    private Transform _secondaryCardHolderContainer;
    private Transform _secondaryCardHolderPoolContainer;

    private ScrollRect _primaryTableContentScroll;
    private List<TableCardHitArea> _primaryHitAreas; // left and right sides
    private TableCardHitArea _secondaryHitArea; // right side

    private TextMeshProUGUI _approveButtonText;
    private Image _approveButtonImage;
    private TableLayout _tableLayout;
    public bool isTableVisible;

    public override void Init()
    {
        base.Init();
        _primaryCardHolders = new();
        _primaryCardHolderContainer = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0); // .../TableContents/PrimaryPage/Content/CardHolders
        _primaryCardHolderPoolContainer = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1); // .../TableContents/Primary/Content/CardHolderPool

        _secondaryCardHolders = new();
        _secondaryCardHolderContainer = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0); // .../TableContents/SecondaryPage/Content/CardHolders
        _secondaryCardHolderPoolContainer = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1); // .../TableContents/Secondary/Content/CardHolderPool
        
        _primaryTableContentScroll = transform.GetChild(0).GetChild(0).GetComponent<ScrollRect>();
        _approveButtonText = transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>();
        _approveButtonImage = transform.GetChild(1).GetComponent<Image>();

        CreateCardHolderPools();
        EnableTableScroll(false);
        CreateUIHitAreas();
        CreateTableLayout();
        isTableVisible = false;
    }

    private void CreateCardHolderPools()
    {
        _primaryCardHolderPool = new();
        for (int i = 0; i < _MAX_PRIMARY_HOLDER_NUM; i++)
        {
            CardHolder holder = Instantiate(GameResourceManager.Instance.tablePrimaryCardHolderPrefab, _primaryCardHolderPoolContainer).GetComponent<CardHolder>();
            holder.Init(i, HolderType.TableCard);
            holder.Data.holderSubType = HolderSubType.PRIMARY;
            holder.gameObject.SetActive(false);
            _primaryCardHolderPool.Add(holder);
        }

        _secondaryCardHolderPool = new();
        for (int i = 0; i < _MAX_SECONDARY_HOLDER_NUM; i++)
        {
            CardHolder holder = Instantiate(GameResourceManager.Instance.tableSecondaryCardHolderPrefab, _secondaryCardHolderPoolContainer).GetComponent<CardHolder>();
            holder.Init(_MAX_PRIMARY_HOLDER_NUM + i, HolderType.TableCard);
            holder.Data.holderSubType = HolderSubType.SECONDARY;
            holder.gameObject.SetActive(false);
            _secondaryCardHolderPool.Add(holder);
        }
    }

    private void CreateTableLayout()
    {
        Rect rect = transform.GetComponent<RectTransform>().rect;
        float tableClosedPosY = transform.position.y;
        float tableOpenPosY = tableClosedPosY + rect.height;
        float primaryHolderWidth = GameResourceManager.Instance.tablePrimaryCardHolderPrefab.GetComponent<RectTransform>().rect.width;
        float secondaryHolderWidth = GameResourceManager.Instance.tableSecondaryCardHolderPrefab.GetComponent<RectTransform>().rect.width;
        _tableLayout = new TableLayout(tableClosedPosY, tableOpenPosY, rect.width, primaryHolderWidth, secondaryHolderWidth);
    }

    public void EnableTableScroll(bool value)
    {
        _primaryTableContentScroll.enabled = value;
    }

    public override List<List<CardIcon>> GetAdjacentPrimaryHolderIcons(HolderData holderData)
    {
        int index = _activeState.PrimaryCardHolderDataCollection.IndexOf(holderData);
        List<List<CardIcon>> adjacentHolderIcons = new() { new(), new() };
        CardHolder leftHolder = index > 0 ? _primaryCardHolders[index - 1] : null;
        CardHolder rightHolder = index < _primaryCardHolders.Count - 1 ? _primaryCardHolders[index + 1] : null;

        if (leftHolder != null && !leftHolder.Data.IsEmpty())
        {
            adjacentHolderIcons[0] = leftHolder.Data.GetAllIconsOfHolder();
        }

        if (rightHolder != null && !rightHolder.Data.IsEmpty())
        {
            adjacentHolderIcons[1] = rightHolder.Data.GetAllIconsOfHolder();
        }
        return adjacentHolderIcons;
    }

    public override void RegisterCardPlacementAction(HolderData holderData, Card card, bool isActionCancelled = false)
    {
        if (isActionCancelled)
        {
            holderData.RemoveItemFromContentList(card);
        }
        else
        {
            holderData.AddItemToContentList(card);
        }
        card.canMove = isActionCancelled;
        card.cardStatus = isActionCancelled ? CardStatus.IN_HAND : CardStatus.PENDING_ON_TABLE;
    }

    public override void AddNewPrimaryHolder(string tag)
    {
        int index = tag == "RectLeft" ? 0 : _primaryCardHolders.Count;
        CardHolder holder = _primaryCardHolderPool[0];
        _primaryCardHolderPool.Remove(holder);
        holder.transform.SetParent(_primaryCardHolderContainer);
        holder.transform.SetSiblingIndex(index);
        holder.gameObject.SetActive(true);
        RectTransform rect = holder.GetComponent<RectTransform>();
        RectTransform prevHolderTransform = null;
        if (_primaryCardHolders.Count > 0)
        {
            int prevHolderIndex = _primaryCardHolders.Count == 1 ? 0 : tag == "RectLeft" ? 0 : _primaryCardHolders.Count - 1;
            prevHolderTransform = _primaryCardHolders[prevHolderIndex].GetComponent<RectTransform>();
        }
        rect.anchoredPosition = _tableLayout.GetPrimaryHolderPosition(prevHolderTransform, tag == "RectLeft" ? -1 : 1, rect.anchoredPosition.y);
        _activeState.PrimaryCardHolderDataCollection.Insert(index, holder.Data);
        _primaryCardHolders.Insert(index, holder);
    }

    public override void AddNewSecondaryHolder()
    {
        CardHolder holder = _secondaryCardHolderPool[0];
        _secondaryCardHolderPool.Remove(holder);
        holder.transform.SetParent(_secondaryCardHolderContainer);
        holder.transform.SetSiblingIndex(_secondaryCardHolders.Count);
        holder.gameObject.SetActive(true);
        RectTransform rect = holder.GetComponent<RectTransform>();
        rect.anchoredPosition = _tableLayout.GetSecondaryCardHolderPosition(_secondaryCardHolders.Count, rect.anchoredPosition.y);
        _activeState.SecondaryCardHolderDataCollection.Add(holder.Data);
        _secondaryCardHolders.Add(holder);
    }

    private void CreateUIHitAreas()
    {
        _primaryHitAreas = new();
        for (int i = 0; i < 2; i++)
        {
            TableCardHitArea primaryHitArea = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).GetChild(i).GetComponent<TableCardHitArea>(); // .../TableContents/Primary/Content/UIHitArea/Hitarea
            primaryHitArea.Init();
            primaryHitArea.type = HolderSubType.PRIMARY;
            primaryHitArea.Toggle(false);
            _primaryHitAreas.Add(primaryHitArea);
        }

        _secondaryHitArea = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(2).GetComponent<TableCardHitArea>(); // .../TableContents/Secondary/Content/Hitarea
        _secondaryHitArea.Init();
        _secondaryHitArea.type = HolderSubType.SECONDARY;
        _secondaryHitArea.Toggle(false);
    }

    public void UpdateHitAreaSizeAction(Card card, bool isActionCancelled = false)
    {
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
            if (cardType == CardType.Ground && _primaryCardHolders.Count < _MAX_PRIMARY_HOLDER_NUM)
            {
                CalculatePrimaryHitAreaSizeAndPosition(cardType, 1);
            }
            else if (cardType == CardType.Landscape && _secondaryCardHolders.Count < _MAX_SECONDARY_HOLDER_NUM)
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
            rect.sizeDelta = _tableLayout.GetPrimaryHitAreaSize(_primaryCardHolders.Count, status, rect.sizeDelta.y);
            float diff = prevWidth - rect.sizeDelta.x;
            rect.anchoredPosition = _tableLayout.GetHitAreaPosition(rect, diff);
        });
    }

    private void CalculateSecondaryHitAreaSizeAndPosition(CardType cardType, int status)
    {
        RectTransform rect = _secondaryHitArea.GetComponent<RectTransform>();
        float prevWidth = rect.sizeDelta.x;
        rect.sizeDelta = _tableLayout.GetSecondaryHitAreaSize(_secondaryCardHolders.Count, status, rect.sizeDelta.y);
        float diff = prevWidth - rect.sizeDelta.x;
        rect.anchoredPosition = _tableLayout.GetHitAreaPosition(rect, diff);
    }

    public void TogglePrimaryHitAreas(bool value)
    {
        if(value && _primaryCardHolders.Count == _MAX_PRIMARY_HOLDER_NUM) return;

        _primaryHitAreas.ForEach(hitArea => hitArea.Toggle(value));
    }

    public void ToggleSecondaryHitArea(bool value)
    {
        if (value && _secondaryCardHolders.Count == _MAX_SECONDARY_HOLDER_NUM) return;

        _secondaryHitArea.Toggle(value);
    }

    public void TogglePanel()
    {
        float speed = GameSettings.Instance.GetDuration(Duration.tableViewOpenSpeed);
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

    public void AdjustHolderVerticallyAction(HolderData holderData, bool isActionCancelled = false)
    {
        if (holderData.holderSubType == HolderSubType.PRIMARY)
        {
            CardHolder holder = _primaryCardHolders[_activeState.PrimaryCardHolderDataCollection.IndexOf(holderData)];
            RectTransform rect = holder.GetComponent<RectTransform>();
            holder.transform.position += _tableLayout.GetUpdatedPrimaryHolderPosition(isActionCancelled ? -1 : 1);
            rect.sizeDelta = _tableLayout.GetUpdatedPrimaryHolderSize(rect, isActionCancelled ? -1 : 1);
        }
    }

    public void PositionTableCard(CardHolder holder, Card card, float speed, float[] handCardPositions, Transform parentTransform)
    {
        bool isPlacement = card.cardStatus == CardStatus.PENDING_ON_TABLE;
        Vector2 position;
        if (isPlacement)
        {
            card.transform.SetParent(parentTransform);
            float[] holderPositions = holder.Data.holderSubType == HolderSubType.PRIMARY 
                ? _tableLayout.GetPrimaryCardHolderPositions(_primaryCardHolders.Count) 
                : _tableLayout.GetSecondaryCardHolderPositions(_secondaryCardHolders.Count);
            position = _tableLayout.GetPlacedCardPosition(card.Data.cardType, holder.Data.GetContentListSize() - 1, holderPositions[holder.transform.GetSiblingIndex()]);
        }
        else
        {
            card.transform.SetParent(parentTransform);
            float posX = handCardPositions.Length > 0 ? handCardPositions[^1] : 0f;
            position = _tableLayout.GetCancelledCardPosition(posX, card.hoverTargetY);
        }
        card.PositionCardTween(position, speed, isPlacement);
    }

    public void RemoveEmptyHolder(HolderSubType holderSubType)
    {
        List<CardHolder> activeCardHolders = holderSubType == HolderSubType.PRIMARY ? _primaryCardHolders : _secondaryCardHolders;
        List<HolderData> holderDataCollection = holderSubType == HolderSubType.PRIMARY ? _activeState.PrimaryCardHolderDataCollection : _activeState.SecondaryCardHolderDataCollection;
        List <CardHolder> cardHolderPool = holderSubType == HolderSubType.PRIMARY ? _primaryCardHolderPool : _secondaryCardHolderPool;
        Transform cardHolderPoolContainer = holderSubType == HolderSubType.PRIMARY ? _primaryCardHolderPoolContainer : _secondaryCardHolderPoolContainer;
        for (int i = 0; i < activeCardHolders.Count; i++)
        {
            CardHolder holder = activeCardHolders[i];
            HolderData holderData = holder.Data;
            if (holderData.IsEmpty())
            {
                holder.transform.SetParent(cardHolderPoolContainer);
                holder.gameObject.SetActive(false);
                activeCardHolders.Remove(holder);
                holderDataCollection.Remove(holderData);
                cardHolderPool.Add(holder);
                break;
            }
        }
    }

    public CardHolder GetPrimaryCardHolderByTag(string tagName)
    {
        int index = tagName == "RectLeft" ? 0 : _primaryCardHolders.Count - 1;
        return _primaryCardHolders[index];
    }

    public CardHolder GetPrimaryCardHolderByID(int ID)
    {
        return _primaryCardHolders.Find(holder => holder.Data.ID == ID);
    }

    public CardHolder GetLastSecondaryCardHolder()
    {
        return _secondaryCardHolders[_secondaryCardHolders.Count - 1];
    }

    public void AlignPrimaryCardHoldersToCenter()
    {
        if(_primaryCardHolders.Count > 0)
        {
            float speed = GameSettings.Instance.GetDuration(Duration.tableHolderCenteringSpeed);
            float[] positions = _tableLayout.GetPrimaryCardHolderPositions(_primaryCardHolders.Count);
            for (int i = 0; i < positions.Length; i++)
            {
                Transform holderTransform = _primaryCardHolders[i].transform;
                float posX = positions[i];
                holderTransform.DOLocalMoveX(posX, speed).SetEase(Ease.InOutSine);
            }
        }
    }

    public void AlignSecondaryCardHoldersToLeft()
    {
        float speed = GameSettings.Instance.GetDuration(Duration.tableHolderCenteringSpeed);
        for (int i = 0; i < _secondaryCardHolders.Count; i++)
        {
            RectTransform rect = _secondaryCardHolders[i].GetComponent<RectTransform>();
            Vector2 targetPos = _tableLayout.GetSecondaryCardHolderPosition(i, rect.anchoredPosition.y);
            rect.DOLocalMove(targetPos, speed).SetEase(Ease.InOutSine);
        }
    }
}
