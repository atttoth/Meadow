using System.Collections.Generic;
using System.Linq;

public class PendingActionCreator
{
    public delegate void PendingActionItem(GameTaskItemData data);
    private readonly Dictionary<int, PendingActionItem[]> _actionItemsCollection;
    private readonly Dictionary<int, GameTaskItemData> _dataCollection;
    private readonly bool _canOnlyCancelLastAction;

    public PendingActionCreator(bool canOnlyCancelLastAction)
    {
        _actionItemsCollection = new();
        _dataCollection = new();
        _canOnlyCancelLastAction = canOnlyCancelLastAction;
    }

    public int GetNumOfActions()
    {
        return _actionItemsCollection.Count;
    }

    public void Create(PendingActionItem[] postActionItems, PendingActionItem[] prevActionItems, GameTaskItemData data)
    {
        int ID = _canOnlyCancelLastAction ? _actionItemsCollection.Count : data.pendingActionID;
        _actionItemsCollection.Add(ID, prevActionItems);
        _dataCollection.Add(ID, data);
        postActionItems.ToList().ForEach(item => item(data));
    }

    public bool TryCancel(GameTaskItemData data)
    {
        if (_canOnlyCancelLastAction && _dataCollection.ToList().Last().Value.pendingActionID != data.pendingActionID) // check if cancelled action was the last action
        {
            return false;
        }

        int ID = _canOnlyCancelLastAction ? _actionItemsCollection.Count - 1 : data.pendingActionID;
        PendingActionItem[] prevActionItems = _actionItemsCollection[ID];
        GameTaskItemData actionData = _dataCollection[ID];
        _actionItemsCollection.Remove(ID);
        _dataCollection.Remove(ID);
        prevActionItems.ToList().ForEach(item => item(actionData));
        return true;
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
