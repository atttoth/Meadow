using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

public class HandScreenHitArea : HitArea
{
    private Image _fakeCardImage;

    public void Init()
    {
        _fakeCardImage = transform.GetChild(0).GetComponent<Image>();
        _fakeCardImage.enabled = false;
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
        StartEventHandler(GameLogicEventType.HAND_SCREEN_TOGGLED, new GameTaskItemData() { value = value });
    }

    public void SetupHitAreaImage(CardData cardData)
    {
        SpriteAtlas atlas = GameAssets.Instance.GetAssetByName<SpriteAtlas>(cardData.deckType.ToString());
        _fakeCardImage.sprite = atlas.GetSprite(cardData.ID.ToString());
    }

    public void ToggleHitAreaImage(bool value)
    {
        _fakeCardImage.enabled = value;
    }

    public void Toggle(bool value)
    {
        gameObject.SetActive(value);
    }
}
