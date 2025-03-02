using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CardIconRemoveScreen : MonoBehaviour
{
    private Image _blackOverlay;

    public void Init()
    {
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
    }
}
