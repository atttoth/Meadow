using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using Random = System.Random;

public class CampView : MonoBehaviour
{
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
    private static readonly Dictionary<int, int> _NUM_OF_CAMP_ICONS_PER_PLAYERS = new() { { 2, 5 }, { 3, 8 } };
    private RectTransform _screen;
    private List<ScreenDisplayItem> _campItems; // icons to choose from
    private List<ScreenDisplayItem> _scoreButtonItems; // score buttons
    private List<ScreenDisplayItem> _selectedItems; // active icons
    private Dictionary<string, ScreenDisplayItem[][]> _itemGroups; // values: index0-item button, index1-adjacent icons
    public int selectionsLeft;
    public bool isCampActionEnabled;

    public void Init()
    {
        _campItems = new();
        _selectedItems = new();
        _screen = transform.GetChild(1).GetComponent<RectTransform>();
    }

    public void SetNumOfIconsForRound(int value)
    {
        selectionsLeft = _NUM_OF_CAMP_ICONS_PER_PLAYERS[value];
    }

    public List<ScreenDisplayItem> CreateCampItems()
    {
        Transform campIconsTransform = transform.GetChild(0).transform;
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        _CAMP_ICONS.ForEach(icon =>
        {
            ScreenDisplayItem item = Instantiate(GameAssets.Instance.screenDisplayItemPrefab, campIconsTransform).GetComponent<ScreenDisplayItem>();
            item.gameObject.SetActive(false);
            item.Init();
            item.type = icon;
            item.image.sprite = atlas.GetSprite("21"); // set back img of camp icon
            _campItems.Add(item);
        });
        return _campItems;
    }

    public List<ScreenDisplayItem> CreateItemButtons()
    {
        _scoreButtonItems = new();
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        for (int i = 0; i < selectionsLeft; ++i)
        {
            ScreenDisplayItem item = Instantiate(GameAssets.Instance.screenDisplayItemPrefab, _screen).GetComponent<ScreenDisplayItem>();
            item.Init();
            item.button.enabled = false;
            item.GetComponent<Image>().enabled = false;
            item.image.color = new Color(item.image.color.g, item.image.color.b, item.image.color.r, 0f);
            item.image.sprite = atlas.GetSprite("campButton_incomplete");
            SpriteState spriteState = item.button.spriteState;
            spriteState.selectedSprite = atlas.GetSprite("campButton_incomplete");
            item.button.spriteState = spriteState;
            item.gameObject.SetActive(true);
            _scoreButtonItems.Add(item);
        }
        return _scoreButtonItems;
    }

    public void ShowCampIcons()
    {
        _campItems.ForEach(item =>
        {
            item.GetComponent<Image>().enabled = false;
            item.button.enabled = true;
            item.gameObject.SetActive(true);
        });
    }

    public void FlipCampIcon(ScreenDisplayItem item)
    {
        selectionsLeft--;
        _selectedItems.Add(item);
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        float halvedCardRotationSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.cardRotationSpeedOnBoard * 0.5f;
        Sequence iconFlip = DOTween.Sequence();
        iconFlip.Append(item.transform.DOScale(1.1f, halvedCardRotationSpeed)).Join(item.transform.DORotate(new Vector3(0f, 90f, 0f), halvedCardRotationSpeed).SetEase(Ease.Linear).OnComplete(() => item.image.sprite = atlas.GetSprite(((int)((CardIcon)item.type)).ToString())));
        iconFlip.Append(item.transform.DORotate(new Vector3(0f, 0f, 0f), halvedCardRotationSpeed)).SetEase(Ease.Linear);
        iconFlip.Append(item.transform.DOScale(1f, 0.05f)).SetEase(Ease.Linear);
    }

    public void SetIconsPositionHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                float iconFadeDuration = ReferenceManager.Instance.gameLogicManager.GameSettings.campIconFadeDuration;
                _campItems.Except(_selectedItems).ToList().ForEach(item => DOTween.Sequence().Append(item.image.DOFade(0f, iconFadeDuration)));
                task.StartDelayMs((int)(iconFadeDuration * 1000));
                break;
            case 1:
                _selectedItems.Sort((a, b) => (a.transform.position.x).CompareTo(b.transform.position.x));
                float iconGroupPositionSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.campIconGroupPositionSpeed;
                Rect screenRect = _screen.rect;
                Rect itemRect = _selectedItems.First().GetComponent<RectTransform>().rect;
                float gap = 40f;
                float itemsWidth = ((_selectedItems.Count - 1) * gap) + (_selectedItems.Count * itemRect.width);
                float startingPosX = (screenRect.width - itemsWidth) * 0.5f;
                float posY = screenRect.height - (itemRect.height * 0.5f);
                for(int i = 0; i < _selectedItems.Count; i++)
                {
                    ScreenDisplayItem item = _selectedItems[i];
                    item.transform.SetParent(_screen);
                    float posX = startingPosX + (i * gap) + (i * itemRect.width) + (itemRect.width * 0.5f);
                    Vector3 position = new(posX, posY, 0f);
                    DOTween.Sequence().Append(item.transform.DOMove(position, iconGroupPositionSpeed));
                }
                _campItems.Except(_selectedItems).ToList().ForEach(item => item.gameObject.SetActive(false));
                task.StartDelayMs((int)(iconGroupPositionSpeed * 1000));
                break;
            case 2:
                List<Vector3> itemPositions = CreateItemPositions();
                List<Vector3> iconPositions = new();
                int posIndex = 0;
                for (int i = 0; i < _selectedItems.Count; i++) // get icon positions
                {
                    iconPositions.Add(itemPositions[posIndex]);
                    posIndex += 2;
                }

