using UnityEngine;

public enum HolderType
{
    BoardCard,
    TableCard,
    BoardMarker,
    CampMarker,
    CardIcon
}

public class Holder : MonoBehaviour
{
    protected HolderData _data;

    public virtual void Init(int id, HolderType type)
    {
        _data = new(id, type);
    }

    public HolderData Data { get { return _data; } }

    public void AddToHolder(Interactable item)
    {
        item.transform.SetParent(transform);
        _data.AddItemToContentList(item);
    }
}
