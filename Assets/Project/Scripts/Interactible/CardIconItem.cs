using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
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
    private List<CardIcon> _icons;
    private Image _raycastTargetImage;
    private TextMeshProUGUI _scoreText;

    public List<CardIcon> Icons {  get { return _icons; } }

    public void Create(List<CardIcon> icons, IconItemType itemType, float iconDimension, int score = 0)
    {
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        RectTransform iconsParentRect = transform.GetChild((int)itemType).GetComponent<RectTransform>();
        _raycastTargetImage = iconsParentRect.GetComponent<Image>();

        if(itemType == IconItemType.SCORE)
        {
            _scoreText = iconsParentRect.GetChild(0).GetComponent<TextMeshProUGUI>();
            _scoreText.text = score.ToString();
        }
        else
        {
            _icons = icons;
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

    public override void ToggleRayCast(bool value)
    {
        _raycastTargetImage.raycastTarget = value;
    }
}
