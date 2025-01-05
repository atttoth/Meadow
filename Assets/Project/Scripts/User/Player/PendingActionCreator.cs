using System.Collections.Generic;
using System.Linq;

public class PendingActionCreator
{
    public delegate void PendingActionItem(GameTaskItemData data);
    private Dictionary<int, PendingActionItem[]> _actionItemsCollection = new();
    private Dictionary<int, GameTaskItemData> _dataCollection = new();

    public int GetNumOfActions()
    {
        return _actionItemsCollection.Count;
    }

    public void Create(PendingActionItem[] postActionItems, PendingActionItem[] prevActionItems, GameTaskItemData data)
    {
        int ID = data.pendingCardDataID;
        _actionItemsCollection.Add(ID, prevActionItems);
        _dataCollection.Add(ID, data);
        postActionItems.ToList().ForEach(item => item(data));
    }

    public void Cancel(GameTaskItemData data)
    {
        int ID = data.pendingCardDataID;
        PendingActionItem[] prevActionItems = _actionItemsCollection[ID];
        GameTaskItemData actionData = _dataCollection[ID];
        _actionItemsCollection.Remove(ID);
        _dataCollection.Remove(ID);
        prevActionItems.ToList().ForEach(item => item(actionData));
    }

    public List<GameTaskItemData> GetDataCollection()
    {
        return _dataCollection.Values.ToList();
    }

    public void Dispose()
    {
        _actionItemsCollection.Clear();
        _dataCollection.Clear();
    }
}
