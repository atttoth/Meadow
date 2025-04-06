using DG.Tweening;
using UnityEngine;

public class NpcMarkerView : MarkerView
{
    public override Marker GetCurrentMarker(int value)
    {
        _currentMarkerIndex = value;
        return _remainingMarkers[_currentMarkerIndex];
    }

    public void PlaceMarkerToHolder(GameTask task, MarkerHolder holder, Marker marker)
    {
        switch(task.State)
        {
            case 0:
                float duration = ReferenceManager.Instance.gameLogicController.GameSettings.npcMarkerPlacementDuration;
                Vector3 targetPosition = holder.GetComponent<RectTransform>().position;
                Vector3 startingPos = GetStartingPosition(targetPosition, holder.Direction);
                RectTransform rect = marker.GetComponent<RectTransform>();
                rect.position = startingPos;
                marker.SetAlpha(true);
                DOTween.Sequence().Append(rect.DOMove(targetPosition, duration)).SetEase(Ease.InOutSine);
                task.StartDelayMs((int)duration * 1000);
                break;
            case 1:
                task.StartDelayMs(1000);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private Vector3 GetStartingPosition(Vector3 pos, MarkerDirection direction)
    {
        switch (direction)
        {
            case MarkerDirection.LEFT: return new Vector3(pos.x - 100f, pos.y, pos.z);
            case MarkerDirection.RIGHT: return new Vector3(pos.x + 100f, pos.y, pos.z);
            default: return new Vector3(pos.x, pos.y - 100f, pos.z);
        }
    }
}
