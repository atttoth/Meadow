using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

public class CampView : MonoBehaviour
{
    private static readonly Dictionary<int, int> _NUM_OF_CAMP_ICONS_PER_PLAYERS = new() { { 2, 5 }, { 3, 8 } };
    private static readonly List<CardIcon> _CAMP_ICONS = new() {
            CardIcon.Bird,
            CardIcon.Tree,
            CardIcon.Paw,
            CardIcon.Butterfly,
            CardIcon.House,
            CardIcon.Fence,
            CardIcon.Mushroom,
            CardIcon.Berry,
            CardIcon.Bloom,
            CardIcon.Wolf,
            CardIcon.Eagle,
            CardIcon.Deer
        };
    private Transform _screen;
    private List<ScreenDisplayItem> _campItems;
    private List<ScreenDisplayItem> _activeItems;
    private List<ScreenDisplayItem> _activeButtons;
    private List<Vector3> _itemPositions;
    public int selectionsLeft;

    public List<Button> Init()
    {
        _campItems = new();
        _activeItems = new();
        _screen = transform.GetChild(2);
        Transform campIconsTransform = transform.GetChild(0).transform;
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        _CAMP_ICONS.ForEach(icon =>
        {
            ScreenDisplayItem item = Object.Instantiate(GameAssets.Instance.screenDisplayItemPrefab, campIconsTransform).GetComponent<ScreenDisplayItem>();
            item.gameObject.SetActive(false);
            item.Init();
            item.type = icon;
            item.image.sprite = atlas.GetSprite("21"); // set back img of camp icon
            _campItems.Add(item);
        });
        return _campItems.Select(item => item.button).ToList();
    }

    public void ShowCampIcons()
    {
        selectionsLeft = _NUM_OF_CAMP_ICONS_PER_PLAYERS[2]; // locked to 2 players for now
        CreateItemPositions();
        _campItems.ForEach(item =>
        {
            item.button.enabled = true;
            item.gameObject.SetActive(true);
        });
    }

    public void FlipCampIcon(ScreenDisplayItem item)
    {
        selectionsLeft--;
        _activeItems.Add(item);
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        float halvedCardRotationSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardRotationSpeedOnBoard * 0.5f;
        Sequence iconFlip = DOTween.Sequence();
        iconFlip.Append(item.transform.DOScale(1.1f, halvedCardRotationSpeed)).Join(item.transform.DORotate(new Vector3(0f, 90f, 0f), halvedCardRotationSpeed).SetEase(Ease.Linear).OnComplete(() => item.image.sprite = atlas.GetSprite(((int)((CardIcon)item.type)).ToString())));
        iconFlip.Append(item.transform.DORotate(new Vector3(0f, 0f, 0f), halvedCardRotationSpeed)).SetEase(Ease.Linear);
        iconFlip.Append(item.transform.DOScale(1f, 0.05f)).SetEase(Ease.Linear);
    }

    private void CreateItemPositions()
    {
        _itemPositions = new();
        int length = selectionsLeft * 2;
        float radius = 300f;
        for (int i = 0; i < length; ++i)
        {
            float circleposition = (float)i / (float)length;
            float x = Mathf.Sin(circleposition * Mathf.PI * 2f);
            float y = Mathf.Cos(circleposition * Mathf.PI * 2f);
            Vector3 pos = (_screen.transform.position) + new Vector3(x, y, 0f) * radius;
            _itemPositions.Add(pos);
        }
    }

    public void SetIconsPositionHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.campIconPositionSpeed;
                int posIndex = 0;
                for (int i = 0; i < _activeItems.Count; ++i)
                {
                    ScreenDisplayItem item = _activeItems[i];
                    item.transform.SetParent(_screen);

                    Vector3 pos = _itemPositions[posIndex];
                    Sequence itemTween = DOTween.Sequence();
                    itemTween.Append(item.transform.DOMove(pos, speed)).SetEase(Ease.Linear);
                    posIndex += 2;
                }
                _campItems.Except(_activeItems).ToList().ForEach(item => item.gameObject.SetActive(false));
                task.StartDelayMs((int)(speed * 1000));
                break;
            case 1:
                task.StartDelayMs(2000);
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void ShowScoreButtonshandler(GameTask task)
    {
        _activeButtons = new();
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        Transform buttonTransform = transform.GetChild(1).transform;
        for (int i = 0; i < _activeItems.Count; ++i)
        {
            ScreenDisplayItem item = Object.Instantiate(GameAssets.Instance.screenDisplayItemPrefab, buttonTransform).GetComponent<ScreenDisplayItem>();
            item.gameObject.SetActive(false);
            item.Init();
            item.image.sprite = atlas.GetSprite("21"); // set proper button image
            _activeButtons.Add(item);
        }
        task.Complete();
    }
}
