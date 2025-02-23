using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class GameTaskItemData
{
    public int pendingActionID = -1;

    public Card card;
    public Holder holder;
    public Marker marker;
    public List<RaycastResult> raycastResults;

    public bool value;

    public Transform originTransform;
    public Transform targetTransform;

    public MarkerAction markerAction;
    public DeckType deckType;
    public List<Card> cards;
    public List<CardData> dataCollection;
    public HolderSubType subType;
    public string hitAreaTag;
    public List<CardHolder> holders;

    public int score;

    /*public void DoSomething(params object[] theObjects) //try this as task param
{
    foreach (object o in theObjects)
    {
        // Something with the Objects…
    }
}

 DoSomething(this, that, theOther);
 */
}
