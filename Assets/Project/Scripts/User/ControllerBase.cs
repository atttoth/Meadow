public abstract class ControllerBase<T, K, E, F> : GameLogicEvent
{
    protected T _tableView;
    protected K _handView;
    protected E _markerView;
    protected F _infoView;

    public virtual void Init(T tableView, K handView, E markerView, F infoView)
    {
        _tableView = tableView;
        _handView = handView;
        _markerView = markerView;
        _infoView = infoView;
    }
}
