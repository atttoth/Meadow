using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum DummyType
{
    CARD,
    ACTION_ICON
}

public class OverlayManager : MonoBehaviour, IPointerClickHandler
{
    private GameObject _dummy;
    private Image _blackOverlay;
    private Vector3 _defaultPosition;
    Sequence examineSequence;

    public void CreateOverlay()
    {
        _dummy = transform.GetChild(1).gameObject;
        _defaultPosition = _dummy.transform.position;
        _blackOverlay = transform.GetChild(0).GetComponent<Image>();
        EnableDummy(false);
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

    public void SetDummies()
    {

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

    public void ShowActionIcon()
    {

    }

    public void ShowActionIcon(bool isJoker)
    {

    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right || eventData.button == PointerEventData.InputButton.Left)
        {
            EnableDummy(false);
            _dummy.transform.localScale = new Vector2(1f, 1f);
            _dummy.transform.eulerAngles = new Vector3(0, 0, 0);
            _dummy.transform.position = _defaultPosition;
            bool value = !ReferenceManager.Instance.playerManager.Controller.IsTableVisible();
            ReferenceManager.Instance.gameLogicManager.EnableRayTargetOInteractables(value);
        }
    }

    public void EnableDummy(bool value)
    {
        _dummy.SetActive(value);
        if(_blackOverlay.enabled != value)
        {
            _blackOverlay.enabled = value;
        }
    }
}
