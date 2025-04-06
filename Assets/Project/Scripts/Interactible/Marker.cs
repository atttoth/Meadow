using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Timeline;
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

    public Sprite GetActionIcon()
    {
        return actionIcon.sprite;
    }

    public void CreateMarker(int index, Color32 color)
    {
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        name = $"marker{index}";
        ID = index;
        _status = MarkerStatus.NONE;
        numberOnMarkerText = transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        numberOnMarker = ID + 1;
        string value = ID < 4 ? numberOnMarker.ToString() : "?";
        numberOnMarkerText.text = value;
        _mainImage = GetComponent<Image>();
        _mainImage.color = color;
        action = (MarkerAction)ID;
        actionIcon = transform.GetChild(1).GetComponent<Image>();
        actionIcon.sprite = atlas.GetSprite("action_" + index.ToString());
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
                StartEventHandler(GameLogicEventType.MARKER_CANCELLED, new object[] { holder.holderType, this });
            }
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_status == MarkerStatus.NONE)
            {
                MarkerHolder holder = transform.parent.GetComponent<MarkerHolder>();
                StartEventHandler(GameLogicEventType.MARKER_PLACED, new object[] { holder, this });
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
}
