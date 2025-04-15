using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public abstract class MarkerView : MonoBehaviour
{
    // used for initial hand setup
    public static int BLANK_MARKER_ID = 5;
    protected Marker _blankMarker;

    // gameplay
    protected bool _isMarkerConsumed;
    protected List<Marker> _allMarkers;
    protected List<Marker> _remainingMarkers;
    protected int _currentMarkerIndex;

    public Marker BlankMarker { get { return _blankMarker; } }

    public void Init(Color32 color)
    {
        _allMarkers = new();
        for (int index = 0; index <= BLANK_MARKER_ID; index++)
        {
            Marker marker = Instantiate(GameAssets.Instance.markerPrefab).GetComponent<Marker>();
            marker.gameObject.SetActive(false);
            marker.transform.SetParent(transform);
            marker.CreateMarker(index, color);
            if(index < BLANK_MARKER_ID)
            {
                _allMarkers.Add(marker);
            }
            else
            {
                _blankMarker = marker;
            }
        }
        Reset();
    }

    public bool IsMarkerConsumed { get {  return _isMarkerConsumed; } set { _isMarkerConsumed = value; } }

    public abstract Marker GetCurrentMarker(int value);

    public List<Marker> GetRemainingMarkers()
    {
        return _remainingMarkers;
    }

    public void EndHandSetupHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                float duration = ReferenceManager.Instance.gameLogicController.GameSettings.gameUIFadeDuration;
                _blankMarker.Fade(false, duration);
                task.StartDelayMs((int)duration * 1000);
                break;
            case 1:
                _blankMarker.transform.parent.GetComponent<MarkerHolder>().RemoveItemFromContentList(_blankMarker);
                Object.Destroy(_blankMarker.gameObject);
                task.StartDelayMs(0);
                break;
            default:
                Reset();
                task.Complete();
                break;
        }
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
        if(_blankMarker?.Status == MarkerStatus.NONE)
        {
            _remainingMarkers.Add(_blankMarker);
        }
        else
        {
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
        }
        _currentMarkerIndex = -1;
    }

    public void Fade(bool value)
    {
        float fadeDuration = ReferenceManager.Instance.gameLogicController.GameSettings.gameUIFadeDuration;
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
