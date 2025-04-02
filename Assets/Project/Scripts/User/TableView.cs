using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class TableView : MonoBehaviour
{
    protected static int _MAX_PRIMARY_HOLDER_NUM = 10;
    protected static int _MAX_SECONDARY_HOLDER_NUM = 8;

    // Primary - ground and observation cards
    protected ScrollRect _primaryTableContentScroll;
    protected List<CardHolder> _activePrimaryCardHolders;
    protected Transform _primaryCardHolderContainer;

    // Secondary - landscape and discovery cards
    protected List<CardHolder> _activeSecondaryCardHolders;
    protected Transform _secondaryCardHolderContainer;

    public abstract void Init();

    public List<CardHolder> ActivePrimaryCardHolders { get { return _activePrimaryCardHolders; } }

    public List<List<CardIcon>> GetAdjacentHolderIcons(CardHolder holder)
    {
        List<List<CardIcon>> adjacentHolderIcons = new() { new(), new() };
        int index = holder.transform.GetSiblingIndex();
        CardHolder leftHolder = index > 0 ? _activePrimaryCardHolders[index - 1] : null;
        CardHolder rightHolder = index < _activePrimaryCardHolders.Count - 1 ? _activePrimaryCardHolders[index + 1] : null;

        if (leftHolder != null && !leftHolder.IsEmpty())
        {
            adjacentHolderIcons[0] = leftHolder.GetAllIconsOfHolder();
        }

        if (rightHolder != null && !rightHolder.IsEmpty())
        {
            adjacentHolderIcons[1] = rightHolder.GetAllIconsOfHolder();
        }
        return adjacentHolderIcons;
    }
}
