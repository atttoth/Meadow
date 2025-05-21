using System.Collections.Generic;

public class TableViewState
{
    // as cards are stacked in order
    private List<CardIcon[][]> _allIconsOfPrimaryHoldersInOrder;
    private List<CardIcon[][]> _allIconsOfSecondaryHoldersInOrder;
    private List<HolderData> _primaryCardHolderDataCollection;
    private List<HolderData> _secondaryCardHolderDataCollection;

    public TableViewState(List<CardIcon[][]> allIconsOfPrimaryHoldersInOrder, List<CardIcon[][]> allIconsOfSecondaryHoldersInOrder, List<HolderData> primaryCardHolderDataCollection, List<HolderData> secondaryCardHolderDataCollection)
    {
        _allIconsOfPrimaryHoldersInOrder = allIconsOfPrimaryHoldersInOrder;
        _allIconsOfSecondaryHoldersInOrder = allIconsOfSecondaryHoldersInOrder;
        _primaryCardHolderDataCollection = primaryCardHolderDataCollection;
        _secondaryCardHolderDataCollection = secondaryCardHolderDataCollection;
    }

    public List<CardIcon[][]> AllIconsOfPrimaryHoldersInOrder { get { return _allIconsOfPrimaryHoldersInOrder; } set { _allIconsOfPrimaryHoldersInOrder = value; } }
    public List<CardIcon[][]> AllIconsOfSecondaryHoldersInOrder { get { return _allIconsOfSecondaryHoldersInOrder; } set { _allIconsOfSecondaryHoldersInOrder = value; } }
    public List<HolderData> PrimaryCardHolderDataCollection { get { return _primaryCardHolderDataCollection; } }
    public List<HolderData> SecondaryCardHolderDataCollection { get { return _secondaryCardHolderDataCollection; } }
}
