using System.Collections.Generic;
using UnityEngine;
public class GameTaskItemData
{
    public int pendingCardDataID = -1; // only used at Pending card actions

    public Card card;
    public Holder holder;
    public Marker marker;

    public Sprite sprite;
    public bool needToRotate;
    public DummyType dummyType;

    public Transform handTransform;

    public MarkerAction markerAction;
    public DeckType deckType;
    public List<Card> topCards;
}
