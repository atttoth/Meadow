using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.WSA;

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
    Road, // 21
    Landscape // 22
}

public enum CardLocationStatus
{
    DEFAULT,
    IN_HAND,
    PENDING_ON_TABLE,
    STACKED_PENDING_ON_TABLE
}

public class Card : Interactable
{
    private CardData _data;
    public int siblingIndexInParent;
    public Image highlightFrame;
    public GameObject drawText;
    public Sprite cardFront;
    public Sprite cardBack;
    public CardLocationStatus cardLocationStatus;
    public bool canZoom;
    public bool isSelected;
    public bool isDragging;
    public bool canMove;
    public bool canHover;
    public int holderColIndex; // once card was drawn from board, the replacement requires the index of col
    public float originXInParent;
    public float hoverOriginY;
    public float hoverTargetY;
    public Vector2 prevAnchoredPosition; // for pending action

    Sequence hoverSequence;
    Sequence zoomSequence;
    Sequence cardDrawing;
    Sequence cardFlip;

    public CardData Data
    {
        get
        {
            return _data;
        }
        set
        {
            _data = value;
        }
    }

    public void ShowDetails()
    {
        Debug.Log(_data.ID);
        Debug.Log(_data.cardType);
    }

    public override void OnDrag(PointerEventData eventData)
    {
        if(!canMove)
        {
            return;
        }
        transform.position = Input.mousePosition;
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if(!canMove)
        {
            return;
        }
        
        transform.SetParent(transform.root);
        ToggleRayCast(false);
        PlayerManager playerManager = ReferenceManager.Instance.playerManager;
        playerManager.Controller.GetHandView().draggingCardType = _data.cardType;
        if (!playerManager.Controller.IsTableVisible())
        {
            playerManager.Controller.GetTableView().TogglePanel();
            playerManager.Controller.GetHandView().ToggleHand(this);
            HighlightCard(true);
        }

        bool val = _data.cardType == CardType.Ground && playerManager.Controller.GetTableView().GetActiveCardHoldersAmount() < 10;
        playerManager.Controller.GetTableView().ToggleUIHitArea(val);
    }

