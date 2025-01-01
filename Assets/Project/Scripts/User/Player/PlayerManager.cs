using UnityEngine;


public class PlayerManager : MonoBehaviour
{
    private PlayerController _controller;   

    public void CreatePlayer()
    {
        PlayerTableView tableView = transform.GetChild(1).GetComponent<PlayerTableView>();
        PlayerHandView handView = transform.GetChild(2).GetComponent<PlayerHandView>();
        PlayerMarkerView markerView = transform.GetChild(3).GetComponent<PlayerMarkerView>();
        PlayerInfoView infoView = tableView.transform.GetChild(4).GetComponent<PlayerInfoView>();
        _controller = GetComponent<PlayerController>();
        _controller.Init(tableView, handView, markerView, infoView);
    }

    public PlayerController Controller
    { 
        get { return _controller; }
    }
}
