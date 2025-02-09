using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
    Landscape, // 22
    AllMatching, // 23
    AllDifferent // 24
}

public enum CardStatus
{
    NONE,
    IN_HAND,
    PENDING_ON_TABLE,
    STACKED_PENDING_ON_TABLE,
    USED
}

public class Card : Interactable
{
    public CardStatus cardStatus;
    public Image highlightFrame; // refactor this
    public bool isSelected;
    public bool canMove;
    public bool canHover;
    public float hoverOriginY;
    public float hoverTargetY;

    private CardData _data;
    private Sprite _cardFront;
    private Sprite _cardBack;
    private Sequence _hoverSequence;
    private Sequence _zoomSequence;
    private bool _canInspect;
    
    // reset card position on cancelled card placement
    private int _siblingIndexInParent;
    private Vector2 _prevPosition;

    public void Init(CardData data, Sprite cardFront, Sprite cardBack)
    {
        ID = data.ID;
        _data = data;
        hoverOriginY = -55f;
        hoverTargetY = 100f;
        cardStatus = CardStatus.NONE;
        _cardFront = cardFront;
        _cardBack = cardBack;
        _mainImage = GetComponent<Image>();
        _mainImage.sprite = cardBack;
        highlightFrame = transform.GetChild(0).GetComponent<Image>();
        highlightFrame.color = Color.green;
        highlightFrame.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }

    public CardData Data { get { return _data; } }

    public Sprite CardFront { get { return _cardFront; } }

    public Sprite CardBack { get { return _cardBack; } }

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
        StartEventHandler(GameLogicEventType.CARD_MOVED, new GameTaskItemData() { card = this });
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if (!canMove)
        {
            return;
        }

