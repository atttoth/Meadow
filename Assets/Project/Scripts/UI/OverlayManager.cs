using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Timeline.Actions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum DummyType
{
    CARD,
    ACTION_ICON
}

public class OverlayManager : GameInteractionEvent
{
    private GameObject _dummy;
    private Image _blackOverlay;
    private Vector3 _defaultPosition;
    Sequence examineSequence;
    private MarkerActionScreen _markerActionScreen;

    public void CreateOverlay()
    {
        _dummy = transform.GetChild(1).gameObject;
        _defaultPosition = _dummy.transform.position;
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        EnableDummy(false);
        _markerActionScreen = transform.GetChild(2).GetComponent<MarkerActionScreen>();
        List<Button> actionIconButtons = _markerActionScreen.Init();
        actionIconButtons.ForEach(button => button.onClick.AddListener(() =>
        {
            button.enabled = true;
            StartEventHandler(GameEventType.MARKER_ACTION_SELECTED, new GameTaskItemData() { markerAction = button.GetComponent<MarkerActionScreenItem>().markerAction });
        }));
    }

    public void SetDummy(Sprite sprite, bool needToRotate, DummyType type)
    {
        int[] size = GetDummySize(type);
        _dummy.GetComponent<RectTransform>().sizeDelta = new(size[0], size[1]);
        _dummy.GetComponent<Image>().sprite = sprite;
        if(needToRotate)
        {
            _dummy.transform.eulerAngles = new Vector3(_dummy.transform.rotation.eulerAngles.x, _dummy.transform.rotation.eulerAngles.y, _dummy.transform.rotation.eulerAngles.z + 90f);
            _dummy.transform.position = new Vector3(_dummy.transform.position.x, _dummy.transform.position.y + 60f, _dummy.transform.position.z);
        }
    }

    private int[] GetDummySize(DummyType type)
    {
        return type switch
        {
            DummyType.CARD => new int[] { 160, 232 },
            DummyType.ACTION_ICON => new int[] { 300, 300 },
            _ => null
        };
    }

    public void StartCardShowSequence()
    {
        examineSequence.Kill();
        examineSequence = DOTween.Sequence();
        examineSequence.Append(_dummy.transform.DOScale(3.5f, 0.5f).SetEase(Ease.InOutSine));
        examineSequence.Play();
    }

    public void EnableDummy(bool value)
    {
        _dummy.SetActive(value);
        if(_blackOverlay.enabled != value)
        {
            _blackOverlay.enabled = value;
        }
    }

    public void ToggleMarkerActionScreen(Marker marker)
    {
        _markerActionScreen.ToggleScreen(marker);
    }
}
