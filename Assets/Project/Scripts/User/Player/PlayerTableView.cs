using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTableView : TableView
{
    private static float _Y_GAP_BETWEEN_STACKED_CARDS = 60f;
    private static float _SLIDE_X_DISTANCE_OF_DISPLAY_ICON = 30f;
    private float _originY;
    private float _targetY;
    public bool isTableVisible;

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
    
    public void Init()
    {
        _originY = transform.position.y;
        _targetY = _originY + transform.GetComponent<RectTransform>().rect.height;
        isTableVisible = false;
        _primaryTableContentScroll = transform.GetChild(0).GetChild(0).GetComponent<ScrollRect>();
        _primaryCardHolderPoolContainer = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(1); // .../TableContents/Primary/Content/CardHolderPool
        _primaryCardHolderContainer = transform.GetChild(0).GetChild(0).GetChild(0).GetChild(0); // .../TableContents/PrimaryPage/Content/CardHolders
        _activePrimaryCardHolders = new();

        _secondaryTableContentScroll = transform.GetChild(0).GetChild(1).GetComponent<ScrollRect>();
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
        AddFirstPrimaryHolder();
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

    public void AddPrimaryHolder(string tag)
    {
        if (_activePrimaryCardHolders.Count == 1 && _activePrimaryCardHolders[0].IsEmpty())
        {
            return;
        }

        int direction = tag == "RectLeft" ? -1 : 1;
        int listIndex = direction == -1 ? 0 : _activePrimaryCardHolders.Count;
        int prevHolderIndex = _activePrimaryCardHolders.Count == 1 ? 0 : direction == -1 ? 0 : _activePrimaryCardHolders.Count - 1;
        CardHolder prevHolder = _activePrimaryCardHolders[prevHolderIndex];
        CardHolder holder = _primaryCardHolderPool[0];
        holder.Init(-1, HolderType.TableCard);
        holder.holderSubType = HolderSubType.PRIMARY;
        holder.transform.SetParent(_primaryCardHolderContainer);
        holder.transform.SetSiblingIndex(listIndex);
        holder.gameObject.SetActive(true);
        RectTransform rect = holder.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(prevHolder.GetComponent<RectTransform>().anchoredPosition.x + 190 * direction, rect.anchoredPosition.y);
        _primaryCardHolderPool.Remove(holder);
        _activePrimaryCardHolders.Insert(listIndex, holder);
    }

    public void AddSecondaryHolder()
    {
        CardHolder holder = _secondaryCardHolderPool[0];
        holder.Init(-1, HolderType.TableCard);
        holder.holderSubType = HolderSubType.SECONDARY;
        holder.transform.SetParent(_secondaryCardHolderContainer);
        holder.transform.SetSiblingIndex(_activeSecondaryCardHolders.Count);
        holder.gameObject.SetActive(true);
        RectTransform rect = holder.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(_activeSecondaryCardHolders.Count * 100f, rect.anchoredPosition.y);
        _secondaryCardHolderPool.Remove(holder);
        _activeSecondaryCardHolders.Add(holder);
    }

    private DisplayIcon GetDisplayIconFromPool()
    {
        DisplayIcon displayIcon;
        if (_displayIconPool.Count < 1)
        {
            displayIcon = Object.Instantiate(GameAssets.Instance.displayIconPrefab, _displayIconPoolTransform).GetComponent<DisplayIcon>();
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
        for (int i = 0; i < _MAX_HOLDER_NUM; i++)
        {
            CardHolder holder = Object.Instantiate(GameAssets.Instance.tableCardHolderPrefab, _primaryCardHolderPoolContainer).GetComponent<CardHolder>();
            holder.ID = i;
            holder.holderType = HolderType.TableCard;
            holder.gameObject.SetActive(false);
            _primaryCardHolderPool.Add(holder);
        }

        _secondaryCardHolderPool = new();
        for (int i = 0; i < _MAX_HOLDER_NUM; i++)
        {
            CardHolder holder = Object.Instantiate(GameAssets.Instance.tableCardHolderPrefab, _secondaryCardHolderPoolContainer).GetComponent<CardHolder>();
            holder.ID = _MAX_HOLDER_NUM + i;
            holder.holderType = HolderType.TableCard;
            holder.gameObject.SetActive(false);
            _secondaryCardHolderPool.Add(holder);
        }
    }

    private void AddFirstPrimaryHolder()
    {
        CardHolder holder = _primaryCardHolderPool[0];
        holder.Init(-1, HolderType.TableCard);
        holder.holderSubType = HolderSubType.PRIMARY;
        holder.gameObject.SetActive(true);
        holder.transform.SetParent(_primaryCardHolderContainer);
        RectTransform rect = holder.GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, rect.anchoredPosition.y);
        _primaryCardHolderPool.Remove(holder);
        _activePrimaryCardHolders.Add(holder);
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

    public void UpdatePrimaryHitAreaSize(GameTaskItemData data)
    {
        // decrease size above 2 holders
        // decrease size only at ground cards
        // ignore updating size at max (10) holders
        if (_activePrimaryCardHolders.Count < 2 || data.card.Data.cardType != CardType.Ground || _activePrimaryCardHolders.Count == _MAX_HOLDER_NUM)
        {
            return;
        }
        CalculatePrimaryHitAreaSizeAndPosition(false);
    }

    public void UpdatePrimaryHitAreaSizeRewind(GameTaskItemData data)
    {
        if (_activePrimaryCardHolders.Count < 2 || data.card.Data.cardType != CardType.Ground)
        {
            return;
        }
        CalculatePrimaryHitAreaSizeAndPosition(true);
    }

    private void CalculatePrimaryHitAreaSizeAndPosition(bool isRewind)
    {
        float status = isRewind ? -1 : 1;
        _primaryHitAreas.ForEach(rect =>
        {
            int direction = rect.CompareTag("RectLeft") ? -1 : 1;
            float widthValue = 95f * status;
            float posXValue = 47.5f;
            float moveValue = (posXValue * direction) * status;
            float width = rect.sizeDelta.x - widthValue;
            rect.sizeDelta = new(width, rect.sizeDelta.y);
            float posX = rect.anchoredPosition.x + moveValue;
            rect.anchoredPosition = new(posX, rect.anchoredPosition.y);
        });
    }

    public void TogglePrimaryHitAreas(bool value)
    {
        if(value && _activePrimaryCardHolders.Count == _MAX_HOLDER_NUM) return;

        _primaryHitAreas.ForEach(rect => rect.gameObject.SetActive(value));
    }

    public void ToggleSecondaryHitArea(bool value)
    {
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
                            float posX = leftPosX - _SLIDE_X_DISTANCE_OF_DISPLAY_ICON * 2 * modifier;
                            posX -= _SLIDE_X_DISTANCE_OF_DISPLAY_ICON * slideDirectionValue;
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
                            float posX = rightPosX + _SLIDE_X_DISTANCE_OF_DISPLAY_ICON * 2 * modifier;
                            posX -= _SLIDE_X_DISTANCE_OF_DISPLAY_ICON * slideDirectionValue;
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
                            posX -= _SLIDE_X_DISTANCE_OF_DISPLAY_ICON * slideDirectionValue;
                            icon.transform.position = new(posX, icon.transform.position.y);
                        });
                    }

                    // slide old icons
                    if (slideDirectionValue != 0)
                    {
                        duration = (int)(speed * 1000);
                        _displayIcons.ForEach(icon =>
                        {
                            float posX = icon.transform.position.x - _SLIDE_X_DISTANCE_OF_DISPLAY_ICON * slideDirectionValue;
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
                        float posX = icon.transform.position.x + _SLIDE_X_DISTANCE_OF_DISPLAY_ICON * 2 * modifier;
                        posX -= _SLIDE_X_DISTANCE_OF_DISPLAY_ICON * (initialDisplayIcons.Count - 1);
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
        isTableVisible = !isTableVisible;
        float target = isTableVisible ? _targetY : _originY;
        transform.DOMoveY(target, 0.5f).SetEase(Ease.InOutExpo);
        if(isTableVisible)
        {
            _primaryTableContentScroll.verticalNormalizedPosition = 0f;
            _secondaryTableContentScroll.verticalNormalizedPosition = 0f;
        }
    }

    public void UpdateApproveButton(bool isPendingAction)
    {
        _approveButtonText.text = isPendingAction ? "OK" : "Close";
        _approveButtonImage.color = isPendingAction ? Color.green : Color.black;
    }

    public void ExpandHolderVertically(GameTaskItemData data)
    {
        UpdateHolderHeight((CardHolder)data.holder, true);
    }

    public void ExpandHolderVerticallyRewind(GameTaskItemData data)
    {
        UpdateHolderHeight((CardHolder)data.holder, false);
    }

    private void UpdateHolderHeight(CardHolder holder, bool isIncrease)
    {
        if(holder.holderSubType == HolderSubType.PRIMARY)
        {
            int modifier = isIncrease ? 1 : -1;
            RectTransform rect = holder.GetComponent<RectTransform>();
            holder.transform.position += new Vector3(0f, _Y_GAP_BETWEEN_STACKED_CARDS * 0.5f * modifier, 0f);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + (_Y_GAP_BETWEEN_STACKED_CARDS * modifier));
        }
    }

    public void StackCard(GameTaskItemData data)
    {
        CardHolder holder = (CardHolder)data.holder;
        holder.AddToContentList(data.card);
        float startingPosY = 115f;
        int count = holder.GetContentListSize() - 1;
        float topPositionY = count < 1 ? startingPosY : startingPosY + _Y_GAP_BETWEEN_STACKED_CARDS * count;
        RectTransform rect = data.card.GetComponent<RectTransform>();
        rect.anchorMin = new(0.5f, 0f);
        rect.anchorMax = new(0.5f, 0f);
        rect.anchoredPosition = new(0f, topPositionY);
        data.card.canMove = false;
        data.card.cardStatus = CardStatus.PENDING_ON_TABLE;
        if (holder.GetContentListSize() > 1)
        {
            Card cardBelow = (Card)holder.GetItemFromContentListByIndex(holder.GetContentListSize() - 2);
            if (cardBelow.cardStatus == CardStatus.PENDING_ON_TABLE)
            {
                cardBelow.cardStatus = CardStatus.STACKED_PENDING_ON_TABLE;
            }
        }
    }

    public void StackCardRewind(GameTaskItemData data)
    {
        CardHolder holder = (CardHolder)data.holder;
        holder.RemoveItemFromContentList(data.card);
        RectTransform rect = data.card.GetComponent<RectTransform>();
        data.card.transform.SetParent(data.targetTransform);
        rect.anchorMin = new(0.5f, 0.5f);
        rect.anchorMax = new(0.5f, 0.5f);
        rect.anchoredPosition = new(data.card.prevAnchoredPosition.x, data.card.prevAnchoredPosition.y);
        data.card.canMove = true;
        data.card.cardStatus = CardStatus.IN_HAND;
        if (holder.GetContentListSize() > 0)
        {
            Card cardBelow = (Card)holder.GetItemFromContentListByIndex(holder.GetContentListSize() - 1);
            if (cardBelow.cardStatus == CardStatus.STACKED_PENDING_ON_TABLE)
            {
                cardBelow.cardStatus = CardStatus.PENDING_ON_TABLE;
            }
        }
    }

    public void RemovePrimaryHolder()
    {
        if (_activePrimaryCardHolders.Count == 1 && _activePrimaryCardHolders[0].IsEmpty())
        {
            return;
        }

        for(int i = 0; i < _activePrimaryCardHolders.Count; i++)
        {
            CardHolder holder = _activePrimaryCardHolders[i];
            if (holder.IsEmpty())
            {
                holder.transform.SetParent(_primaryCardHolderPoolContainer);
                holder.gameObject.SetActive(false);
                _activePrimaryCardHolders.Remove(holder);
                _primaryCardHolderPool.Add(holder);
                break;
            }
        }
    }

    public void RemoveSecondaryHolder()
    {
        for (int i = 0; i < _activeSecondaryCardHolders.Count; i++)
        {
            CardHolder holder = _activeSecondaryCardHolders[i];
            if (holder.IsEmpty())
            {
                holder.transform.SetParent(_secondaryCardHolderPoolContainer);
                holder.gameObject.SetActive(false);
                _activeSecondaryCardHolders.Remove(holder);
                _secondaryCardHolderPool.Add(holder);
                break;
            }
        }
    }

    public void CenterCardHolders()
    {
        float[] positions = GetCardHolderLayout();
        for (int i = 0; i < positions.Length; i++)
        {
            Transform holderTransform = _activePrimaryCardHolders[i].transform;
            float posX = positions[i];
            holderTransform.DOLocalMoveX(posX, 0.3f).SetEase(Ease.InOutSine);
        }
    }

    private float[] GetCardHolderLayout()
    {
        return _activePrimaryCardHolders.Count switch
        {
            2 => new float[] { -95, 95 },
            3 => new float[] { -190, 0, 190 },
            4 => new float[] { -285, -95, 95, 285 },
            5 => new float[] { -380, -190, 0, 190, 380 },
            6 => new float[] { -475, -285, -95, 95, 285, 475 },
            7 => new float[] { -570, -380, -190, 0, 190, 380, 570 },
            8 => new float[] { -665, -475, -285, -95, 95, 285, 475, 665 },
            9 => new float[] { -760, -570, -380, -190, 0, 190, 380, 570, 760 },
            10 => new float[] { -855, -665, -475, -285, -95, 95, 285, 475, 665, 855 },
            _ => new float[] { 0 },
        };
    }
}
