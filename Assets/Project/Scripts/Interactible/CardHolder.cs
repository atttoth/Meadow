using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Threading.Tasks;
using System.Reflection;

public class CardHolder : Holder
{
    public GameObject highlightFrame;
    protected Image _blackOverlay;

    public override void Init(int id, HolderType type)
    {
        base.Init(id, type);
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
    }

    public List<CardIcon> GetAllIconsOfHolder()
    {
        List<CardIcon> allIcons = ((Card)GetItemFromContentListByIndex(_contentList.Count - 1)).Data.icons.ToList();
        if (_contentList.Count > 1)
        {
            CardIcon[] groundIcons = ((Card)GetItemFromContentListByIndex(0)).Data.icons;

            foreach (CardIcon cardIcon in groundIcons)
            {
                if ((int)cardIcon < 5)
                {
                    allIcons.Add(cardIcon);
                }
            }
        }
        
        return allIcons;
    }

    public void EnableOverlay(bool value)
    {
        _blackOverlay.transform.SetAsLastSibling();
        if (_blackOverlay.enabled != value)
        {
            _blackOverlay.enabled = value;
        }
    }
}
