using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class NpcTableView : TableView
{
    private List<TableViewState> _states; // store states during card placement/selection evaluation
    private Transform _placedCardsContainer;

    public Transform PlacedCardsContainer {  get { return _placedCardsContainer; } }

    public override void Init()
    {
        base.Init();
        _placedCardsContainer = transform.GetChild(0);
    }

    private List<HolderData> CreateCopyOfHolderDataCollection(List<HolderData> oldCollection)
    {
        List<HolderData> newCollection = new();
        oldCollection.ForEach(data =>
        {
            HolderData holderData = new(data.ID, data.holderType);
            holderData.holderSubType = data.holderSubType;
            holderData.ContentList = new(data.ContentList);
            newCollection.Add(holderData);
        });
        return newCollection;
    }

    public int GetLastTableStateIndex()
    {
        return _states.Count - 1;
    }

    public void SaveState()
    {
        TableViewState prevState = new(
            new(_activeState.AllIconsOfPrimaryHoldersInOrder),
            new(_activeState.AllIconsOfSecondaryHoldersInOrder),
            CreateCopyOfHolderDataCollection(_activeState.PrimaryCardHolderDataCollection),
            CreateCopyOfHolderDataCollection(_activeState.SecondaryCardHolderDataCollection)
            );
        TableViewState currentState = new(
            new(prevState.AllIconsOfPrimaryHoldersInOrder),
            new(prevState.AllIconsOfSecondaryHoldersInOrder),
            CreateCopyOfHolderDataCollection(prevState.PrimaryCardHolderDataCollection),
            CreateCopyOfHolderDataCollection(prevState.SecondaryCardHolderDataCollection)
            );
        _states.Add(prevState);
        _activeState = currentState;
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
        TableViewState loadedState = updatedStates.Last();
        _states = updatedStates;
        _activeState = new(
            new(loadedState.AllIconsOfPrimaryHoldersInOrder),
            new(loadedState.AllIconsOfSecondaryHoldersInOrder),
            CreateCopyOfHolderDataCollection(loadedState.PrimaryCardHolderDataCollection),
            CreateCopyOfHolderDataCollection(loadedState.SecondaryCardHolderDataCollection)
            );
    }

    public void DisposeStates()
    {
        _states = new();
    }

    public override void RegisterCardPlacementAction(HolderData holderData, Card card, bool isActionCancelled = false)
    {
        holderData.AddItemToContentList(card);
        card.cardStatus = CardStatus.PENDING_ON_TABLE;
    }

    public override void AddNewPrimaryHolder(string tag)
    {
        int index = tag == "RectLeft" ? 0 : _activeState.PrimaryCardHolderDataCollection.Count;
        HolderData holderData = new(_activeState.PrimaryCardHolderDataCollection.Count, HolderType.TableCard);
        holderData.holderSubType = HolderSubType.PRIMARY;
        _activeState.PrimaryCardHolderDataCollection.Insert(index, holderData);
    }

    public override void AddNewSecondaryHolder()
    {
        HolderData holderData = new(_activeState.SecondaryCardHolderDataCollection.Count, HolderType.TableCard);
        holderData.holderSubType = HolderSubType.SECONDARY;
        _activeState.SecondaryCardHolderDataCollection.Add(holderData);
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
