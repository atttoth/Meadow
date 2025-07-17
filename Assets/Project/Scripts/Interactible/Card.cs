using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Linq;

public enum CardType
{
    Ground, // 0
    Observation, // 1
    Landscape, // 2
    Discovery, // 3
    None // 4
}

public enum CardIcon
{
    //ground icons
    Litterfall, // 0
    Grass, // 1
    Sands, // 2
    Rocks, // 3
    Wetlands, // 4
    // everything else
    Insect, // 5
    Frog, // 6
    Worm, // 7
    Bird, // 8
    Tree, // 9
    Paw, // 10
    Butterfly, // 11
    House, // 12
    Fence, // 13
    Mushroom, // 14
    Berry, // 15
    Bloom, // 16
    Wolf, // 17
    Eagle, // 18
    Deer, // 19
    Bag, // 20
    RoadToken, // 21
    Landscape // 22
}

public enum CardStatus
{
    NONE,
    IN_HAND,
    PENDING_ON_TABLE,
    USED
}

public class Card : Interactable
{
    public CardStatus cardStatus;
    public Image highlightFrame;
    public bool isSelected;
    public bool selectedToDispose;
    public bool isDisposable;
    public bool isInspected;
    public bool canMove;
    public bool canHover;
    public bool canScale;
    public float hoverOriginY;
    public float hoverTargetY;

    private CardData _data;
    private CardIconItemsView _iconItemsView;
    private Sprite _cardFront;
    private Sprite _cardBack;
    private Sequence _hoverSequence;
    private Sequence _scaleSequence;
    private bool _canInspect;
    
    // to reset card position on invalid placement
    private int _siblingIndexInParent;
    private Vector2 _prevPosition;

    public void Create(CardData data, Sprite cardFront, Sprite cardBack)
    {
        Init();
        _data = data;
        _iconItemsView = transform.GetChild(1).GetComponent<CardIconItemsView>();
        InitIconItemsView(data);
        hoverOriginY = -55f;
        hoverTargetY = 100f;
        cardStatus = CardStatus.NONE;
        _cardFront = cardFront;
        _cardBack = cardBack;
        _mainImage = GetComponent<Image>();
        _mainImage.sprite = cardBack;
        highlightFrame = transform.GetChild(0).GetComponent<Image>();
        highlightFrame.color = Color.red;
        ToggleHighlight(false);
        ToggleRayCast(false);
        gameObject.SetActive(false);
    }

    public void InitIconItemsView(CardData data)
    {
        if(data != null)
        {
            _iconItemsView.Init(data);
        }
    }

    public CardData Data { get { return _data; } }

    public Sprite CardFront { get { return _cardFront; } }

    public Sprite CardBack { get { return _cardBack; } }

    public CardIconItemsView CardIconItemsView {  get { return _iconItemsView; } }

    public override void OnDrag(PointerEventData eventData)
    {
        if (!canMove)
        {
            return;
        }
        transform.position = Input.mousePosition;
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (!canMove)
        {
            return;
        }
        _siblingIndexInParent = transform.GetSiblingIndex();
        _prevPosition = GetComponent<RectTransform>().anchoredPosition;
        transform.SetParent(transform.root);
        ToggleRayCast(false);
        _eventController.InvokeEventHandler(GameLogicEventType.CARD_MOVED, new object[] { _data.cardType });
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (!canMove)
        {
            return;
        }

        List<RaycastResult> raycastResults = new();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        _eventController.InvokeEventHandler(GameLogicEventType.CARD_PLACED, new object[] { this, raycastResults});
    }

    public void SetCardReadyInHand()
    {
        cardStatus = CardStatus.IN_HAND;
        canHover = true;
    }

