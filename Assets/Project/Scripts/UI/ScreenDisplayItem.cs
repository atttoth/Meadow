using System;
using UnityEngine;
using UnityEngine.UI;

public class ScreenDisplayItem : MonoBehaviour
{
    public Enum type;
    public Image image;
    public Button button;

    public void Init()
    {
        image = transform.GetChild(0).GetComponent<Image>();
        button = GetComponent<Button>();
    }
}
