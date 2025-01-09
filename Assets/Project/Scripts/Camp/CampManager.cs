using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class CampManager : GameLogicEvent
{
    private static readonly int NUM_OF_MARKER_SLOTS = 3;
    private List<MarkerHolder> _markerHolders;
    private CampView _view;
    private Button _campToggleButton;
    private bool _isCampVisible;

    public void CreateCamp()
    {
        _markerHolders = new();
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        GetComponent<Image>().sprite = atlas.GetSprite("campDisplay");
        Transform markerDisplayHolders = transform.GetChild(0);
        for (int i = 0; i < NUM_OF_MARKER_SLOTS; i++)
        {
            MarkerHolder markerHolder = markerDisplayHolders.GetChild(i).GetComponent<MarkerHolder>();
            markerHolder.Init(i, HolderType.CampMarker);
            _markerHolders.Add(markerHolder);
        }
        _view = transform.GetChild(1).GetComponent<CampView>();
        _view.Init();
        _campToggleButton = transform.GetChild(2).GetComponent<Button>();
        _campToggleButton.onClick.AddListener(() => ToggleCampView());
    }

    public void DisposeCampForRound()
    {
        _view.ResetCampView();
    }

    private void InitCampForRound()
    {
        _view.SetNumOfIconsForRound(2); // locked to 2 players for now
        List<ScreenDisplayItem> campItems = _view.CreateCampItems();
        campItems.ForEach(item => item.button.onClick.AddListener(() =>
        {
            item.button.enabled = false;
            _view.FlipCampIcon(item);
            if (_view.selectionsLeft < 1)
            {
                campItems.ForEach(item => item.button.enabled = false);
                StartEventHandler(GameLogicEventType.CAMP_ICONS_SELECTED, null);
            }
        }));

        List<ScreenDisplayItem> scoreButtonItems = _view.CreateItemButtons();
        scoreButtonItems.ForEach(item => item.button.onClick.AddListener(() =>
        {
            _view.OnItemButtonClick(item);
            StartEventHandler(GameLogicEventType.CAMP_SCORE_RECEIVED, null);
        }));
    }

    private void ToggleCampView()
    {
        _isCampVisible = !_isCampVisible;
        _view.gameObject.SetActive(_isCampVisible);
    }

    public void ShowViewSetupHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                InitCampForRound();
                ToggleCampView();
                task.StartDelayMs(500);
                break;
            case 1:
                _view.ShowCampIcons();
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void StartViewSetupHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                int delay1 = (int)(ReferenceManager.Instance.gameLogicManager.GameSettings.cardRotationSpeedOnBoard * 1000); // wait for last icon to flip
                task.StartDelayMs(delay1);
                break;
            case 1:
                task.StartHandler(_view.SetIconsPositionHandler);
                break;
            case 2:
                int delay2 = 1000;
                task.StartDelayMs(delay2);
                break;
            case 3:
                task.StartHandler(_view.ShowScoreButtonsHandler);
                break;
            case 4:
                int delay3 = 2000;
                task.StartDelayMs(delay3);
                break;
            default:
                ToggleCampView();
                task.Complete();
                break;
        }
    }

    public void ToggleMarkerHolders(bool value)
    {
        _markerHolders.ForEach(holder => holder.ToggleRayCast(value));
    }

    public void ToggleCampAction(bool value)
    {
        _view.isCampActionEnabled = value;
    }
}
