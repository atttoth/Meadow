using UnityEngine;
public class GameTaskItemData
{
    public int pendingCardDataID = -1; // only used at Pending card actions

    public Card card;
    public CardHolder cardHolder;
    public MarkerHolder markerHolder;
    public Marker marker;
    public bool isMarkerReset = false;

    public Sprite sprite;
    public bool needToRotate;
    public DummyType dummyType;

    public bool isSpecialMarker = false;

    public Transform handTransform;
}
