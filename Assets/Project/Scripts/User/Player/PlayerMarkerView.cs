using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMarkerView : ViewBase
{
    private List<Marker> _allMarkers;
    private List<Marker> _remainingMarkers;
    private int _currentMarkerIndex;

    public override void Init()
    {
        _allMarkers = new();
        _remainingMarkers = new();
        for (int index = 0; index < 5; index++)
        {
            Marker marker = Instantiate(GameAssets.Instance.markerPrefab).GetComponent<Marker>();
            marker.gameObject.SetActive(false);
            marker.transform.SetParent(transform);
            marker.CreateMarker(index);
            _allMarkers.Add(marker);
            _remainingMarkers.Add(marker);
        }
        _currentMarkerIndex = -1;
    }

    public List<Marker> GetRemainingMarkers()
    {
        return _remainingMarkers;
    }

    public Marker GetCurrentMarker(int value)
    {
        if(value == 0 && _currentMarkerIndex < 0)
        {
            _currentMarkerIndex = 0;
        }
        else if (_currentMarkerIndex == 0 && value == -1)
        {
            _currentMarkerIndex = _remainingMarkers.Count - 1;
        }
        else if(_currentMarkerIndex == _remainingMarkers.Count - 1 && value == 1)
        {
            _currentMarkerIndex = 0;
        }
        else
        {
            _currentMarkerIndex += value;
        }
        return _remainingMarkers[_currentMarkerIndex];
    }

    public void SetPlacedMarkerToUsed()
    {
        Marker placedMarker = _remainingMarkers.Find(marker => marker.Status == MarkerStatus.PLACED);
        if (placedMarker)
        {
            placedMarker.Status = MarkerStatus.USED;
            _remainingMarkers.Remove(placedMarker);
            _currentMarkerIndex = 0;
        }
    }

    public void Reset()
    {
        _allMarkers.ForEach(marker =>
        {
            MarkerHolder prevHolder = marker.transform.parent.GetComponent<MarkerHolder>();
            prevHolder.RemoveItemFromContentList(marker);
            marker.gameObject.SetActive(false);
            marker.transform.SetParent(transform);
            marker.Status = MarkerStatus.NONE;
            _remainingMarkers.Add(marker);
        });
        _currentMarkerIndex = -1;
    }
}