    public override void OnEndDrag(PointerEventData eventData)
    {
        if(!canMove)
        {
            return;
        }

        PlayerManager playerManager = ReferenceManager.Instance.playerManager;
        var raycastResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, raycastResults);
        if (raycastResults.Count < 1) // card dropped outside of table
        {
            MoveCardBackToHand();
        }
        else
        {
            foreach (RaycastResult result in raycastResults)
            {
                CardHolder holder = result.gameObject.GetComponent<CardHolder>();
                TableCardUI uiRect = result.gameObject.GetComponent<TableCardUI>();
                holder = uiRect ? playerManager.Controller.GetLatestTableCardHolderByTag(uiRect.tag) : holder;
                if (holder && holder.holderType == HolderType.TableCard && ReferenceManager.Instance.gameLogicManager.CanCardBePlaced(holder, this))
                {
                    StartEventHandler(GameEventType.CARD_PLACED, new GameTaskItemData() { pendingCardDataID = Data.ID, card = this, cardHolder = holder });
                    break;
                }
                else
                {
                    MoveCardBackToHand();
                }
            }
        }
        ToggleRayCast(true);
        playerManager.Controller.GetTableView().ToggleUIHitArea(false);
        playerManager.Controller.GetHandView().draggingCardType = CardType.None;
    }

    public void SavePosition(float posX)
    {
        originXInParent = posX;
        siblingIndexInParent = transform.GetSiblingIndex();
    }

    public void SetCardReadyInHand()
    {
        cardLocationStatus = CardLocationStatus.IN_HAND;
        canMove = true;
        canHover = true;
    }

    private void MoveCardBackToHand()
    {
        PlayerController playerController = ReferenceManager.Instance.playerManager.Controller;
        transform.SetParent(playerController.GetHandView().transform);
        GetComponent<RectTransform>().anchoredPosition = new(originXInParent, hoverTargetY);
        transform.SetSiblingIndex(siblingIndexInParent);
        canHover = !playerController.IsTableVisible();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (cardLocationStatus == CardLocationStatus.PENDING_ON_TABLE)
            {
                StartEventHandler(GameEventType.CANCELLED_PENDING_CARD_PLACED, new GameTaskItemData() { pendingCardDataID = Data.ID, card = this });
            }
            else
            {
                ExamineCard();
                //ShowDetails();
            }
        }

        if (eventData.button == PointerEventData.InputButton.Left && isSelected)
        {
            BoardManager boardManager = ReferenceManager.Instance.boardManager;
            boardManager.DeSelectCards();
            zoomSequence.Kill();
            transform.localScale = new Vector3(1f, 1f, 1f);
            boardManager.FindParentOfCard(_data.ID);
            StartEventHandler(GameEventType.CARD_PICKED, new GameTaskItemData() { card = this });
        }
    }

    private void ExamineCard()
    {
        transform.SetParent(parent);
        StartEventHandler(GameEventType.CARD_EXAMINED, new GameTaskItemData() { 
            sprite = GetComponent<Image>().sprite, 
            needToRotate = _data.cardType == CardType.Landscape || _data.cardType == CardType.Discovery,
            dummyType = DummyType.CARD
        });
    }

    public void HighlightCard(bool value)
    {
        highlightFrame.gameObject.SetActive(value);
    }

    public void Select()
    {
        isSelected = true;
    }

    public override void ToggleRayCast(bool value)
    {
        mainImage.raycastTarget = value;
        highlightFrame.raycastTarget = value;
    }

    public override void OnPointerEnter(PointerEventData eventData) => OnHoverEnter();

    public override void OnPointerExit(PointerEventData eventData) => OnHoverExit();

    private void OnHoverEnter()
    {

        if (cardLocationStatus == CardLocationStatus.IN_HAND && canHover)
        {
            MoveCard(hoverTargetY, 0.4f);
        }

        if(Array.Exists(new[] { CardLocationStatus.IN_HAND, CardLocationStatus.PENDING_ON_TABLE }, value => value == cardLocationStatus) && !canHover)
        {
            HighlightCard(true);
        }

        if (isSelected)
        {
            HighlightCard(true);
            highlightFrame.GetComponent<Image>().color = Color.red;
            drawText.SetActive(true);
        }

        if(canZoom)
        {
            ZoomCard(true);
        }
    }

    private void OnHoverExit()
    {

        if (cardLocationStatus == CardLocationStatus.IN_HAND && canHover)
        {
            MoveCard(hoverOriginY, 0.2f);
        }

        if (Array.Exists(new[] { CardLocationStatus.IN_HAND, CardLocationStatus.PENDING_ON_TABLE, CardLocationStatus.STACKED_PENDING_ON_TABLE }, value => value == cardLocationStatus) && !canHover)
        {
            HighlightCard(false);
        }

        if (isSelected)
        {
            HighlightCard(false);
            drawText.SetActive(false);
        }

        if(canZoom)
        {
            ZoomCard(false);
        }
    }

    private void ZoomCard(bool value)
    {
        if(!value)
        {
            transform.SetParent(parent);
            zoomSequence.Kill();
        }

        if (value)
        {
            parent = transform.parent;
            transform.SetParent(transform.root);
        }

        float target = value ? 1.2f : 1f;
        float duration = value ? 0.5f : 0.2f;
        zoomSequence = DOTween.Sequence();

        zoomSequence.Append(transform.DOScale(target, duration));
    }

    public void MoveCard(float endYvalue, float duration)
    {
        hoverSequence.Kill();
        hoverSequence = DOTween.Sequence();

        hoverSequence.Append(transform.DOLocalMoveY(endYvalue, duration)).OnComplete(() => hoverSequence.Kill());
    }

    public async Task MoveCardWithAsyncDelay(float endYvalue)
    {
        float delay = Time.time + 0.3f;
        hoverSequence.Kill();
        hoverSequence = DOTween.Sequence();

        hoverSequence.Append(transform.DOLocalMoveY(endYvalue, 0.4f)).OnComplete(() => hoverSequence.Kill());

        while (Time.time < delay)
        {
            prevAnchoredPosition = GetComponent<RectTransform>().anchoredPosition;
            await Task.Yield();
        }
    }

    public void PlayDrawingAnimation(float delay, CardHolder holder)
    {
        float cardDrawSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardDrawSpeedFromDeck;
        gameObject.SetActive(true);
        canMove = false;
        canHover = false;
        canZoom = true;

        cardDrawing = DOTween.Sequence();
        cardDrawing.Append(transform.DOMoveY(holder.transform.position.y, cardDrawSpeed).SetEase(Ease.InOutQuart).SetDelay(delay));
        cardDrawing.OnComplete(() => 
        {
            holder.AddToContentList(this);
            FlipCard();
        });
    }

    public void FlipCard()
    {
        float halvedCardRotationSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardRotationSpeedOnBoard * 0.5f;
        transform.SetParent(transform.root); // because next card should be above prev card
        cardFlip = DOTween.Sequence();
        cardFlip.Append(transform.DOScale(1.1f, halvedCardRotationSpeed)).Join(transform.DORotate(new Vector3(0f, 90f, 0f), halvedCardRotationSpeed).SetEase(Ease.Linear).OnComplete(() => mainImage.sprite = cardFront));
        cardFlip.Append(transform.DORotate(new Vector3(0f, 0f, 0f), halvedCardRotationSpeed)).SetEase(Ease.Linear);
        cardFlip.Append(transform.DOScale(1f, 0.05f)).SetEase(Ease.Linear);
        cardFlip.OnComplete(() =>
        {
            transform.SetParent(parent);
            transform.SetAsFirstSibling();
        });
    }

    public void MoveCardHorizontally(float endPosition, bool isTableToggled = false)
    {
        float duration = isTableToggled ? 0.2f : 0.4f;
        Sequence sequence = DOTween.Sequence();
        Ease ease = isTableToggled ? Ease.Linear : Ease.InOutBack;
        sequence.Append(transform.DOLocalMoveX(endPosition, duration).SetEase(ease));
    }
}
