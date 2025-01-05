using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class CampManager : GameInteractionEvent
{
    private static readonly int NUM_OF_MARKER_SLOTS = 3;
    private bool _isCampActionEnabled;
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
        List<Button> iconButtons = _view.Init();
        iconButtons.ForEach(button => button.onClick.AddListener(() =>
        {
            button.enabled = false;
            _view.FlipCampIcon(button.GetComponent<ScreenDisplayItem>());
            if(_view.selectionsLeft < 1)
            {
                iconButtons.ForEach(button => button.enabled = false);
                StartEventHandler(GameEventType.CAMP_ICONS_SELECTED, null);
            }
        }));
        _campToggleButton = transform.GetChild(2).GetComponent<Button>();
        _campToggleButton.onClick.AddListener(() => ToggleCampView());
    }

    private void ToggleCampView()
    {
        _isCampVisible = !_isCampVisible;
        _view.gameObject.SetActive(_isCampVisible);
    }

    public void StartViewSetup(GameTask task)
    {
        switch(task.State)
        {
            case 0:
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

    public void EndViewSetup(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                int delay = (int)(ReferenceManager.Instance.gameLogicManager.GameSettings.cardRotationSpeedOnBoard * 1000); // wait for last icon to flip
                task.StartDelayMs(delay);
                break;
            case 1:
                task.StartHandler(_view.SetIconsPositionHandler);
                break;
            case 2:
                task.StartHandler(_view.ShowScoreButtonshandler);
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
        _isCampActionEnabled = value;
    }
}
