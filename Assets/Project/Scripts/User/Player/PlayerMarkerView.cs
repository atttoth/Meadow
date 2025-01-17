using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMarkerView : MonoBehaviour
{
    private List<Marker> _allMarkers;
    private List<Marker> _remainingMarkers;
    private int _currentMarkerIndex;

    public void Init()
    {
        _allMarkers = new();
        for (int index = 0; index < 5; index++)
        {
            Marker marker = Instantiate(GameAssets.Instance.markerPrefab).GetComponent<Marker>();
            marker.gameObject.SetActive(false);
            marker.transform.SetParent(transform);
            marker.CreateMarker(index);
            _allMarkers.Add(marker);
        }
        Reset();
    }

    public List<Marker> GetRemainingMarkers()
    {
        return _remainingMarkers;
    }

    public Marker GetCurrentMarker(int value)
    {
        if(value == 0 && _currentMarkerIndex < 0) // initial marker hover
        {
            _currentMarkerIndex = 0;
        }
        if(_currentMarkerIndex == 0 && value == -1)
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
        _remainingMarkers = new();
        _allMarkers.ForEach(marker =>
        {
            MarkerHolder prevHolder = marker.transform.parent.GetComponent<MarkerHolder>();
            if (prevHolder)
            {
                prevHolder.RemoveItemFromContentList(marker);
            }
            marker.gameObject.SetActive(false);
            marker.transform.SetParent(transform);
            marker.Status = MarkerStatus.NONE;
            _remainingMarkers.Add(marker);
        });
        _currentMarkerIndex = -1;
    }

    public void Fade(bool value)
    {
        float fadeDuration = ReferenceManager.Instance.gameLogicManager.GameSettings.gameUIFadeDuration;
        float targetValue = value ? 0f : 1f;
        _allMarkers
            .Where(marker => marker.Status == MarkerStatus.USED)
            .ToList()
            .ForEach(marker =>
            {
                DOTween.Sequence()
                .Append(marker.GetComponent<Image>().DOFade(targetValue, fadeDuration))
                .Join(marker.actionIcon.DOFade(targetValue, fadeDuration))
                .Join(marker.numberOnMarkerText.DOFade(targetValue, fadeDuration));
            });
    }
}
