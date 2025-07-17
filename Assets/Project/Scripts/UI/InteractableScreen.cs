using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InteractableScreen : Interactable
{
    public override void Init()
    {
        base.Init();
        _mainImage = transform.GetComponent<Image>();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _eventController.InvokeEventHandler(GameLogicEventType.CARD_INSPECTION_ENDED, new object[0]);
        }
    }
}
