using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PendingActionCreator
{
    public delegate void PendingActionFunction(object[] args);
    private readonly Dictionary<int, PendingActionFunction[]> _actionFunctionsCollection;
    private readonly Dictionary<int, object[]> _dataCollection; // in object array index 0 represents pendingActionID, index 1 represents isActionCancelled flag
    private readonly bool _cancelLastActionOnly;

    public PendingActionCreator(bool cancelLastActionOnly)
    {
        _actionFunctionsCollection = new();
        _dataCollection = new();
        _cancelLastActionOnly = cancelLastActionOnly;
    }

    public int GetNumOfActions()
    {
        return _actionFunctionsCollection.Count;
    }

    public void Create(PendingActionFunction[] actionFunctions, PendingActionFunction[] cancelledActionFunctions, params object[] args)
    {
        int ID = _cancelLastActionOnly ? _actionFunctionsCollection.Count : (int)args[0];
        _actionFunctionsCollection.Add(ID, cancelledActionFunctions);
        _dataCollection.Add(ID, args);
        actionFunctions.ToList().ForEach(item => item(args));
    }

    public bool TryCancel(int pendingActionID)
    {
        if (_cancelLastActionOnly && (int)_dataCollection.ToList().Last().Value[0] != pendingActionID) // check if cancelled action was the last action
        {
            return false;
        }

        int ID = _cancelLastActionOnly ? _actionFunctionsCollection.Count - 1 : pendingActionID;
        PendingActionFunction[] prevActionItems = _actionFunctionsCollection[ID];
        object[] args = _dataCollection[ID];
        args[1] = true; // setting isActionCancelled to true
        _actionFunctionsCollection.Remove(ID);
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
        _actionFunctionsCollection.Clear();
        _dataCollection.Clear();
    }
}
