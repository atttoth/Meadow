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
    public abstract void RegisterCardPlacementAction(HolderData holderData, Card card, bool isActionCancelled);

    public virtual void Init()
    {
        _activeState = new TableViewState(new(), new(), new(), new());
    }

    public void UpdateHolderIconsAction(HolderData holderData)
    {
        if (holderData.holderSubType == HolderSubType.PRIMARY)
        {
            _activeState.AllIconsOfPrimaryHoldersInOrder = new();
            for (int i = 0; i < _activeState.PrimaryCardHolderDataCollection.Count; i++)
            {
                HolderData data = _activeState.PrimaryCardHolderDataCollection[i];
                CardIcon[][] items = new CardIcon[data.ContentList.Count][];
                for (int j = 0; j < items.Length; j++)
                {
                    Card card = (Card)data.GetItemFromContentListByIndex(j);
                    items[j] = card.Data.icons;
                }
                _activeState.AllIconsOfPrimaryHoldersInOrder.Add(items);
            }
        }
        else
        {
            _activeState.AllIconsOfSecondaryHoldersInOrder = new();
            for (int i = 0; i < _activeState.SecondaryCardHolderDataCollection.Count; i++)
            {
                HolderData data = _activeState.SecondaryCardHolderDataCollection[i];
                CardIcon[][] items = new CardIcon[data.ContentList.Count][];
                for (int j = 0; j < items.Length; j++)
                {
                    Card card = (Card)data.GetItemFromContentListByIndex(j);
                    items[j] = card.Data.icons;
                }
                _activeState.AllIconsOfSecondaryHoldersInOrder.Add(items);
            }
        }
    }

    public List<CardIcon[]> GetTopPrimaryIcons()
    {
        List<CardIcon[]> topIcons = new();
        _activeState.AllIconsOfPrimaryHoldersInOrder.ForEach(values =>
        {
            CardIcon[] icons = values[^1].Where(icon => (int)icon > 4).ToArray();
            topIcons.Add(icons);
        });
        return topIcons;
    }

    public List<CardIcon> GetAllRelevantIcons(HolderSubType holderSubType) // top icons and ground icons
    {
        List<CardIcon[][]> collection = holderSubType == HolderSubType.PRIMARY ? _activeState.AllIconsOfPrimaryHoldersInOrder : _activeState.AllIconsOfSecondaryHoldersInOrder;
        List<CardIcon> allCurrentIcons = new();
        for(int i = 0; i < collection.Count; i++)
        {
            CardIcon[][] items = collection[i];
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