                Random random = new();
                for (int i = _selectedItems.Count - 1; i > 0; i--) // randomize icons order
                {
                    int indexToSwap = random.Next(i + 1);
                    (_selectedItems[indexToSwap], _selectedItems[i]) = (_selectedItems[i], _selectedItems[indexToSwap]);
                }

                List<Vector3> buttonPositions = itemPositions.Except(iconPositions).ToList();
                _itemGroups = new();
                for (int i = 0; i < _scoreButtonItems.Count; i++) // position and link buttons with adjacent icons
                {
                    int idx = i == _scoreButtonItems.Count - 1 ? 0 : (i + 1);
                    string key = i.ToString();
                    ScreenDisplayItem buttonItem = _scoreButtonItems[i];
                    buttonItem.type = key;
                    buttonItem.GetComponent<RectTransform>().position = buttonPositions[i];
                    ScreenDisplayItem item1 = _selectedItems[i];
                    ScreenDisplayItem item2 = _selectedItems[idx];
                    ScreenDisplayItem[][] items = new ScreenDisplayItem[2][];
                    items[0] = new ScreenDisplayItem[] { buttonItem };
                    items[1] = new ScreenDisplayItem[] { item1, item2 };
                    _itemGroups[key] = items;
                }

                float iconPositionSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.campIconSinglePositionSpeed;
                float iconPositionDelay = ReferenceManager.Instance.gameLogicManager.GameSettings.campIconPositionDelay;
                int duration = (int)(((_selectedItems.Count - 1) * iconPositionDelay + iconPositionSpeed) * 1000);
                int index = 0;
                while (_selectedItems.Count > index) // position icons
                {
                    float delay = index * iconPositionDelay;
                    ScreenDisplayItem item = _selectedItems[index];
                    Vector3 pos = iconPositions[index];
                    DOTween.Sequence().Append(item.transform.DOMove(pos, iconPositionSpeed).SetEase(Ease.Linear).SetDelay(delay));
                    index++;
                }
                task.StartDelayMs(duration);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private List<Vector3> CreateItemPositions()
    {
        List<Vector3> itemPositions = new();
        int length = _selectedItems.Count * 2;
        float radius = 300f;
        for (int i = 0; i < length; ++i)
        {
            float circleposition = (float)i / (float)length + 0.25f;
            float x = Mathf.Sin(circleposition * Mathf.PI * 2f);
            float y = Mathf.Cos(circleposition * Mathf.PI * 2f);
            Vector3 pos = (_screen.transform.position) + new Vector3(x, y, 0f) * radius;
            itemPositions.Add(pos);
        }
        return itemPositions;
    }

    public void ShowScoreButtonsHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                float iconFadeDuration = ReferenceManager.Instance.gameLogicManager.GameSettings.campIconFadeDuration;
                foreach (ScreenDisplayItem[][] items in _itemGroups.Values)
                {
                    ScreenDisplayItem item = items[0][0]; // item button
                    DOTween.Sequence().Append(item.image.DOFade(1f, iconFadeDuration));
                    item.button.enabled = true;
                }
                task.StartDelayMs((int)(iconFadeDuration * 1000));
                break;
            default:
                task.Complete();
                break;
        }
    }

    public int OnItemButtonClick(ScreenDisplayItem item)
    {
        // check if adjacent icons condition fulfilled
        // add score handler - event?
        if(isCampActionEnabled)
        {
            //item.button.enabled = false;
            //SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
            //item.image.sprite = atlas.GetSprite("campButton_complete");
        }
        string key = item.type.ToString();
        ScreenDisplayItem[] items = _itemGroups[key][1];
        items.ToList().ForEach(item =>
        {
            Debug.Log(item.type.ToString());
        });
        return 0; // return score
    }

    public void ResetCampView()
    {
        _campItems.ForEach(item => Destroy(item.gameObject));
        _campItems.Clear();
        _scoreButtonItems.ForEach(item => Destroy(item.gameObject));
        _scoreButtonItems.Clear();
        _selectedItems.ForEach(item => Destroy(item.gameObject));
        _selectedItems.Clear();
        _itemGroups.Clear();
    }
}
