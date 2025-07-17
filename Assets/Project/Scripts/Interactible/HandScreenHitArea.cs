using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;

public class HandScreenHitArea : HitArea
{
    private Card _fakeCard;

    public override void Init()
    {
        base.Init();
        _fakeCard = Instantiate(GameResourceManager.Instance.cardPrefab, transform).GetComponent<Card>();
        _fakeCard.GetComponent<RectTransform>().anchoredPosition = new(0f, 105f);
        EnableFakeCard(null);
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        ToggleHandScreen(true);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        ToggleHandScreen(false);
    }

    private void ToggleHandScreen(bool value)
    {
        _eventController.InvokeEventHandler(GameLogicEventType.HAND_SCREEN_TOGGLED, new object[] { value });
    }

    public void EnableFakeCard(CardData cardData)
    {
        bool value = cardData is not null;
        if(value)
        {
            SpriteAtlas atlas = GameResourceManager.Instance.GetAssetByName<SpriteAtlas>(cardData.deckType.ToString());
            Sprite cardFront = atlas.GetSprite(cardData.ID.ToString());
            _fakeCard.Create(cardData, cardFront, null);
            _fakeCard.ToggleRayCast(false);
            _fakeCard.MainImage.sprite = cardFront;
            _fakeCard.CardIconItemsView.Toggle(true);
        }
        _fakeCard.gameObject.SetActive(value);
    }
}
