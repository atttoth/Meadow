using UnityEngine;
using UnityEngine.EventSystems;

public class TableCardHitArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public HolderSubType type;

    public void OnPointerEnter(PointerEventData eventData)
    {
        UpdateHitArea(tag);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        UpdateHitArea();
    }

    private void UpdateHitArea(string hitAreaTag = null)
    {
        PlayerController playerController = ReferenceManager.Instance.playerController;
        if ((playerController.draggingCardType == CardType.Ground && type == HolderSubType.PRIMARY) || (playerController.draggingCardType == CardType.Landscape && type == HolderSubType.SECONDARY))
        {
            playerController.UpdateActiveCardHolders(type, hitAreaTag);
        }
    }
}
