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
    public Image backgroundImage; //used at BoardCard, TableCard
    public Image blackOverlay; //used only at boardCard type

    public List<CardIcon> GetAllIconsOfHolder()
    {
        List<CardIcon> allIcons = ((Card)GetItemFromContentListByIndex(contentList.Count - 1)).Data.icons.ToList();
        if (contentList.Count > 1)
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
        if(blackOverlay.enabled != value)
        {
            blackOverlay.enabled = value;
        }
    }
}
