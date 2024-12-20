using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Threading.Tasks;

public class CampManager : MonoBehaviour
{
    public bool isAvailable;
    private List<MarkerHolder> _markerHolders;

    public void CreateCamp()
    {
        isAvailable = false;
        _markerHolders = new();
        Transform markerDisplayHolders = transform.GetChild(1).GetChild(0);
        for(int i = 0; i < 3; i++)
        {
            MarkerHolder markerHolder = markerDisplayHolders.GetChild(i).GetComponent<MarkerHolder>();
            markerHolder.Init();
            markerHolder.ID = i;
            markerHolder.holderType = HolderType.CampMarker;
            markerHolder.contentList = new();
            _markerHolders.Add(markerHolder);
        }
    }

    public async void PlaceMarkerToCamp(MarkerHolder holder, Marker marker)
    {
        Sequence sequence = DOTween.Sequence();
        sequence.Append(marker.transform.DOMove(holder.transform.position, 0.2f).SetEase(Ease.InOutBack));
        sequence.Play();
        await sequence.AsyncWaitForCompletion();
    }

    public void EnableCamp(bool value)
    {
        isAvailable = value;
    }

    public void ToggleMarkerHolders(bool value)
    {
        _markerHolders.ForEach(holder => holder.ToggleRayCast(value));
    }
}
