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
    public int ID;
    protected List<Interactable> _contentList;

    public virtual void Init(int id, HolderType type)
    {
        if (id != -1)
        {
            ID = id;
        }
        holderType = type;
        _contentList = new();
    }

    public bool IsEmpty() => _contentList.Count < 1;

    public int GetContentListSize()
    {
        return _contentList.Count;
    }

    public void AddToContentList(Interactable item)
    {
        item.transform.SetParent(transform);
        _contentList.Add(item);
    }

    public Interactable GetItemFromContentListByIndex(int index)
    {
        if (_contentList.Count > 0)
        {
            return _contentList[index];
        }
        return default;
    }

    public void RemoveItemFromContentList(Interactable item)
    {
        _contentList.Remove(item);
    }

    public List<Interactable> GetAllContent()
    {
        return _contentList;
    }
}
