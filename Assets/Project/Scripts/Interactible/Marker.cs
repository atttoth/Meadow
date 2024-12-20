using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.WSA;

public enum MarkerStatus
{
    NONE,
    PLACED,
    USED
}

public class Marker : Interactable
{
    public int numberOnMarker;
    private TextMeshProUGUI _numberOnMarker;
    private Image _actionIcon;
    private MarkerStatus _status;

    public MarkerStatus Status {
        get { return _status; }
        set { _status = value; }
    }

    public void CreateMarker(int index)
    {
        parent = transform.parent;
        name = $"marker{index}";
        ID = index;
        siblingIndex = transform.GetSiblingIndex();
        _status = MarkerStatus.NONE;
        _numberOnMarker = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        numberOnMarker = ID + 1;
        string value = ID < 4 ? numberOnMarker.ToString() : "?";
        _numberOnMarker.text = value;
        mainImage = GetComponent<Image>();
        _actionIcon = transform.GetChild(1).GetComponent<Image>();
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            _status = MarkerStatus.NONE;
            StartEventHandler(GameEventType.MARKER_CANCELLED, new GameTaskItemData() { marker = this });
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            _status = MarkerStatus.PLACED;
            MarkerHolder holder = parent.GetComponent<MarkerHolder>();
            StartEventHandler(GameEventType.MARKER_PLACED, new GameTaskItemData() { markerHolder = holder, marker = this });
        }
    }

    public void Rotate(MarkerDirection direction)
    {
        RectTransform markerTransform = GetComponent<RectTransform>();
        RectTransform valueTransform = _numberOnMarker.gameObject.GetComponent<RectTransform>();
        RectTransform iconTransform = _actionIcon.gameObject.GetComponent<RectTransform>();
        float zRot;
        switch (direction)
        {
            case MarkerDirection.LEFT:
                zRot = 180f;
                break;
            case MarkerDirection.RIGHT:
                zRot = 0f;
                break;
            default:
                zRot = -90f;
                break;
        }
        markerTransform.eulerAngles = new(0f, 0f, zRot);
        valueTransform.eulerAngles = new(0f, 0f, 0f);
        iconTransform.eulerAngles = new(0f, 0f, 0f);
    }

    public override void ToggleRayCast(bool value)
    {
        mainImage.raycastTarget = value;
        _numberOnMarker.raycastTarget = value;
        _actionIcon.raycastTarget = value;
    }

    public Sprite GetActionIcon()
    {
       return _actionIcon.sprite;
    }
}
