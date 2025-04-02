using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class CardInspectionScreen : MonoBehaviour
{
    private InteractableScreen _interactableScreen;
    private Card _inspectedCard;
    private Card _fakeCard;
    private RectTransform _fakeCardTransform;
    private Button _approveIconRemoveButton;
    private int _disposableIconItemID;

    public CardIconItem GetDisposedIconItem()
    {
        return _fakeCard.CardIconItemsView.GetIconItemByID(_disposableIconItemID);
    }

    public Button Init()
    {
        _interactableScreen = transform.GetChild(0).GetComponent<InteractableScreen>();
        _interactableScreen.Init();
        _fakeCard = transform.GetChild(1).GetComponent<Card>();
        _fakeCard.Init(null, null, null);
        _fakeCard.ToggleRayCast(false);
        _fakeCard.MainImage.enabled = false;
        _fakeCardTransform = _fakeCard.GetComponent<RectTransform>();
        _approveIconRemoveButton = transform.GetChild(2).GetComponent<Button>();
        ToggleIconRemoveButton(false);
        ToggleRayCast(false);
        return _approveIconRemoveButton;
    }

    public void ShowCardHandler(GameTask task, Card card, bool isTableVisible)
    {
        switch(task.State)
        {
            case 0:
                _inspectedCard = card;
                _inspectedCard.ToggleIsInspectedFlag(true);
                _approveIconRemoveButton.enabled = true;
                float duration = ReferenceManager.Instance.gameLogicController.GameSettings.cardInspectionFlipDuration;
                float quarterOfDuration = duration * 0.25f;
                Vector2 screenPosition = _interactableScreen.GetComponent<RectTransform>().anchoredPosition;
                _fakeCardTransform.localScale = Vector3.one;
                _fakeCardTransform.anchoredPosition = screenPosition;
                _fakeCardTransform.eulerAngles = new Vector3(0f, 0f, 0f);
                _fakeCard.InitIconItemsView(card.Data);
                _fakeCard.MainImage.sprite = card.CardFront;
                _interactableScreen.MainImage.enabled = true;
                _fakeCard.MainImage.enabled = true;
                _fakeCard.gameObject.SetActive(true);
                DOTween.Sequence()
                    .Append(_fakeCardTransform.DOMove(screenPosition, duration))
                    .Join(_fakeCardTransform.DOScale(2.5f, duration).SetEase(Ease.InOutSine))
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
                _fakeCard.CardIconItemsView.Toggle(true);
                task.StartDelayMs(0);
                break;
            default:
                ToggleRayCast(true);
                if(_fakeCard.CardIconItemsView.GetRequiredIconItemsNumber() > 1 && isTableVisible)
                {
                    _fakeCard.CardIconItemsView.ToggleRequiredIconsRaycast(true);
                }
                task.Complete();
                break;
        }
    }

    public void HideCardHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                ToggleIconRemoveButton(false);
                ToggleRayCast(false);
                _fakeCard.CardIconItemsView.ToggleRequiredIconsRaycast(false);
                _fakeCard.CardIconItemsView.Toggle(false);
                task.StartDelayMs(0);
                break;
            case 1:
                _fakeCard.gameObject.SetActive(false);
                _fakeCard.MainImage.enabled = false;
                _interactableScreen.MainImage.enabled = false;
                _inspectedCard.ToggleIsInspectedFlag(false);
                _inspectedCard = null;
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void RemoveIconItemHandler(GameTask task, CardIconItem item)
    {
        switch (task.State)
        {
            case 0:
                float duration = ReferenceManager.Instance.gameLogicController.GameSettings.fakeCardIconItemDelete;
                item.PlayDeleteAnimation();
                task.StartDelayMs((int)duration * 1000);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void SelectIconItemOnCard(int iconItemID)
    {
        _disposableIconItemID = iconItemID;
        _fakeCard.CardIconItemsView.UpdateDisposeStatusOfItems(iconItemID);
    }

    public void UpdateIconRemoveButtonStatus(bool hasEnoughDisposableCards)
    {
        ToggleIconRemoveButton(hasEnoughDisposableCards && _fakeCard.CardIconItemsView.HasIconItemSelectedForDispose());
    }

    private void ToggleIconRemoveButton(bool value)
    {
        _approveIconRemoveButton.gameObject.SetActive(value);
    }

    private void ToggleRayCast(bool value)
    {
        _interactableScreen.MainImage.raycastTarget = value;
    }
}
