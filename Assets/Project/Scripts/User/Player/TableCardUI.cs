using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class TableCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public void OnPointerEnter(PointerEventData eventData)
    {
        PlayerManager playerManager = ReferenceManager.Instance.playerManager;
        if (playerManager.Controller.GetHandView().draggingCardType == CardType.Ground)
        {
            playerManager.Controller.UpdateTableCardUI(tag);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        PlayerManager playerManager = ReferenceManager.Instance.playerManager;
        if (playerManager.Controller.GetHandView().draggingCardType == CardType.Ground)
        {
            playerManager.Controller.UpdateTableCardUI();
        }
    }
}
