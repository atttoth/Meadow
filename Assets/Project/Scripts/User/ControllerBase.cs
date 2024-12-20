using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ControllerBase<T, K, E, F> : GameInteractionEvent
{
    protected T _tableView;
    protected K _handView;
    protected E _markerView;
    protected F _scoreView;

    public virtual void Init(T tableView, K handView, E markerView, F scoreView)
    {
        _tableView = tableView;
        _handView = handView;
        _markerView = markerView;
        _scoreView = scoreView;
    }
}
