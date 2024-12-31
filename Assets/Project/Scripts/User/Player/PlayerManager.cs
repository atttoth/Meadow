using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using UnityEngine.U2D;
using System.Threading.Tasks;
using UnityEngine.InputSystem.XR;

public class PlayerManager : MonoBehaviour
{
    private PlayerController _controller;   

    public void CreatePlayer()
    {
        PlayerTableView tableView = transform.GetChild(1).GetComponent<PlayerTableView>();
        PlayerHandView handView = transform.GetChild(2).GetComponent<PlayerHandView>();
        PlayerMarkerView markerView = transform.GetChild(3).GetComponent<PlayerMarkerView>();
        PlayerScoreView scoreView = transform.GetChild(4).GetComponent<PlayerScoreView>();
        _controller = GetComponent<PlayerController>();
        _controller.Init(tableView, handView, markerView, scoreView);
    }

    public PlayerController Controller
    { 
        get { return _controller; }
    }
}
