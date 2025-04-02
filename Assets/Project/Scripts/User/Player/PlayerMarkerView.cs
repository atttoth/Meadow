public class PlayerMarkerView : MarkerView
{
    public override Marker GetCurrentMarker(int value)
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
}
