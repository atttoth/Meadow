using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class TableView : MonoBehaviour
{
    protected static int _MAX_PRIMARY_HOLDER_NUM = 10;
    protected static int _MAX_SECONDARY_HOLDER_NUM = 8;
    protected TableViewState _activeState;

    public TableViewState ActiveState { get { return _activeState; } }
    public abstract void AddNewPrimaryHolder(string tag);
    public abstract void AddNewSecondaryHolder();
    public abstract List<List<CardIcon>> GetAdjacentPrimaryHolderIcons(HolderData holderData);
    public abstract void RegisterCardPlacementAction(object[] args);

    public virtual void Init()
    {
        _activeState = new TableViewState(new(), new(), new(), new());
    }

    public Dictionary<int, CardIcon[][]> GetAllIcons(HolderSubType holderSubType)
    {
        return holderSubType == HolderSubType.PRIMARY ? _activeState.AllIconsOfPrimaryHoldersInOrder : _activeState.AllIconsOfSecondaryHoldersInOrder;
    }

    public List<CardIcon> GetAllRelevantIcons(HolderSubType holderSubType) // top icons and ground icons
    {
        Dictionary<int, CardIcon[][]> collection = GetAllIcons(holderSubType);
        List<CardIcon> allCurrentIcons = new();
        foreach (CardIcon[][] items in collection.Values)
        {
            allCurrentIcons.AddRange(items[items.Length - 1]);
            if (items.Length > 1)
            {
                List<CardIcon> groundIcons = items[0].Where(icon => (int)icon < 5).ToList();
                allCurrentIcons.AddRange(groundIcons);
            }
        }
        return allCurrentIcons;
    }
}
