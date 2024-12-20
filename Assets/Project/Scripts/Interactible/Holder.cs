using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum HolderType
{
    BoardCard,
    TableCard,
    BoardMarker,
    CampMarker
}

public class Holder : MonoBehaviour
{
    [HideInInspector] public HolderType holderType;
    public int ID; // used at BoardMarker, BoardCard to link holder with corresponding display icon
    public List<Interactable> contentList;

    public bool IsEmpty() => contentList.Count < 1;

    public void AddToContentList(Interactable item)
    {
        item.GetComponent<Interactable>().parent = transform;
        item.transform.SetParent(transform);
        contentList.Add(item);
    }

    public Interactable GetItemFromContentListByIndex(int index)
    {
        if (contentList.Count > 0)
        {
            return contentList[index];
        }
        return default;
    }

    public void RemoveItemFromContentList(Interactable item)
    {
        contentList.Remove(item);
    }
}
