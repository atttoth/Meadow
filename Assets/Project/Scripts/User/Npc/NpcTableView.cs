using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class NpcTableView : TableView
{
    private List<TableViewState> _states; // store states during card placement/selection evaluation
    private Transform _placedCardsContainer;

    public Transform PlacedCardsContainer {  get { return _placedCardsContainer; } }

    public override void Init()
    {
        base.Init();
        _states = new();
        _placedCardsContainer = transform.GetChild(0);
    }

    public int GetLastTableStateIndex()
    {
        return _states.Count - 1;
    }

    public void SaveState()
    {
        _states.Add(_activeState);
        TableViewState state = new(new(_activeState.AllIconsOfPrimaryHoldersInOrder), new(_activeState.AllIconsOfSecondaryHoldersInOrder), new(_activeState.PrimaryCardHolderDataCollection), new(_activeState.SecondaryCardHolderDataCollection));
        _activeState = state;
    }

    public void LoadState(int stateIndex)
    {
        List<TableViewState> updatedStates = new();
        for(int i = 0; i < _states.Count; i++)
        {
            if (i <= stateIndex)
            {
                updatedStates.Add(_states[i]);
            }
        }
        _states = updatedStates;
        TableViewState state = new(new(_states.Last().AllIconsOfPrimaryHoldersInOrder), new(_states.Last().AllIconsOfSecondaryHoldersInOrder), new(_states.Last().PrimaryCardHolderDataCollection), new(_states.Last().SecondaryCardHolderDataCollection));
        _activeState = state;
    }

    public override void RegisterCardPlacementAction(object[] args)
    {
        HolderData holderData = (HolderData)args[2];
        Card card = (Card)args[3];
        holderData.AddItemToContentList(card);
        card.cardStatus = CardStatus.PENDING_ON_TABLE;
    }

    public override void AddNewPrimaryHolder(string tag)
    {
        List<HolderData> primaryCardHolderDataCollection = _activeState.PrimaryCardHolderDataCollection;
        int index = tag == "RectLeft" ? 0 : primaryCardHolderDataCollection.Count;
        HolderData holderData = new(primaryCardHolderDataCollection.Count, HolderType.TableCard);
        holderData.holderSubType = HolderSubType.PRIMARY;
        primaryCardHolderDataCollection.Insert(index, holderData);
    }

    public override void AddNewSecondaryHolder()
    {
        List<HolderData> secondaryCardHolderDataCollection = _activeState.SecondaryCardHolderDataCollection;
        HolderData holderData = new(secondaryCardHolderDataCollection.Count, HolderType.TableCard);
        holderData.holderSubType = HolderSubType.SECONDARY;
        secondaryCardHolderDataCollection.Add(holderData);
    }

    public override List<List<CardIcon>> GetAdjacentPrimaryHolderIcons(HolderData holderData)
    {
        List<HolderData> primaryCardHolder = _activeState.PrimaryCardHolderDataCollection;
        int index = primaryCardHolder.IndexOf(holderData);
        List<List<CardIcon>> adjacentHolderIcons = new() { new(), new() };
        HolderData leftHolderData = index > 0 ? primaryCardHolder[index - 1] : null;
        HolderData rightHolderData = index < primaryCardHolder.Count - 1 ? primaryCardHolder[index + 1] : null;

        if (leftHolderData != null && !leftHolderData.IsEmpty())
        {
            adjacentHolderIcons[0] = leftHolderData.GetAllIconsOfHolder();
        }

        if (rightHolderData != null && !rightHolderData.IsEmpty())
        {
            adjacentHolderIcons[1] = rightHolderData.GetAllIconsOfHolder();
        }
        return adjacentHolderIcons;
    }

    public HolderData GetPrimaryHolderDataByTag(string tagName)
    {
        int index = tagName == "RectLeft" ? 0 : _activeState.PrimaryCardHolderDataCollection.Count - 1;
        return _activeState.PrimaryCardHolderDataCollection[index];
    }

    public HolderData GetLastSecondaryHolderData()
    {
        return _activeState.SecondaryCardHolderDataCollection[_activeState.SecondaryCardHolderDataCollection.Count - 1];
    }
}
