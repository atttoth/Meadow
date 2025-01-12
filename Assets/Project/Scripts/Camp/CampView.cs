using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using static UnityEditor.Progress;
using Random = System.Random;

public class CampView : ViewBase
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
    private RectTransform _display;
    private List<ScreenDisplayItem> _campItemSelection; // icons to choose from
    private List<ScreenDisplayItem> _scoreButtonItems; // score buttons
    private List<ScreenDisplayItem> _selectedItems; // active icons
    private List<int> _usedScoreButtonIDs;
    private int _playerCampScoreToken;
    public int selectionsLeft;
    public bool isCampActionEnabled;

    public override void Init()
    {
        _campItemSelection = new();
        _scoreButtonItems = new();
        _selectedItems = new();
        _usedScoreButtonIDs = new();
        _display = transform.GetChild(1).GetComponent<RectTransform>();
    }

    public int PlayerCampScoreToken { get { return _playerCampScoreToken; } }

    public void SetNumOfIconsForRound(int value)
    {
        selectionsLeft = _NUM_OF_CAMP_ICONS_PER_PLAYERS[value];
    }

    private void ShuffleItems(List<ScreenDisplayItem> list) // shuffles both list items and objects in hierarchy
    {
        Random random = new Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int i = random.Next(n + 1);
            ScreenDisplayItem item = list[i];
            list[i] = list[n];
            list[n] = item;
        }
        list.ForEach(item => item.transform.SetSiblingIndex(list.IndexOf(item)));
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
            item.mainImage.sprite = atlas.GetSprite("21"); // set back img of camp icon
            _campItemSelection.Add(item);
        });
        ShuffleItems(_campItemSelection);

        return _campItemSelection;
    }

    public List<ScreenDisplayItem> CreateItemButtons()
    {
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        for (int i = 0; i < selectionsLeft; ++i)
        {
            ScreenDisplayItem item = Instantiate(GameAssets.Instance.screenDisplayItemPrefab, _display).GetComponent<ScreenDisplayItem>();
            item.Init();
            item.ID = i;
            item.button.enabled = false;
            item.GetComponent<Image>().enabled = false;
            item.mainImage.color = new Color(item.mainImage.color.g, item.mainImage.color.b, item.mainImage.color.r, 0f);
            item.mainImage.sprite = atlas.GetSprite("campButton_incomplete");
            SpriteState spriteState = item.button.spriteState;
            spriteState.selectedSprite = atlas.GetSprite("campButton_incomplete");
            item.button.spriteState = spriteState;
            item.gameObject.SetActive(true);
            _scoreButtonItems.Add(item);
        }
        return _scoreButtonItems;
    }

    public void ShowCampIconSelection()
    {
        _campItemSelection.ForEach(item =>
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
        iconFlip.Append(item.transform.DOScale(1.1f, halvedCardRotationSpeed)).Join(item.transform.DORotate(new Vector3(0f, 90f, 0f), halvedCardRotationSpeed).SetEase(Ease.Linear).OnComplete(() => item.mainImage.sprite = atlas.GetSprite(((int)((CardIcon)item.type)).ToString())));
        iconFlip.Append(item.transform.DORotate(new Vector3(0f, 0f, 0f), halvedCardRotationSpeed)).SetEase(Ease.Linear);
        iconFlip.Append(item.transform.DOScale(1f, 0.05f)).SetEase(Ease.Linear);
    }

    public void SetIconsPositionHandler(GameTask task)
    {
        switch(task.State)
        {
            case 0:
                float iconFadeDuration = ReferenceManager.Instance.gameLogicManager.GameSettings.campIconFadeDuration;
                _campItemSelection.Except(_selectedItems).ToList().ForEach(item => DOTween.Sequence().Append(item.mainImage.DOFade(0f, iconFadeDuration)));
                task.StartDelayMs((int)(iconFadeDuration * 1000));
                break;
            case 1:
                _selectedItems.Sort((a, b) => (a.transform.position.x).CompareTo(b.transform.position.x));
                float iconGroupPositionSpeed = ReferenceManager.Instance.gameLogicManager.GameSettings.campIconGroupPositionSpeed;
                Rect screenRect = _display.rect;
                Rect itemRect = _selectedItems.First().GetComponent<RectTransform>().rect;
                float gap = 40f;
                float itemsWidth = ((_selectedItems.Count - 1) * gap) + (_selectedItems.Count * itemRect.width);
                float startingPosX = (screenRect.width - itemsWidth) * 0.5f;
                float posY = screenRect.height - (itemRect.height * 0.5f);
                for(int i = 0; i < _selectedItems.Count; i++)
                {
                    ScreenDisplayItem item = _selectedItems[i];
                    item.transform.SetParent(_display);
                    float posX = startingPosX + (i * gap) + (i * itemRect.width) + (itemRect.width * 0.5f);
                    Vector3 position = new(posX, posY, 0f);
                    DOTween.Sequence().Append(item.transform.DOMove(position, iconGroupPositionSpeed));
                }
                _campItemSelection.Except(_selectedItems).ToList().ForEach(item => item.gameObject.SetActive(false));
                task.StartDelayMs((int)(iconGroupPositionSpeed * 1000));
                break;
            case 2:
                ShuffleItems(_selectedItems); // randomize icons order
                List<Vector3> itemPositions = CreateItemPositions();
                List<Vector3> iconPositions = new();
                int posIndex = 0;
                for (int i = 0; i < _selectedItems.Count; i++) // get icon positions
                {
                    iconPositions.Add(itemPositions[posIndex]);
                    posIndex += 2;
                }
                
                List<Vector3> buttonPositions = itemPositions.Except(iconPositions).ToList();
                for (int i = 0; i < _scoreButtonItems.Count; i++) // position and link buttons with adjacent icons
                {
                    int idx = i == _scoreButtonItems.Count - 1 ? 0 : (i + 1);
                    string key = i.ToString();
                    ScreenDisplayItem buttonItem = _scoreButtonItems[i];
                    buttonItem.GetComponent<RectTransform>().position = buttonPositions[i];
                    CardIcon icon1 = (CardIcon)_selectedItems[i].type;
                    CardIcon icon2 = (CardIcon)_selectedItems[idx].type;
                    buttonItem.type = new List<CardIcon>() { icon1, icon2 };
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
            Vector3 pos = (_display.transform.position) + new Vector3(x, y, 0f) * radius;
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
                _scoreButtonItems.ForEach(item => DOTween.Sequence().Append(item.mainImage.DOFade(1f, iconFadeDuration)));
                task.StartDelayMs((int)(iconFadeDuration * 1000));
                break;
            default:
                task.Complete();
                break;
        }
    }

    public void OnItemButtonClick(ScreenDisplayItem buttonItem)
    {
        _scoreButtonItems.ForEach(item => item.button.enabled = false);
        buttonItem.message.text = "";
        _usedScoreButtonIDs.Add(buttonItem.ID);
        SpriteAtlas atlas = GameAssets.Instance.baseAtlas;
        buttonItem.mainImage.sprite = atlas.GetSprite("campButton_complete");
    }

    public void UpdateCampScoreToken(int playerCampScoreToken)
    {
        _playerCampScoreToken = playerCampScoreToken;
    }

    public void CheckFulfilledAdjacentIcons(List<List<CardIcon>> iconPairs)
    {
        if(iconPairs.Count < 1 || _usedScoreButtonIDs.Count == _scoreButtonItems.Count)
        {
            return;
        }
        else if(_playerCampScoreToken == 0)
        {
            ToggleItemButtons(new List<int>());
            return;
        }

        List<ScreenDisplayItem> availableItems = _scoreButtonItems.Where(item => !_usedScoreButtonIDs.Contains(item.ID)).ToList();
        List<int> enabledItemIDs = new();
        for (int i = 0; i < iconPairs.Count; i++)
        {
            List<CardIcon> icons = iconPairs[i];
            CardIcon icon1 = icons[0];
            CardIcon icon2 = icons[1];
            for(int j = 0; j < availableItems.Count; j++)
            {
                ScreenDisplayItem item = availableItems[j];
                List<CardIcon> campIcons = (List<CardIcon>)item.type;
                if (campIcons.Contains(icon1) && campIcons.Contains(icon2))
                {
                    enabledItemIDs.Add(item.ID);
                }
            }
        }
        ToggleItemButtons(enabledItemIDs.Distinct().ToList());
    }

    private void ToggleItemButtons(List<int> enabledItemIDs)
    {
        _scoreButtonItems.ForEach(item =>
        {
            item.message.text = _playerCampScoreToken.ToString();
            item.button.enabled = enabledItemIDs.Contains(item.ID);
        });
    }

    public void ResetCampView()
    {
        _campItemSelection.ForEach(item => Destroy(item.gameObject));
        _campItemSelection.Clear();
        _scoreButtonItems.ForEach(item => Destroy(item.gameObject));
        _scoreButtonItems.Clear();
        _selectedItems.ForEach(item => Destroy(item.gameObject));
        _selectedItems.Clear();
        _usedScoreButtonIDs.Clear();
    }
}
