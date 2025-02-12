using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ScreenDisplayItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int ID;
    public object type;
    public Image mainImage;
    public Button button;
    public TextMeshProUGUI message;

    public void Init()
    {
        mainImage = transform.GetChild(0).GetComponent<Image>();
        button = GetComponent<Button>();
        message = transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        message.text = "";
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        if(button.enabled)
        {
            message.enabled = true;
        }
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        message.enabled = false;
    }
}
