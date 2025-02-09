using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTableView : TableView
{
    // Primary - ground and observation cards
    private List<CardHolder> _primaryCardHolderPool;
    private Transform _primaryCardHolderPoolContainer;
    private List<RectTransform> _primaryHitAreas; // left and right sides

    // Secondary - landscape and discovery cards
    private List<CardHolder> _secondaryCardHolderPool;
    private Transform _secondaryCardHolderPoolContainer;
    private RectTransform _secondaryHitArea; // right side

    private TextMeshProUGUI _approveButtonText;
    private Image _approveButtonImage;
    private TableLayout _tableLayout;
    public bool isTableVisible;

    public void Init()
    {
        _primaryTableContentScroll = transform.GetChild(0).GetChild(0).GetComponent<ScrollRect>();
        _primaryCardHolderPoolContainer = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1); // .../TableContents/Primary/Content/CardHolderPool
        _primaryCardHolderContainer = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0); // .../TableContents/PrimaryPage/Content/CardHolders
        _activePrimaryCardHolders = new();

        _secondaryCardHolderPoolContainer = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(1); // .../TableContents/Secondary/Content/CardHolderPool
        _secondaryCardHolderContainer = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(0); // .../TableContents/SecondaryPage/Content/CardHolders
        _activeSecondaryCardHolders = new();

        _iconsTransform = transform.GetChild(2).GetChild(0).GetChild(1); // .../TableDisplayButton/IconDisplay/DisplayIcons
        _displayIconPoolTransform = transform.GetChild(2).GetChild(0).GetChild(0); // .../TableDisplayButton/IconDisplay/DisplayIconPool
        _preparedIconsTransform = transform.GetChild(2).GetChild(0).GetChild(2); // .../TableDisplayButton/IconDisplay/PreparedDisplayIcons
        _displayIconPool = new();
        _displayIcons = new();
        _preparedDisplayIcons = new();

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

    public int GetDisplayIconsAmount()
    {
        return _displayIcons.Count;
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

    private DisplayIcon GetDisplayIconFromPool()
    {
        DisplayIcon displayIcon;
        if (_displayIconPool.Count < 1)
        {
            displayIcon = Instantiate(GameAssets.Instance.displayIconPrefab, _displayIconPoolTransform).GetComponent<DisplayIcon>();
            displayIcon.gameObject.SetActive(false);
        }
        else
        {
            displayIcon = _displayIconPool.First();
            _displayIconPool.RemoveAt(0);
        }
        return displayIcon;
    }

    private void AddDisplayIconToPool(DisplayIcon displayIcon)
    {
        _displayIconPool.Add(displayIcon);
        displayIcon.gameObject.SetActive(false);
        displayIcon.transform.SetParent(_displayIconPoolTransform);
        displayIcon.transform.position = _displayIconPoolTransform.position;
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
            TableCardHitArea primaryHitArea = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(2).GetChild(i).GetComponent<TableCardHitArea>(); // .../TableContents/Primary/Content/UIHitArea/Rect
            primaryHitArea.type = HolderSubType.PRIMARY;
            RectTransform rect = primaryHitArea.GetComponent<RectTransform>();
            rect.gameObject.SetActive(false);
            _primaryHitAreas.Add(rect);
        }

        TableCardHitArea secondaryHitArea = transform.GetChild(0).GetChild(1).GetChild(0).GetChild(2).GetComponent<TableCardHitArea>(); // .../TableContents/Secondary/Content/RectRight/
        secondaryHitArea.type = HolderSubType.SECONDARY;
        secondaryHitArea.gameObject.SetActive(false);
        _secondaryHitArea = secondaryHitArea.GetComponent<RectTransform>();
    }

    public void UpdateHitAreaSize(GameTaskItemData data)
    {
        CardType cardType = data.card.Data.cardType;
        if (cardType == CardType.Ground && _activePrimaryCardHolders.Count < _MAX_PRIMARY_HOLDER_NUM)
        {
            CalculatePrimaryHitAreaSizeAndPosition(cardType, 1);
        }
        else if(cardType == CardType.Landscape && _activeSecondaryCardHolders.Count < _MAX_SECONDARY_HOLDER_NUM)
        {
            CalculateSecondaryHitAreaSizeAndPosition(cardType, 1);
        }
    }

    public void UpdateHitAreaSizeRewind(GameTaskItemData data)
    {
        CardType cardType = data.card.Data.cardType;
        if (cardType == CardType.Ground)
        {
            CalculatePrimaryHitAreaSizeAndPosition(cardType, -1);
        }
        else if(cardType == CardType.Landscape)
        {
            CalculateSecondaryHitAreaSizeAndPosition(cardType, -1);
        }
    }

    private void CalculatePrimaryHitAreaSizeAndPosition(CardType cardType, int status)
    {
        _primaryHitAreas.ForEach(rect =>
        {
            float prevWidth = rect.sizeDelta.x;
            rect.sizeDelta = _tableLayout.GetPrimaryHitAreaSize(_activePrimaryCardHolders.Count, status, rect.sizeDelta.y);
            float diff = prevWidth - rect.sizeDelta.x;
            rect.anchoredPosition = _tableLayout.GetHitAreaPosition(rect, diff);
        });
    }

    private void CalculateSecondaryHitAreaSizeAndPosition(CardType cardType, int status)
    {
        float prevWidth = _secondaryHitArea.sizeDelta.x;
        _secondaryHitArea.sizeDelta = _tableLayout.GetSecondaryHitAreaSize(_activeSecondaryCardHolders.Count, status, _secondaryHitArea.sizeDelta.y);
        float diff = prevWidth - _secondaryHitArea.sizeDelta.x;
        _secondaryHitArea.anchoredPosition = _tableLayout.GetHitAreaPosition(_secondaryHitArea, diff);
    }

    public void TogglePrimaryHitAreas(bool value)
    {
        if(value && _activePrimaryCardHolders.Count == _MAX_PRIMARY_HOLDER_NUM) return;

        _primaryHitAreas.ForEach(rect => rect.gameObject.SetActive(value));
    }

    public void ToggleSecondaryHitArea(bool value)
    {
        if (value && _activeSecondaryCardHolders.Count == _MAX_SECONDARY_HOLDER_NUM) return;

        _secondaryHitArea.gameObject.SetActive(value);
    }

    public void PrepareDisplayIcon(Card card)
    {
        CardHolder holder = card.transform.parent.GetComponent<CardHolder>();
        int holderID = holder.ID;
        DisplayIcon displayIcon = GetDisplayIconFromPool();
        displayIcon.transform.SetParent(_preparedIconsTransform);
        displayIcon.transform.position = _preparedIconsTransform.position;
        displayIcon.SetUpDisplayIcon(holderID);
        List<CardIcon> icons = card.Data.icons.ToList();
        if (card.Data.cardType != CardType.Ground) // need to find a ground icon for displayIcon
        {
            List<CardIcon> groundIcons;
            groundIcons = _displayIcons.Find(icon => icon.ID == holderID)?.GroundIcons;
            if (groundIcons == null)
            {
                groundIcons = _preparedDisplayIcons.Find(icon => icon.ID == holderID)?.GroundIcons;
                if (groundIcons == null)
                {
                    groundIcons = _activePrimaryCardHolders
                        .Find(holder => holder.ID == holderID)
                        .GetAllIconsOfHolder()
                        .Where(cardIcon => (int)cardIcon < 5)
                        .ToList();
                }
            }
            icons.AddRange(groundIcons);
        }
        displayIcon.SaveIcons(icons);
        _preparedDisplayIcons.Add(displayIcon);
    }

    public void SetDisplayIconsHorizontalPositionHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                int duration = 0;
                if (GetDisplayIconsAmount() > 0)
                {
                    float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.displayIconHorizontalSlideSpeed;
                    List<int> oldHolderIDs = _displayIcons.Select(icon => icon.ID).ToList();
                    List<int> preparedHolderIDs = _preparedDisplayIcons.Select(icon => icon.ID).ToList();
                    List<int> newHolderIDs = preparedHolderIDs.Except(oldHolderIDs).ToList();
                    List<DisplayIcon> leftDisplayIcons = new();
                    List<DisplayIcon> rightDisplayIcons = new();
                    List<DisplayIcon> existingDisplayIcons = new();
                    int minSiblingIndex = _activePrimaryCardHolders
                        .Where(holder => oldHolderIDs.Contains(holder.ID))
                        .Select(holder => holder.transform.GetSiblingIndex())
                        .Min();

                    newHolderIDs.ForEach(holderID =>
                    {
                        DisplayIcon newDisplayIcon = _preparedDisplayIcons.Find(icon => icon.ID == holderID);
                        int siblingIndex = _activePrimaryCardHolders.Find(holder => holder.ID == holderID).transform.GetSiblingIndex();
                        if (siblingIndex < minSiblingIndex)
                        {
                            leftDisplayIcons.Add(newDisplayIcon);
                        }
                        else
                        {
                            rightDisplayIcons.Add(newDisplayIcon);
                        }
                    });
                    existingDisplayIcons = _preparedDisplayIcons.Where(icon => oldHolderIDs.Contains(icon.ID)).ToList();

                    int slideDirectionValue = (leftDisplayIcons.Count * -1) + rightDisplayIcons.Count;

                    // position left icons
                    if (leftDisplayIcons.Count > 0)
                    {
                        float leftPosX = _displayIcons.Find(icon => icon.transform.GetSiblingIndex() == 0).transform.position.x;
                        int modifier = leftDisplayIcons.Count;
                        leftDisplayIcons.ForEach(icon =>
                        {
                            float posX = leftPosX - TableLayout.SLIDE_X_DISTANCE_OF_DISPLAY_ICON * 2 * modifier;
                            posX -= TableLayout.SLIDE_X_DISTANCE_OF_DISPLAY_ICON * slideDirectionValue;
                            icon.transform.position = new(posX, icon.transform.position.y);
                            modifier--;
                        });
                    }

                    // position right icons
                    if (rightDisplayIcons.Count > 0)
                    {
                        float rightPosX = _displayIcons.Find(icon => icon.transform.GetSiblingIndex() == _displayIcons.Count - 1).transform.position.x;
                        int modifier = rightDisplayIcons.Count;
                        rightDisplayIcons.Reverse();
                        rightDisplayIcons.ForEach(icon =>
                        {
                            float posX = rightPosX + TableLayout.SLIDE_X_DISTANCE_OF_DISPLAY_ICON * 2 * modifier;
                            posX -= TableLayout.SLIDE_X_DISTANCE_OF_DISPLAY_ICON * slideDirectionValue;
                            icon.transform.position = new(posX, icon.transform.position.y);
                            modifier--;
                        });
                    }

                    // position remaining (existing) icons
                    if (existingDisplayIcons.Count > 0)
                    {
                        existingDisplayIcons.ForEach(icon =>
                        {
                            float posX = _displayIcons.Find(oldIcon => oldIcon.ID == icon.ID).transform.position.x;
                            posX -= TableLayout.SLIDE_X_DISTANCE_OF_DISPLAY_ICON * slideDirectionValue;
                            icon.transform.position = new(posX, icon.transform.position.y);
                        });
                    }

                    // slide old icons
                    if (slideDirectionValue != 0)
                    {
                        duration = (int)(speed * 1000);
                        _displayIcons.ForEach(icon =>
                        {
                            float posX = icon.transform.position.x - TableLayout.SLIDE_X_DISTANCE_OF_DISPLAY_ICON * slideDirectionValue;
                            icon.transform.DOMoveX(posX, speed).SetEase(Ease.InOutSine);
                        });
                    }
                }
                else
                {
                    // no displayIcons present
                    List<DisplayIcon> initialDisplayIcons = _preparedDisplayIcons;
                    int modifier = initialDisplayIcons.Count - 1;
                    initialDisplayIcons.Reverse();
                    initialDisplayIcons.ForEach(icon =>
                    {
                        float posX = icon.transform.position.x + TableLayout.SLIDE_X_DISTANCE_OF_DISPLAY_ICON * 2 * modifier;
                        posX -= TableLayout.SLIDE_X_DISTANCE_OF_DISPLAY_ICON * (initialDisplayIcons.Count - 1);
                        icon.transform.position = new(posX, icon.transform.position.y);
                        modifier--;
                    });
                }
                task.StartDelayMs(duration);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void ChangeDisplayIconsHandler(GameTask task)
    {
        // move new icons up, and old ones out
        switch(task.State)
        {
            case 0:
                float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.displayIconVerticalSlideSpeed;
                _preparedDisplayIcons.ForEach(displayIcon =>
                {
                    DisplayIcon oldDisplayIcon = _displayIcons.Find(icon => icon.ID == displayIcon.ID);
                    displayIcon.gameObject.SetActive(true);

                    if (displayIcon.MainIcons.Count > 1)
                    {
                        displayIcon.PlayIconToggle();
                    }
                    else if (displayIcon.MainIcons.Count == 1)
                    {
                        displayIcon.SetIconImage();
                    }
                    else
                    {
                        displayIcon.ChangeAlpha(0);
                    }

                    if (displayIcon.GroundIcons.Count > 1)
                    {
                        displayIcon.PlayBackgroundToggle();
                    }
                    else
                    {
                        displayIcon.background.color = displayIcon.GetColorByGroundIcon(displayIcon.GroundIcons.First());
                    }

                    if (oldDisplayIcon != null)
                    {
                        _displayIcons.Remove(oldDisplayIcon);
                        oldDisplayIcon.transform.DOMoveY(_displayIconPoolTransform.position.y, speed).OnComplete(() => AddDisplayIconToPool(oldDisplayIcon));
                    }
                    _displayIcons.Add(displayIcon);
                    displayIcon.transform.DOMoveY(_iconsTransform.position.y, speed).OnComplete(() => displayIcon.transform.SetParent(_iconsTransform));
                });
                _preparedDisplayIcons = new();
                task.StartDelayMs((int)(speed * 1000));
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void ReOrderDisplayIconsHierarchy()
    {
        _displayIcons.ForEach(displayIcon =>
        {
            int siblingIndex = _activePrimaryCardHolders.Find(holder => holder.ID == displayIcon.ID).transform.GetSiblingIndex();
            displayIcon.transform.SetSiblingIndex(siblingIndex);
        });
    }

    public void TogglePanel()
    {
        float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.tableViewOpenSpeed;
        isTableVisible = !isTableVisible;
        float target = _tableLayout.GetTargetTableViewPosY(isTableVisible);
        if(isTableVisible)
        {
            _primaryTableContentScroll.verticalNormalizedPosition = 0f;
        }
        transform.DOMoveY(target, speed).SetEase(Ease.InOutExpo);
    }

    public void UpdateApproveButton(bool isPendingAction)
    {
        _approveButtonText.text = isPendingAction ? "OK" : "Close";
        _approveButtonImage.color = isPendingAction ? Color.green : Color.black;
    }

    public void ExpandHolderVertically(GameTaskItemData data)
    {
        CardHolder holder = (CardHolder)data.holder;
        if (holder.holderSubType == HolderSubType.PRIMARY)
        {
            RectTransform rect = holder.GetComponent<RectTransform>();
            holder.transform.position += _tableLayout.GetUpdatedPrimaryHolderPosition(1);
            rect.sizeDelta = _tableLayout.GetUpdatedPrimaryHolderSize(rect, 1);
        }
    }

    public void ExpandHolderVerticallyRewind(GameTaskItemData data)
    {
        CardHolder holder = (CardHolder)data.holder;
        if (holder.holderSubType == HolderSubType.PRIMARY)
        {
            RectTransform rect = holder.GetComponent<RectTransform>();
            holder.transform.position += _tableLayout.GetUpdatedPrimaryHolderPosition(-1);
            rect.sizeDelta = _tableLayout.GetUpdatedPrimaryHolderSize(rect, -1);
        }
    }

    public void StackCard(GameTaskItemData data)
    {
        Card card = data.card;
        CardHolder holder = (CardHolder)data.holder;
        holder.AddToContentList(card);
        int contentSize = holder.GetContentListSize();
        card.canMove = false;
        card.cardStatus = CardStatus.PENDING_ON_TABLE;
        if (contentSize > 1) // update card status below top card
        {
            Card cardBelow = (Card)holder.GetItemFromContentListByIndex(contentSize - 2);
            if (cardBelow.cardStatus == CardStatus.PENDING_ON_TABLE)
            {
                cardBelow.cardStatus = CardStatus.STACKED_PENDING_ON_TABLE;
            }
        }
    }

    public void StackCardRewind(GameTaskItemData data)
    {
        Card card = data.card;
        CardHolder holder = (CardHolder)data.holder;
        holder.RemoveItemFromContentList(card);
        int contentSize = holder.GetContentListSize();
        card.canMove = true;
        card.cardStatus = CardStatus.IN_HAND;
        if (contentSize > 0) // update card status below top card
        {
            Card cardBelow = (Card)holder.GetItemFromContentListByIndex(contentSize - 1);
            if (cardBelow.cardStatus == CardStatus.STACKED_PENDING_ON_TABLE)
            {
                cardBelow.cardStatus = CardStatus.PENDING_ON_TABLE;
            }
        }
    }

    public void PositionTableCard(Card card, int contentCount, float speed, bool isPlacement, float lastPosX)
    {
        RectTransform rect = card.GetComponent<RectTransform>();
        Vector2 targetPos = _tableLayout.GetCardTargetPosition(card, contentCount, isPlacement, lastPosX);
        if (Array.Exists(new CardType[] { CardType.Landscape, CardType.Discovery }, type => type == card.Data.cardType))
        {
            float rotation = isPlacement ? 90f : 0f;
            DOTween.Sequence()
                .Append(rect.DOAnchorPos(targetPos, speed))
                .Join(rect.DORotate(new(0f, 0f, rotation), speed))
                .SetEase(Ease.InOutSine);
        }
        else
        {
            DOTween.Sequence().Append(rect.DOAnchorPos(targetPos, speed)).SetEase(Ease.InOutSine);
        }
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
            float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.tableHolderCenteringSpeed;
            float[] positions = _tableLayout.GetPrimaryCardHolderLayout(_activePrimaryCardHolders.Count);
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
        float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.tableHolderCenteringSpeed;
        for (int i = 0; i < _activeSecondaryCardHolders.Count; i++)
        {
            CardHolder holder = _activeSecondaryCardHolders[i];
            RectTransform rect = holder.GetComponent<RectTransform>();
            Vector2 targetPos = _tableLayout.GetSecondaryCardHolderPosition(i, rect.anchoredPosition.y);
            rect.DOLocalMove(targetPos, speed).SetEase(Ease.InOutSine);
        }
    }
}
