using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class CardInspectionScreen : MonoBehaviour
{
    private Image _blackOverlay;
    private Card _fakeCard;
    private RectTransform _fakeCardTransform;
    private Button _screenButton;

    public Button Init()
    {
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        _fakeCard = transform.GetChild(1).GetComponent<Card>();
        _fakeCard.Init(null, null, null);
        _fakeCard.ToggleRayCast(false);
        _fakeCard.MainImage.enabled = false;
        _fakeCardTransform = _fakeCard.GetComponent<RectTransform>();
        _screenButton = GetComponent<Button>();
        _screenButton.enabled = false;
        return _screenButton;
    }

    public void ShowCardHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                Card card = task.Data.card;
                float duration = ReferenceManager.Instance.gameLogicManager.GameSettings.cardInspectionFlipDuration;
                float quarterOfDuration = duration * 0.25f;
                Vector2 screenPosition = _blackOverlay.GetComponent<RectTransform>().anchoredPosition;
                _fakeCardTransform.localScale = Vector3.one;
                _fakeCardTransform.anchoredPosition = screenPosition;
                _fakeCardTransform.eulerAngles = new Vector3(0f, 0f, 0f);
                _fakeCard.InitIconItemsView(card.Data);
                _fakeCard.MainImage.sprite = card.CardFront;
                _blackOverlay.enabled = true;
                _fakeCard.MainImage.enabled = true;
                _fakeCard.gameObject.SetActive(true);
                DOTween.Sequence()
                    .Append(_fakeCardTransform.DOMove(screenPosition, duration))
                    .Join(_fakeCardTransform.DOScale(3.5f, duration).SetEase(Ease.InOutSine))
                    .Join(_fakeCardTransform.DOLocalRotate(new Vector3(0f, 360f, 0f), duration, RotateMode.FastBeyond360).SetRelative(true).SetEase(Ease.Linear));

                DOTween.Sequence().SetDelay(quarterOfDuration).OnComplete(() =>
                {
                    _fakeCard.MainImage.sprite = card.CardBack;
                    _fakeCard.MainImage.GetComponent<RectTransform>().localScale = new Vector3(-1f, 1f, 1f); // mirror back image
                });

                DOTween.Sequence().SetDelay(quarterOfDuration * 3).OnComplete(() =>
                {
                    _fakeCard.MainImage.sprite = card.CardFront;
                    _fakeCard.MainImage.GetComponent<RectTransform>().localScale = new Vector3(1f, 1f, 1f);
                });

                if (Array.Exists(new[] { CardType.Landscape, CardType.Discovery }, cardType => cardType == card.Data.cardType))
                {
                    DOTween.Sequence()
                        .SetDelay(quarterOfDuration * 3)
                        .Append(_fakeCardTransform.DORotate(new Vector3(0f, 0f, 90f), quarterOfDuration));
                }
                task.StartDelayMs((int)duration * 1000);
                break;
            case 1:
                _fakeCard.ToggleIcons(true);
                task.StartDelayMs(0);
                break;
            default:
                ToggleScreenButton(true);
                task.Complete();
                break;
        }
    }

    public void HideCardHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                ToggleScreenButton(false);
                _fakeCard.ToggleIcons(false);
                task.StartDelayMs(0);
                break;
            case 1:
                _fakeCard.gameObject.SetActive(false);
                _fakeCard.MainImage.enabled = false;
                _blackOverlay.enabled = false;
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void ToggleScreenButton(bool value)
    {
        _screenButton.enabled = value;
    }
}
