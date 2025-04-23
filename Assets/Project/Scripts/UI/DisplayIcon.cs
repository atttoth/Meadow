using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayIcon : MonoBehaviour
{
    public int ID;
    public Image background;
    public Image cardIcon;
    private List<CardIcon> _groundIcons;
    private List<CardIcon> _mainIcons;
    private CanvasGroup _canvasGroup;

    public List<CardIcon> MainIcons
    {
        get
        {
            return _mainIcons;
        }
    }

    public List<CardIcon> GroundIcons
    {
        get
        {
            return _groundIcons;
        }
    }

    public void SaveIcons(List<CardIcon> cardIcons)
    {
        _mainIcons = new();
        foreach (CardIcon cardIcon in cardIcons)
        {
            if ((int)cardIcon < 5)
            {
                _groundIcons.Add(cardIcon);
            }
            else
            {
                _mainIcons.Add(cardIcon);
            }
        }
    }

    public void SetUpDisplayIcon(int HolderID)
    {
        ID = HolderID;
        background = transform.GetChild(0).GetComponent<Image>();
        cardIcon = transform.GetChild(1).GetComponent<Image>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        _groundIcons = new();
        _mainIcons = new();
    }

    private Sprite SetIconImageByCardIcon(CardIcon cardIcon)
    {
        string name = ((int)cardIcon).ToString();
        return GameResourceManager.Instance.Base.GetSprite(name);
    }

    public Color32 GetColorByGroundIcon(CardIcon cardIcon)
    {
        switch (cardIcon)
        {
            case CardIcon.Grass:
                return new Color32(31, 255, 0, 85);
            case CardIcon.Litterfall:
                return new Color32(255, 80, 0, 85);
            case CardIcon.Sands:
                return new Color32(255, 207, 0, 85);
            case CardIcon.Rocks:
                return new Color32(0, 84, 255, 85);
            default:
                return new Color32(255, 0, 246, 85);
        }
    }

    private void ToggleIcons()
    {
        CardIcon icon = _mainIcons[0];
        _mainIcons.RemoveAt(0);
        _mainIcons.Add(icon);
        cardIcon.sprite = SetIconImageByCardIcon(icon);
    }

    private void ToggleBackgrounds()
    {
        CardIcon icon = _groundIcons[0];
        _groundIcons.RemoveAt(0);
        _groundIcons.Add(icon);
        background.color = GetColorByGroundIcon(icon);
    }

    public void PlayIconToggle()
    {
        if (cardIcon.color.a == 0)
        {
            ChangeAlpha(1);
        }
        CancelInvoke("ToggleIcons");
        InvokeRepeating("ToggleIcons", 0f, 2f);
    }

    public void PlayBackgroundToggle()
    {
        InvokeRepeating("ToggleBackgrounds", 0f, 2f);
    }

    public void SetIconImage()
    {
        if (cardIcon.color.a == 0)
        {
            ChangeAlpha(1);
        }
        cardIcon.sprite = SetIconImageByCardIcon(MainIcons[0]);
    }

    public void ChangeAlpha(int value)
    {
        Color newColor = cardIcon.color;
        newColor.a = value;
        cardIcon.color = newColor;
    }

    public void PosXTween(float posX, float speed)
    {
        DOTween.Sequence().Append(transform.DOMoveX(posX, speed).SetEase(Ease.InOutSine));
    }

    public void posYTween(float posY, float speed, bool isNewIcon)
    {
        float alpha = isNewIcon ? 1f : 0f;
        DOTween.Sequence().Append(transform.DOMoveY(posY, speed)).Join(_canvasGroup.DOFade(alpha, speed));
    }
}
