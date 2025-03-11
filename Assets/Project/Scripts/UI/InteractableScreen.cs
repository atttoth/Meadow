using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractableScreen : Interactable
{
    public void Init()
    {
        _mainImage = transform.GetComponent<Image>();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            StartEventHandler(GameLogicEventType.CARD_INSPECTION_ENDED, new object[0]);
        }
    }
}