        List<RaycastResult> raycastResults = new();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        StartEventHandler(GameLogicEventType.CARD_PLACED, new GameTaskItemData() { raycastResults = raycastResults, card = this });
    }

    public void SetCardReadyInHand()
    {
        cardStatus = CardStatus.IN_HAND;
        canHover = true;
        ToggleRayCast(true);
    }

    public void MoveCardBackToHand(Transform handViewTransform)
    {
        transform.SetParent(handViewTransform);
        GetComponent<RectTransform>().anchoredPosition = _prevPosition;
        transform.SetSiblingIndex(_siblingIndexInParent);
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (cardStatus == CardStatus.PENDING_ON_TABLE)
            {
                StartEventHandler(GameLogicEventType.CANCELLED_PENDING_CARD_PLACED, new GameTaskItemData() { pendingCardDataID = Data.ID, card = this });
            }
            else if (Array.Exists(new[] { CardStatus.NONE, CardStatus.IN_HAND }, status => status == cardStatus))
            {
                if(cardStatus == CardStatus.NONE)
                {
                    transform.SetParent(_parent);
                }
                StartEventHandler(GameLogicEventType.CARD_INSPECTION_STARTED, new GameTaskItemData() { card = this });
            }
        }

        if (eventData.button == PointerEventData.InputButton.Left && isSelected)
        {
            transform.SetParent(_parent);
            _canInspect = false;
            highlightFrame.gameObject.SetActive(false);
            highlightFrame.color = Color.green;
            _zoomSequence.Kill();
            transform.localScale = new Vector3(1f, 1f, 1f);
            StartEventHandler(GameLogicEventType.CARD_PICKED, new GameTaskItemData() { card = this, holder = _parent.GetComponent<CardHolder>() });
        }
    }

    public void HighlightCard(bool value)
    {
        highlightFrame.gameObject.SetActive(value);
    }

    public void ToggleSelection(bool value)
    {
        isSelected = value;
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

        if (Array.Exists(new[] { CardStatus.IN_HAND, CardStatus.PENDING_ON_TABLE }, status => status == cardStatus) && !canHover)
        {
            HighlightCard(true);
        }

        if (isSelected)
        {
            HighlightCard(true);
            highlightFrame.GetComponent<Image>().color = Color.red;
        }

        if (_canInspect)
        {
            ZoomCard(true);
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (cardStatus == CardStatus.IN_HAND && canHover)
        {
            MoveCard(hoverOriginY, 0.2f);
        }

        if (Array.Exists(new[] { CardStatus.IN_HAND, CardStatus.PENDING_ON_TABLE, CardStatus.STACKED_PENDING_ON_TABLE }, status => status == cardStatus) && !canHover)
        {
            HighlightCard(false);
        }

        if (isSelected)
        {
            HighlightCard(false);
        }

        if (_canInspect)
        {
            ZoomCard(false);
        }
    }

    private void ZoomCard(bool value)
    {
        if (!value)
        {
            transform.SetParent(_parent);
            _zoomSequence.Kill();
        }

        if (value)
        {
            _parent = transform.parent;
            transform.SetParent(transform.root);
        }

        float target = value ? 1.2f : 1f;
        float duration = value ? 0.5f : 0.2f;
        _zoomSequence = DOTween.Sequence();

        _zoomSequence.Append(transform.DOScale(target, duration));
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
        DOTween.Sequence().Append(transform.DOLocalMoveY(posY, 0.4f));
    }

    public void PlayDrawingAnimation(float delay, CardHolder holder, Transform cardDrawContainer)
    {
        float cardDrawSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardDrawSpeedFromDeck;
        _parent = holder.transform;
        gameObject.SetActive(true);
        canHover = false;

        DOTween.Sequence()
            .Append(transform.DOMoveY(holder.transform.position.y, cardDrawSpeed)
            .SetEase(Ease.InOutQuart)
            .SetDelay(delay))
            .OnComplete(() =>
            {
                holder.AddToContentList(this);
                FlipBoardCard(cardDrawContainer);
            });
    }

    private void FlipBoardCard(Transform cardDrawContainer)
    {
        float halvedCardRotationSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardRotationSpeedOnBoard * 0.5f;
        transform.SetParent(cardDrawContainer);
        Sequence cardFlip = DOTween.Sequence();
        cardFlip.Append(transform.DOScale(1.1f, halvedCardRotationSpeed)).Join(transform.DORotate(new Vector3(0f, 90f, 0f), halvedCardRotationSpeed).SetEase(Ease.Linear).OnComplete(() => _mainImage.sprite = _cardFront));
        cardFlip.Append(transform.DORotate(new Vector3(0f, 0f, 0f), halvedCardRotationSpeed)).SetEase(Ease.Linear);
        cardFlip.Append(transform.DOScale(1f, 0.05f)).SetEase(Ease.Linear).OnComplete(() =>
        {
            transform.SetParent(_parent);
            transform.SetAsFirstSibling();
            _canInspect = true;
        });
    }

    public void FlipDeckCard(bool isReset = false)
    {
        float halvedCardRotationSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardRotationSpeedOnBoard * 0.5f;
        Sprite sprite = isReset ? _cardBack : _cardFront;
        Sequence cardFlip = DOTween.Sequence();
        cardFlip.Append(transform.DOScale(1.1f, halvedCardRotationSpeed)).Join(transform.DORotate(new Vector3(0f, 90f, 0f), halvedCardRotationSpeed).SetEase(Ease.Linear).OnComplete(() => _mainImage.sprite = sprite));
        cardFlip.Append(transform.DORotate(new Vector3(0f, 0f, 0f), halvedCardRotationSpeed)).SetEase(Ease.Linear);
        cardFlip.Append(transform.DOScale(1f, 0.05f)).SetEase(Ease.Linear);
    }

    public void MoveCardHorizontally(float posX, bool isRapid)
    {
        float duration = canMove ? 0.2f : 0.4f;
        Ease ease = isRapid ? Ease.InOutBack : canMove ? Ease.Linear : Ease.InOutQuad;
        DOTween.Sequence().Append(transform.DOLocalMoveX(posX, duration).SetEase(ease));
    }
}
