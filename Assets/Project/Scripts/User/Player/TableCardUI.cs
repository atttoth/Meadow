using UnityEngine;
using UnityEngine.EventSystems;

public class TableCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayerController playerController = ReferenceManager.Instance.playerController;
        if (playerController.draggingCardType == CardType.Ground)
        {
            playerController.UpdateTableCardUI(tag);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayerController playerController = ReferenceManager.Instance.playerController;
        if (playerController.draggingCardType == CardType.Ground)
        {
            playerController.UpdateTableCardUI();
        }
    }
}
