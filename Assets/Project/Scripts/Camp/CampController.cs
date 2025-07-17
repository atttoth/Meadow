using DG.Tweening;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class CampController : MonoBehaviour
{
    private static readonly int NUM_OF_MARKER_SLOTS = 3;
    private GameEventController _eventController;
    private List<MarkerHolder> _markerHolders;
    private GameTask _waitingTask;
    private CampView _view;
    private CanvasGroup _canvasGroup;

    public void Create()
    {
        _eventController = new();
        _markerHolders = new();
        SpriteAtlas atlas = GameResourceManager.Instance.Base;
        RectTransform markerDisplayHolders = transform.GetChild(0).GetComponent<RectTransform>();
        markerDisplayHolders.GetComponent<Image>().sprite = atlas.GetSprite("campDisplay");
        for (int i = 0; i < NUM_OF_MARKER_SLOTS; i++)
        {
            MarkerHolder markerHolder = markerDisplayHolders.GetChild(i).GetComponent<MarkerHolder>();
            markerHolder.Init(i, HolderType.CampMarker);
            _markerHolders.Add(markerHolder);
        }
        _view = transform.GetChild(1).GetComponent<CampView>();
        _view.Init();
        _canvasGroup = markerDisplayHolders.GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
        ToggleRayCastOfMarkerHolders(false);
    }

    public void DisposeCampForRound()
    {
        _view.ResetCampView();
    }

    private void InitCampForSession()
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
                _waitingTask.CancelWait();
                _waitingTask = null;
            }
        }));

        List<ScreenDisplayItem> scoreButtonItems = _view.CreateItemButtons();
        scoreButtonItems.ForEach(item => 
        item.button.onClick.AddListener(() =>
        {
            int score = _view.PlayerCampScoreToken;
            if (_view.isCampActionEnabled && score > 0)
            {
                _view.OnItemButtonClick(item);
                _eventController.InvokeEventHandler(GameLogicEventType.CAMP_SCORE_RECEIVED, new object[] { score, item.GetComponent<Transform>().position });
            }
            else
            {
                Debug.Log("Unlock camp to get score");
            }
        }));
    }

    public List<List<CardIcon>> GetAdjacentIconPairs()
    {
        return _view.GetAvailableIconPairs();
    }

    public void ToggleCampView(bool value)
    {
        _view.gameObject.SetActive(value);
    }

    public void SetupViewHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                _waitingTask = task;
                InitCampForSession();
                ToggleCampView(true);
                task.StartDelayMs(500);
                break;
            case 1:
                _view.ShowCampIconSelection();
                task.StartDelayMs(0, true);
                break;
            case 2:
                task.Wait();
                break;
            case 3:
                task.StartDelayMs((int)(GameSettings.Instance.GetDuration(Duration.cardRotationSpeedOnBoard) * 1000)); // wait for last icon to flip
                break;
            case 4:
                task.StartHandler((Action<GameTask>)_view.SetIconsPositionHandler);
                break;
            case 5:
                task.StartDelayMs(1000);
                break;
            case 6:
                task.StartHandler((Action<GameTask>)_view.ShowScoreButtonsHandler);
                break;
            case 7:
                task.StartDelayMs(2000);
                break;
            default:
                ToggleCampView(false);
                task.Complete();
                break;
        }
    }

    public void SaveCampScoreToken(int playerCampScoreToken)
    {
        _view.UpdateCampScoreToken(playerCampScoreToken);
    }

    public void EnableScoreButtonOfFulfilledIcons(List<List<CardIcon>> iconPairs)
    {
        _view.CheckFulfilledAdjacentIcons(iconPairs);
    }

    public void ToggleRayCastOfMarkerHolders(bool value)
    {
        _markerHolders.ForEach(holder => holder.ToggleRayCast(value));
    }

    public void ToggleCampAction(bool value)
    {
        _view.isCampActionEnabled = value;
    }

    public void Fade(bool value)
    {
        float fadeDuration = GameSettings.Instance.GetDuration(Duration.gameUIFadeDuration);
        float targetValue = value ? 1f : 0f;
        DOTween.Sequence().Append(_canvasGroup.DOFade(targetValue, fadeDuration));
    }
}