    public void MoveCardBackToHand(Transform handViewTransform)
    {
        transform.SetParent(handViewTransform);
        GetComponent<RectTransform>().anchoredPosition = _prevPosition;
        transform.SetSiblingIndex(_siblingIndexInParent);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right && !isSelected && !isDisposable)
        {
            if (cardStatus == CardStatus.PENDING_ON_TABLE)
            {
                _eventController.InvokeEventHandler(GameLogicEventType.CANCELLED_PENDING_CARD_PLACED, new object[] { this });
            }
            else if (Array.Exists(new[] { CardStatus.NONE, CardStatus.IN_HAND }, status => status == cardStatus) && _canInspect)
            {
                if(cardStatus == CardStatus.NONE)
                {
                    transform.SetParent(_parent);
                }
                _eventController.InvokeEventHandler(GameLogicEventType.CARD_INSPECTION_STARTED, new object[] { this });
            }
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if(isSelected)
            {
                OnPick();
                _eventController.InvokeEventHandler(GameLogicEventType.CARD_PICKED, new object[] { _parent.GetComponent<CardHolder>(), this });
            }
            else if(isDisposable && !isInspected)
            {
                selectedToDispose = !selectedToDispose;
                ToggleHighlight(selectedToDispose);
                _eventController.InvokeEventHandler(GameLogicEventType.CARD_SELECTED_FOR_DISPOSE, new object[0]);
            }
        }
    }

    public void OnPick()
    {
        transform.SetParent(_parent);
        ToggleCanInspectFlag(false);
        ToggleHighlight(false);
        canScale = false;
        _scaleSequence.Kill();
        transform.localScale = new Vector3(1f, 1f, 1f);
    }

    private void ToggleHighlight(bool value)
    {
        highlightFrame.gameObject.SetActive(value);
    }

    public void ResetDisposeSelection()
    {
        selectedToDispose = false;
        ToggleHighlight(false);
    }

    public void ToggleSelection(bool value)
    {
        isSelected = value;
    }

    public void ToggleCanInspectFlag(bool value)
    {
        _canInspect = value;
    }

    public void ToggleIsInspectedFlag(bool value)
    {
        isInspected = value;
    }

    public void SetParentTransform(Transform transform)
    {
        _parent = transform;
    }

    public override void ToggleRayCast(bool value)
    {
        _mainImage.raycastTarget = value;
        highlightFrame.raycastTarget = value;
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (cardStatus == CardStatus.IN_HAND && canHover)
        {
            MoveCard(hoverTargetY, 0.4f);
        }

        if (isSelected)
        {
            ToggleHighlight(true);
        }

        if (canScale)
        {
            ScaleCard(true);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (cardStatus == CardStatus.IN_HAND && canHover)
        {
            MoveCard(hoverOriginY, 0.2f);
        }

        if (isSelected)
        {
            ToggleHighlight(false);
        }

        if (canScale)
        {
            ScaleCard(false);
        }
    }

    private void ScaleCard(bool value)
    {
        if (value)
        {
            _parent = transform.parent;
            transform.SetParent(transform.root);
        }
        else
        {
            transform.SetParent(_parent);
            _scaleSequence.Kill();
        }

        float target = value ? 1.2f : 1f;
        float duration = value ? 0.5f : 0.2f;
        _scaleSequence = DOTween.Sequence();
        _scaleSequence.Append(transform.DOScale(target, duration));
    }

    public void RemoveRequirementsFromCardData(CardIconItem item)
    {
        switch(item.ItemType)
        {
            case IconItemType.SINGLE:
                _data.requirements = UpdateDataIcons(item.Icons, _data.requirements.ToList());
                break;
            case IconItemType.OPTIONAL:
                _data.optionalRequirements = UpdateDataIcons(item.Icons, _data.optionalRequirements.ToList());
                break;
            default:
                _data.adjacentRequirements = UpdateDataIcons(item.Icons, _data.adjacentRequirements.ToList());
                break;
        }
    }

    private CardIcon[] UpdateDataIcons(List<CardIcon> itemIcons, List<CardIcon> dataIcons)
    {
        int itemIconIndex = itemIcons.Count - 1;
        for (int i = dataIcons.Count - 1; i >= 0; i--)
        {
            if ((int)dataIcons[i] == (int)itemIcons[itemIconIndex])
            {
                dataIcons.RemoveAt(i);
                itemIconIndex--;
                if (itemIconIndex < 0)
                {
                    break;
                }
            }
        }
        return dataIcons.ToArray();
    }

    public void MoveCard(float endYvalue, float duration)
    {
        _hoverSequence.Kill();
        _hoverSequence = DOTween.Sequence();

        _hoverSequence.Append(transform.DOLocalMoveY(endYvalue, duration)).OnComplete(() => _hoverSequence.Kill());
    }

    public void ToggleCard()
    {
        float posY = canHover ? hoverTargetY : hoverOriginY;
        canHover = !canHover;
        canMove = !canMove;
        ToggleCanInspectFlag(canMove);
        DOTween.Sequence().Append(transform.DOLocalMoveY(posY, 0.4f));
    }

    public void DrawFromDeckTween(float posY)
    {
        float cardDrawSpeed = GameSettings.Instance.GetDuration(Duration.cardDrawSpeedFromDeck);
        gameObject.SetActive(true);
        canHover = false;
        DOTween.Sequence()
            .Append(transform.DOMoveY(posY, cardDrawSpeed)
            .SetEase(Ease.InOutQuart)
            .OnComplete(() => FlipBoardCardTween()));
    }

    public void FillBoardTween(float delay, CardHolder holder, Transform cardDrawContainer)
    {
        float cardDrawSpeed = GameSettings.Instance.GetDuration(Duration.cardDrawSpeedFromDeck);
        _parent = holder.transform;
        gameObject.SetActive(true);
        canHover = false;

        DOTween.Sequence()
            .Append(transform.DOMoveY(holder.transform.position.y, cardDrawSpeed)
            .SetEase(Ease.InOutQuart)
            .SetDelay(delay))
            .OnComplete(() =>
            {
                holder.AddToHolder(this);
                transform.SetParent(cardDrawContainer);
                FlipBoardCardTween();
            });
    }

    private void FlipBoardCardTween()
    {
        float halvedCardRotationSpeed = GameSettings.Instance.GetDuration(Duration.cardRotationSpeedOnBoard) * 0.5f;
        Sequence cardFlip = DOTween.Sequence();
        cardFlip.Append(transform.DOScale(1.1f, halvedCardRotationSpeed)).Join(transform.DORotate(new Vector3(0f, 90f, 0f), halvedCardRotationSpeed).SetEase(Ease.Linear).OnComplete(() => _mainImage.sprite = _cardFront));
        cardFlip.Append(transform.DORotate(new Vector3(0f, 0f, 0f), halvedCardRotationSpeed)).SetEase(Ease.Linear);
        cardFlip.Append(transform.DOScale(1f, 0.05f)).SetEase(Ease.Linear).OnComplete(() =>
        {
            if(_parent)
            {
                transform.SetParent(_parent);
                transform.SetAsFirstSibling();
                canScale = true;
            }
            _iconItemsView.Toggle(true);
        });
    }

    public void ClearBoardTween(Transform display, float delay)
    {
        float halvedCardRotationSpeed = GameSettings.Instance.GetDuration(Duration.cardRotationSpeedOnBoard) * 0.5f;
        float cardDrawSpeed = GameSettings.Instance.GetDuration(Duration.cardDrawSpeedFromDeck);
        transform.SetParent(display);
        _iconItemsView.Toggle(false);
        Sequence cardFlip = DOTween.Sequence();
        cardFlip.SetDelay(delay);
        cardFlip.Append(transform.DOScale(1.1f, halvedCardRotationSpeed)).Join(transform.DORotate(new Vector3(0f, 90f, 0f), halvedCardRotationSpeed).SetEase(Ease.Linear).OnComplete(() => _mainImage.sprite = _cardBack));
        cardFlip.Append(transform.DORotate(new Vector3(0f, 0f, 0f), halvedCardRotationSpeed)).SetEase(Ease.Linear);
        cardFlip.Append(transform.DOScale(1f, 0.05f)).SetEase(Ease.Linear)
            .OnComplete(() => DOTween.Sequence().Append(transform.DOMove(display.position, cardDrawSpeed).SetEase(Ease.InOutQuart)));
    }

    public void MoveToPositionTween(Vector3 position, float drawSpeed, float delay = 0f)
    {
        DOTween.Sequence().Append(GetComponent<RectTransform>().DOAnchorPos(position, drawSpeed).SetDelay(delay).SetEase(Ease.InOutBack));
    }

    public void FlipDeckCardTween(bool value)
    {
        float halvedCardRotationSpeed = GameSettings.Instance.GetDuration(Duration.cardRotationSpeedOnBoard) * 0.5f;
        Sprite sprite = value ? _cardFront : _cardBack;
        Sequence cardFlip = DOTween.Sequence();
        cardFlip.Append(transform.DOScale(1.1f, halvedCardRotationSpeed)).Join(transform.DORotate(new Vector3(0f, 90f, 0f), halvedCardRotationSpeed).SetEase(Ease.Linear).OnComplete(() => _mainImage.sprite = sprite));
        cardFlip.Append(transform.DORotate(new Vector3(0f, 0f, 0f), halvedCardRotationSpeed)).SetEase(Ease.Linear);
        cardFlip.Append(transform.DOScale(1f, 0.05f)).SetEase(Ease.Linear);
    }

    public void MoveCardHorizontallyTween(float posX, bool isRapid)
    {
        float duration = canMove ? 0.2f : 0.4f;
        Ease ease = isRapid ? Ease.InOutBack : canMove ? Ease.Linear : Ease.InOutQuad;
        DOTween.Sequence().Append(transform.DOLocalMoveX(posX, duration).SetEase(ease));
    }

    public void PositionCardTween(Vector3 position, float speed, bool isPlacement)
    {
        RectTransform rect = GetComponent<RectTransform>();
        if (Array.Exists(new CardType[] { CardType.Landscape, CardType.Discovery }, type => type == Data.cardType))
        {
            Vector3 rotation = new(0f, 0f, isPlacement ? 90f : 0f);
            _iconItemsView.Rotate(isPlacement ? -90f : 90f);
            DOTween.Sequence()
                .Append(rect.DOAnchorPos(position, speed))
                .Join(rect.DORotate(rotation, speed))
                .SetEase(Ease.InOutSine);
        }
        else
        {
            DOTween.Sequence().Append(rect.DOAnchorPos(position, speed)).SetEase(Ease.InOutSine);
        }
    }
}
