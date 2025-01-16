using System.Collections.Generic;
using UnityEngine;
public class GameTaskItemData
{
    public int pendingCardDataID = -1; // only used at Pending card actions

    public Card card;
    public Holder holder;
    public Marker marker;

    public Sprite sprite;
    public bool value;
    public DummyType dummyType;

    public Transform originTransform;
    public Transform targetTransform;

    public MarkerAction markerAction;
    public DeckType deckType;
    public List<Card> cards;

    public int score;
}
