using System.Collections.Generic;

public class TableViewState
{
    // as cards are stacked in order
    private Dictionary<int, CardIcon[][]> _allIconsOfPrimaryHoldersInOrder;
    private Dictionary<int, CardIcon[][]> _allIconsOfSecondaryHoldersInOrder;
    private List<HolderData> _primaryCardHolderDataCollection;
    private List<HolderData> _secondaryCardHolderDataCollection;

    public TableViewState(Dictionary<int, CardIcon[][]> allIconsOfPrimaryHoldersInOrder, Dictionary<int, CardIcon[][]> allIconsOfSecondaryHoldersInOrder, List<HolderData> primaryCardHolderDataCollection, List<HolderData> secondaryCardHolderDataCollection)
    {
        _allIconsOfPrimaryHoldersInOrder = allIconsOfPrimaryHoldersInOrder;
        _allIconsOfSecondaryHoldersInOrder = allIconsOfSecondaryHoldersInOrder;
        _primaryCardHolderDataCollection = primaryCardHolderDataCollection;
        _secondaryCardHolderDataCollection = secondaryCardHolderDataCollection;
    }

    public Dictionary<int, CardIcon[][]> AllIconsOfPrimaryHoldersInOrder { get { return _allIconsOfPrimaryHoldersInOrder; } }
    public Dictionary<int, CardIcon[][]> AllIconsOfSecondaryHoldersInOrder { get { return _allIconsOfSecondaryHoldersInOrder; } }
    public List<HolderData> PrimaryCardHolderDataCollection { get { return _primaryCardHolderDataCollection; } }
    public List<HolderData> SecondaryCardHolderDataCollection { get { return _secondaryCardHolderDataCollection; } }
}
