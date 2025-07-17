using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;
using DG.Tweening;
using static UnityEditor.Progress;

public enum IconItemType
{
    SINGLE,
    OPTIONAL,
    ADJACENT,
    OPTIONAL_AND_ADJACENT,
    SCORE
}

public class CardIconItem : Interactable
{
    public int ID;
    public bool selectedToDispose;
    private List<CardIcon> _icons;
    private IconItemType _itemType;
    private Image _raycastTargetImage;
    private Sequence _zoomSequence;

    public List<CardIcon> Icons {  get { return _icons; } }

    public IconItemType ItemType { get { return _itemType; } }

    public void Create(List<CardIcon> icons, IconItemType itemType, float iconDimension, int itemID, int score = 0)
    {
        Init();
        ID = itemID;
        SpriteAtlas atlas = GameResourceManager.Instance.Base;
        RectTransform iconsParentRect = transform.GetChild((int)itemType).GetComponent<RectTransform>();
        _raycastTargetImage = iconsParentRect.GetComponent<Image>();

        if (itemType == IconItemType.SCORE)
        {
            Image scoreImage = iconsParentRect.GetChild(0).GetComponent<Image>();
            scoreImage.sprite = atlas.GetSprite("score_" + score.ToString());
        }
        else
        {
            _icons = icons;
            _itemType = itemType;
            _raycastTargetImage.sprite = atlas.GetSprite(GetBackgroundSpriteNameOfType());
            Image icon1 = iconsParentRect.GetChild(0).GetComponent<Image>();
            icon1.GetComponent<RectTransform>().sizeDelta = new(iconDimension, iconDimension);
            icon1.sprite = atlas.GetSprite(((int)icons[0]).ToString());

            if (Array.Exists(new[] { IconItemType.OPTIONAL, IconItemType.OPTIONAL_AND_ADJACENT }, iconItemType => iconItemType == itemType))
            {
                Image icon2 = iconsParentRect.GetChild(1).GetComponent<Image>();
                icon2.GetComponent<RectTransform>().sizeDelta = new(iconDimension, iconDimension);
                icon2.sprite = atlas.GetSprite(((int)icons[1]).ToString());
            }
        }
        iconsParentRect.gameObject.SetActive(true);
    }

    private string GetBackgroundSpriteNameOfType()
    {
        switch(_itemType)
        {
            case IconItemType.OPTIONAL: return "bg_optional";
            case IconItemType.ADJACENT: return "bg_adjacent";
            case IconItemType.OPTIONAL_AND_ADJACENT: return "bg_optionalAndAdjacent";
            default: return "bg_single";
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _eventController.InvokeEventHandler(GameLogicEventType.CARD_ICON_CLICKED, new object[] { ID });
        }
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        ZoomItem(true);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        ZoomItem(false);
    }

    private void ZoomItem(bool value)
    {
        if (!value)
        {
            _zoomSequence.Kill();
        }

        float target = value ? 1.2f : 1f;
        float duration = value ? 0.5f : 0.2f;
        _zoomSequence = DOTween.Sequence();
        _zoomSequence.Append(transform.DOScale(target, duration));
    }

    public void PlayDeleteAnimation()
    {
        // show delete anim on icon item
    }

    public override void ToggleRayCast(bool value)
    {
        _raycastTargetImage.raycastTarget = value;
    }
}
