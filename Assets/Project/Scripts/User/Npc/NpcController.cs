using UnityEngine;

public class NpcController : UserController
{
    public override void CreateUser()
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
        _markerView.Init();
        _allIconsOfPrimaryHoldersInOrder = new();
        _allIconsOfSecondaryHoldersInOrder = new();
        base.CreateUser();
    }

    public void DoTurnAction(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                Debug.Log(userID);
                task.StartDelayMs(1000);
                break;
            default:
                EndTurn();
                task.Complete();
                break;
        }
    }
}
