using System;
using System.Collections.Generic;
using System.Linq;

enum DataCollectionType
{ 
    ITEM_ID,
    ACTION_FUNCTIONS,
    CANCELLED_ACTION_FUNCTIONS,
    PENDING_DATA
}

public class PendingActionCreator
{
    private readonly Dictionary<int, object[]> _functionDataCollection;
    private readonly bool _cancelLastActionOnly;

    public PendingActionCreator(bool cancelLastActionOnly)
    {
        _functionDataCollection = new();
        _cancelLastActionOnly = cancelLastActionOnly;
    }

    public int GetNumOfActions()
    {
        return _functionDataCollection.Count;
    }

    public int Create(int itemID, object[] actionFunctionsData, object[] cancelledActionFunctionsData, object[] pendingDataCollection)
    {
        int actionID = _cancelLastActionOnly ? GetNumOfActions() : itemID;
        object[] item = new object[] { itemID, actionFunctionsData, cancelledActionFunctionsData, pendingDataCollection };
        _functionDataCollection.Add(actionID, item);
        return actionID;
    }

    public void Start(int actionID)
    {
        int dataIndex = (int)DataCollectionType.ACTION_FUNCTIONS;
        Execute((object[])_functionDataCollection[actionID][dataIndex]);
    }

    public bool TryCancel(int itemID)
    {
        int dataIndex = (int)DataCollectionType.ITEM_ID;
        if (_cancelLastActionOnly && (int)_functionDataCollection.Last().Value[dataIndex] != itemID) // check if cancelled action was the last action
        {
            return false;
        }

        int actionID = _cancelLastActionOnly ? GetNumOfActions() - 1 : itemID;
        dataIndex = (int)DataCollectionType.CANCELLED_ACTION_FUNCTIONS;
        Execute((object[])_functionDataCollection[actionID][dataIndex]);
        _functionDataCollection.Remove(actionID);
        return true;
    }

    private void Execute(object[] functionsData)
    {
        Delegate[] functionCollection = (Delegate[])functionsData[0];
        object[][] argsCollection = (object[][])functionsData[1];
        for (int i = 0; i < functionCollection.Length; i++)
        {
            Delegate f = functionCollection[i];
            object[] args = argsCollection[i];
            f.DynamicInvoke(args);
        }
    }

    public List<object[]> GetPendingDataCollection()
    {
        int dataIndex = (int)DataCollectionType.PENDING_DATA;
        return _functionDataCollection.Select(collection => (object[])collection.Value[dataIndex]).ToList();
    }

    public void Dispose()
    {
        _functionDataCollection.Clear();
    }
}
