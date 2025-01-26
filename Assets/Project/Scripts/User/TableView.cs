using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class TableView : MonoBehaviour
{
    protected static int _MAX_HOLDER_NUM = 10;

    // Primary - ground and observation cards
    protected ScrollRect _primaryTableContentScroll;
    protected List<CardHolder> _activePrimaryCardHolders;
    protected Transform _primaryCardHolderContainer;

    // Secondary - landscape and discovery cards
    protected ScrollRect _secondaryTableContentScroll;
    protected List<CardHolder> _activeSecondaryCardHolders;
    protected Transform _secondaryCardHolderContainer;

    // Icon display
    protected Transform _iconsTransform;
    protected Transform _preparedIconsTransform;
    protected Transform _displayIconPoolTransform;
    protected List<DisplayIcon> _displayIconPool;
    protected List<DisplayIcon> _displayIcons;
    protected List<DisplayIcon> _preparedDisplayIcons;

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
