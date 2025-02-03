using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class CardInspectionScreen : MonoBehaviour
{
    private Image _blackOverlay;
    private RectTransform _fakeCardTransform;
    private Image _fakeCardImage;
    private Button _screenButton;

    public Button Init()
    {
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        _fakeCardTransform = transform.GetChild(1).GetComponent<RectTransform>();
        _fakeCardImage = _fakeCardTransform.GetChild(0).GetComponent<Image>();
        _fakeCardImage.enabled = false;
        _screenButton = GetComponent<Button>();
        _screenButton.enabled = false;
        return _screenButton;
    }

    public void ToggleScreenButton(bool value)
    {
        _screenButton.enabled = value;
    }

    public void ShowCard(Card card)
    {
        float duration = ReferenceManager.Instance.gameLogicManager.GameSettings.cardInspectionFlipDuration;
        float quarterOfDuration = duration * 0.25f;
        Vector2 screenPosition = _blackOverlay.GetComponent<RectTransform>().anchoredPosition;
        _fakeCardTransform.localScale = Vector3.one;
        _fakeCardTransform.anchoredPosition = screenPosition;
        _fakeCardTransform.eulerAngles = new Vector3(0f, 0f, 0f);
        _fakeCardImage.sprite = card.CardFront;
        _blackOverlay.enabled = true;
        _fakeCardImage.enabled = true;
        DOTween.Sequence()
            .Append(_fakeCardTransform.DOMove(screenPosition, duration))
            .Join(_fakeCardTransform.DOScale(3.5f, duration).SetEase(Ease.InOutSine))
            .Join(_fakeCardTransform.DOLocalRotate(new Vector3(0f, 360f, 0f), duration, RotateMode.FastBeyond360).SetRelative(true).SetEase(Ease.Linear));

        DOTween.Sequence().SetDelay(quarterOfDuration).OnComplete(() =>
        {
            _fakeCardImage.sprite = card.CardBack;
            _fakeCardImage.GetComponent<RectTransform>().localScale = new Vector3(-1f, 1f, 1f); // mirror back image
        });

        DOTween.Sequence().SetDelay(quarterOfDuration * 3).OnComplete(() =>
        {
            _fakeCardImage.sprite = card.CardFront;
            _fakeCardImage.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
        });

        if (Array.Exists(new[] { CardType.Landscape, CardType.Discovery }, cardType => cardType == card.Data.cardType))
        {
            DOTween.Sequence()
                .SetDelay(quarterOfDuration * 3)
                .Append(_fakeCardTransform.DORotate(new Vector3(0f, 0f, 90f), quarterOfDuration));
        }
    }

    public void HideCard()
    {
        ToggleScreenButton(false);
        _fakeCardImage.enabled = false;
        _blackOverlay.enabled = false;
    }
}
