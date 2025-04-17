using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.U2D;
using UnityEngine.UI;

public enum MarkerStatus
{
    NONE,
    PLACED,
    USED
}

public enum MarkerAction
{
    PICK_ANY_CARD_FROM_BOARD,
    TAKE_2_ROAD_TOKENS,
    PICK_A_CARD_FROM_CHOSEN_DECK,
    PLAY_UP_TO_2_CARDS,
    DO_ANY
}

public class Marker : Interactable
{
    public int ID;
    public int numberOnMarker;
    public MarkerAction action;
    public TextMeshProUGUI numberOnMarkerText;
    public Image actionIcon;
    private MarkerStatus _status;

    public Transform Parent
    {
        get { return _parent; }
        set { _parent = value; }
    }

    public MarkerStatus Status
    {
        get { return _status; }
        set { _status = value; }
    }

    public void Create(int index, Color32 color)
    {
        Init();
        SpriteAtlas atlas = GameResourceManager.Instance.Base;
        name = $"marker{index}";
        ID = index;
        _status = MarkerStatus.NONE;
        _mainImage = GetComponent<Image>();
        _mainImage.color = color;
        numberOnMarkerText = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        actionIcon = transform.GetChild(1).GetComponent<Image>();
        numberOnMarker = index + 1;
        if (index < MarkerView.BLANK_MARKER_ID)
        {
            _mainImage.enabled = true;
            string value = index < 4 ? numberOnMarker.ToString() : "?";
            numberOnMarkerText.text = value;
            action = (MarkerAction)index;
            actionIcon.sprite = atlas.GetSprite("action_" + index.ToString());
        }
        else
        {
            actionIcon.enabled = false;
        }
        if(transform.parent.GetComponent<MarkerView>().GetType() == typeof(NpcMarkerView))
        {
            ToggleRayCast(false);
        }
    }

    public override void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (_status == MarkerStatus.PLACED)
            {
                MarkerHolder holder;
                holder = transform.parent.GetComponent<MarkerHolder>();
                if (holder == null)
                {
                    holder = _parent.GetComponent<MarkerHolder>();
                }
                _dispatcher.InvokeEventHandler(GameLogicEventType.MARKER_CANCELLED, new object[] { holder.holderType, this });
            }
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_status == MarkerStatus.NONE)
            {
                MarkerHolder holder = transform.parent.GetComponent<MarkerHolder>();
                _dispatcher.InvokeEventHandler(GameLogicEventType.MARKER_PLACED, new object[] { holder, this });
            }
        }
    }

    public void Rotate(MarkerDirection direction)
    {
        RectTransform markerTransform = GetComponent<RectTransform>();
        RectTransform valueTransform = numberOnMarkerText.gameObject.GetComponent<RectTransform>();
        RectTransform iconTransform = actionIcon.gameObject.GetComponent<RectTransform>();
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

    public void SetAlpha(bool isPlaced)
    {
        Color tempColor = _mainImage.color;
        tempColor.a = isPlaced ? 1f : 0.5f;
        _mainImage.color = tempColor;
    }

    public override void ToggleRayCast(bool value)
    {
        _mainImage.raycastTarget = value;
        numberOnMarkerText.raycastTarget = value;
        actionIcon.raycastTarget = value;
    }

    public void Fade(bool value, float duration)
    {
        float endValue = value ? 1f : 0f;
        DOTween.Sequence().Append(_mainImage.DOFade(endValue, duration)).SetEase(Ease.Linear);
    }

    public void SnapToHolderTween(Vector3 targetPosition, float duration)
    {
        DOTween.Sequence().Append(GetComponent<RectTransform>().DOMove(targetPosition, duration)).SetEase(Ease.InOutSine);
    }
}
