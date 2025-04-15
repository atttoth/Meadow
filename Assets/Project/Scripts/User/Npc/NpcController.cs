using System;
using System.Collections.Generic;
using System.Linq;

public class NpcController : UserController
{
    private MarkerHolder _selectedMarkerHolder;
    private Marker _selectedMarker;

    public MarkerHolder SelectedMarkerHolder {  get { return _selectedMarkerHolder; } }
    public Marker SelectedMarker { get { return _selectedMarker; } }

    public override void CreateUser(GameMode gameMode)
    {
        _tableView = transform.GetChild(0).GetComponent<NpcTableView>();
        _iconDisplayView = _tableView.transform.GetChild(0).GetComponent<IconDisplayView>();
        _infoView = _tableView.transform.GetChild(1).GetComponent<InfoView>();
        _handView = transform.GetChild(1).GetComponent<NpcHandView>();
        _markerView = transform.GetChild(2).GetComponent<NpcMarkerView>();
        _tableView.Init();
        _iconDisplayView.Init();
        _infoView.Init();
        _handView.Init();
        _markerView.Init(gameMode.GetMarkerColorByUserID(userID));
        _allIconsOfPrimaryHoldersInOrder = new();
        _allIconsOfSecondaryHoldersInOrder = new();
        base.CreateUser(gameMode);
    }

    public void SelectRandomMarkerAndMarkerHolder(List<MarkerHolder> holders)
    {
        List<Marker> remainingMarkers = _markerView.GetRemainingMarkers();
        System.Random random = new();
        _selectedMarkerHolder = holders[random.Next(0, holders.Count - 1)];
        _selectedMarker = _markerView.GetCurrentMarker(random.Next(0, remainingMarkers.Count - 1));
        remainingMarkers.ForEach(marker => marker.gameObject.SetActive(marker == _selectedMarker));
    }

    public void ShowMarkerPlacementHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                task.StartHandler((Action<GameTask, MarkerHolder, Marker>)(_markerView as NpcMarkerView).PlaceMarkerToHolder, _selectedMarkerHolder, _selectedMarker);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public Card SelectInitialGroundCard(List<Card> cards)
    {
        return cards.First();
    }

    public override void PlaceInitialGroundCardOnTable(GameTask task, Card card)
    {
        task.Complete();
    }

    public void RegisterScoreHandler(GameTask task)
    {
        UpdateScore((_handView as NpcHandView).GetLastCardInHand().Data.score);
        task.Complete();
    }
}
