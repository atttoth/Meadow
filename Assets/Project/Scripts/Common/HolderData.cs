using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class HolderData
{
    [HideInInspector] public int ID;
    [HideInInspector] public HolderType holderType;
    [HideInInspector] public HolderSubType holderSubType;
    private List<Interactable> _contentList;

    public HolderData(int id, HolderType type)
    {
        ID = id;
        _contentList = new();
        holderType = type;
    }

    public List<Interactable> ContentList { get { return _contentList; } set { _contentList = value; } }

    public bool IsEmpty() => _contentList.Count < 1;

    public Interactable GetItemFromContentListByIndex(int index)
    {
        if (_contentList.Count > 0)
        {
            return _contentList[index];
        }
        return default;
    }

    public void AddItemToContentList(Interactable item)
    {
        _contentList.Add(item);
    }

    public void RemoveItemFromContentList(Interactable item)
    {
        _contentList.Remove(item);
    }

    public bool IsTopCardOfHolder(Card item)
    {
        return _contentList.IndexOf(item) == _contentList.Count - 1;
    }

    public List<CardIcon> GetAllIconsOfHolder()
    {
        List<CardIcon> allIcons = ((Card)GetItemFromContentListByIndex(_contentList.Count - 1)).Data.icons.ToList();
        if (_contentList.Count > 1 && holderSubType == HolderSubType.PRIMARY)
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
}
