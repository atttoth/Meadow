using System.Collections.Generic;
using System.Linq;

public class PendingActionCreator
{
    public delegate void PendingActionItem(object[] args);
    private readonly Dictionary<int, PendingActionItem[]> _actionItemsCollection;
    private readonly Dictionary<int, object[]> _dataCollection; // index 0 in array should always represent pendingActionID
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

    public void Create(PendingActionItem[] postActionItems, PendingActionItem[] prevActionItems, params object[] args)
    {
        int ID = _canOnlyCancelLastAction ? _actionItemsCollection.Count : (int)args[0];
        _actionItemsCollection.Add(ID, prevActionItems);
        _dataCollection.Add(ID, args);
        postActionItems.ToList().ForEach(item => item(args));
    }

    public bool TryCancel(int pendingActionID)
    {
        if (_canOnlyCancelLastAction && (int)_dataCollection.ToList().Last().Value[0] != pendingActionID) // check if cancelled action was the last action
        {
            return false;
        }

        int ID = _canOnlyCancelLastAction ? _actionItemsCollection.Count - 1 : pendingActionID;
        PendingActionItem[] prevActionItems = _actionItemsCollection[ID];
        object[] args = _dataCollection[ID];
        _actionItemsCollection.Remove(ID);
        _dataCollection.Remove(ID);
        prevActionItems.ToList().ForEach(item => item(args));
        return true;
    }

    public List<object[]> GetDataCollection()
    {
        return _dataCollection.Values.ToList();
    }

    public void Dispose()
    {
        _actionItemsCollection.Clear();
        _dataCollection.Clear();
    }
}
