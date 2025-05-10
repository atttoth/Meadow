using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class IconDisplayView : MonoBehaviour
{
    protected Transform _displayIconPoolTransform;
    protected Transform _iconsTransform;
    protected Transform _preparedIconsTransform;
    protected List<DisplayIcon> _displayIconPool;
    protected List<DisplayIcon> _displayIcons;
    protected List<DisplayIcon> _preparedDisplayIcons;
    protected List<DisplayIcon> _oldDisplayIcons;
    private IconDisplayLayout _iconDisplayLayout;
    protected Image _activeUserFrame;

    public void Init()
    {
        _displayIconPoolTransform = transform.GetChild(0); // IconDisplayView/DisplayIconPool
        _iconsTransform = transform.GetChild(1); // IconDisplayView/DisplayIcons
        _preparedIconsTransform = transform.GetChild(2); // IconDisplayView/PreparedDisplayIcons
        _displayIconPool = new();
        _displayIcons = new();
        _preparedDisplayIcons = new();
        _oldDisplayIcons = new();
        _iconDisplayLayout = new IconDisplayLayout();
        _activeUserFrame = transform.GetChild(3).GetComponent<Image>();
        ToggleActiveUserFrame(false);
    }

    public void ToggleActiveUserFrame(bool value)
    {
        _activeUserFrame.enabled = value;
    }

    public void UpdateIconsHandler(GameTask task, List<Card> cards, List<HolderData> holderDataCollection)
    {
        switch (task.State)
        {
            case 0:
                if(cards.Count > 0)
                {
                    List<ArrayList> dataPairs = new();
                    for (int i = 0; i < holderDataCollection.Count; i++)
                    {
                        HolderData holderData = holderDataCollection[i];
                        for (int j = 0; j < cards.Count; j++)
                        {
                            Card card = cards[j];
                            if(holderData.ContentList.Contains(card))
                            {
                                dataPairs.Add(new ArrayList { holderDataCollection.IndexOf(holderData), card.Data, holderData });
                            }
                        }
                    }
                    dataPairs
                        .OrderBy(list => (int)list[0])
                        .ToList()
                        .ForEach(list => PrepareDisplayIcon((CardData)list[1], (HolderData)list[2]));
                    task.StartDelayMs(0);
                }
                else
                {
                    task.NextState(3);
                }
                break;
            case 1:
                task.StartHandler((Action<GameTask, List<HolderData>>)SetupDisplayIconsPositionHandler, holderDataCollection);
                break;
            case 2:
                task.StartHandler((Action<GameTask, List<HolderData>>)ChangeDisplayIconsHandler, holderDataCollection);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private DisplayIcon GetDisplayIconFromPool()
    {
        DisplayIcon displayIcon;
        if (_displayIconPool.Count < 1)
        {
            displayIcon = Instantiate(GameResourceManager.Instance.displayIconPrefab, _displayIconPoolTransform).GetComponent<DisplayIcon>();
            displayIcon.gameObject.SetActive(false);
        }
        else
        {
            displayIcon = _displayIconPool.First();
            _displayIconPool.RemoveAt(0);
        }
        return displayIcon;
    }

    private void AddDisplayIconToPool(DisplayIcon displayIcon)
    {
        _displayIconPool.Add(displayIcon);
        displayIcon.gameObject.SetActive(false);
        displayIcon.transform.SetParent(_displayIconPoolTransform);
        displayIcon.transform.position = _displayIconPoolTransform.position;
    }

    private void PrepareDisplayIcon(CardData cardData, HolderData holderData)
    {
        DisplayIcon displayIcon = GetDisplayIconFromPool();
        displayIcon.transform.SetParent(_preparedIconsTransform);
        displayIcon.transform.position = _preparedIconsTransform.position;
        displayIcon.SetUpDisplayIcon(holderData.ID);
        List<CardIcon> icons = cardData.icons.ToList();
        if (cardData.cardType != CardType.Ground) // need to find a ground icon for displayIcon
        {
            List<CardIcon> groundIcons = holderData
                .GetAllIconsOfHolder()
                .Where(cardIcon => (int)cardIcon < 5)
                .ToList();
            icons.AddRange(groundIcons);
        }
        displayIcon.SaveIcons(icons);
        _preparedDisplayIcons.Add(displayIcon);
    }

    private void SetupDisplayIconsPositionHandler(GameTask task, List<HolderData> holderDataCollection)
    {
        switch (task.State)
        {
            case 0:
                int duration = 0;
                if (_displayIcons.Count > 0)
                {
                    List<int> oldHolderIDs = _displayIcons.Select(icon => icon.ID).ToList();
                    List<int> preparedHolderIDs = _preparedDisplayIcons.Select(icon => icon.ID).ToList();
                    List<int> newHolderIDs = preparedHolderIDs.Except(oldHolderIDs).ToList();
                    List<DisplayIcon> leftDisplayIcons = new();
                    List<DisplayIcon> rightDisplayIcons = new();
                    List<DisplayIcon> existingDisplayIcons = new();
                    int minSiblingIndex = holderDataCollection
                        .Where(data => oldHolderIDs.Contains(data.ID))
                        .Select(data => GetSiblingIndexByHolderData(holderDataCollection, data))
                        .Min();

                    newHolderIDs.ForEach(holderID =>
                    {
                        DisplayIcon newDisplayIcon = _preparedDisplayIcons.Find(icon => icon.ID == holderID);
                        HolderData holderData = holderDataCollection.Find(data => data.ID == holderID);
                        int siblingIndex = GetSiblingIndexByHolderData(holderDataCollection, holderData);
                        if (siblingIndex < minSiblingIndex)
                        {
                            leftDisplayIcons.Add(newDisplayIcon);
                        }
                        else
                        {
                            rightDisplayIcons.Add(newDisplayIcon);
                        }
                    });
                    existingDisplayIcons = _preparedDisplayIcons.Where(icon => oldHolderIDs.Contains(icon.ID)).ToList();

                    int slideDirectionValue = (leftDisplayIcons.Count * -1) + rightDisplayIcons.Count;

                    // position left icons
                    if (leftDisplayIcons.Count > 0)
                    {
                        float leftPosX = _displayIcons.Find(icon => icon.transform.GetSiblingIndex() == 0).transform.position.x;
                        int modifier = leftDisplayIcons.Count;
                        leftDisplayIcons.ForEach(icon =>
                        {
                            icon.transform.position = _iconDisplayLayout.GetNewIconPosition(leftPosX, icon.transform.position.y, modifier, slideDirectionValue);
                            modifier--;
                        });
                    }

                    // position right icons
                    if (rightDisplayIcons.Count > 0)
                    {
                        float rightPosX = _displayIcons.Find(icon => icon.transform.GetSiblingIndex() == _displayIcons.Count - 1).transform.position.x;
                        int modifier = -rightDisplayIcons.Count;
                        rightDisplayIcons.Reverse();
                        rightDisplayIcons.ForEach(icon =>
                        {
                            icon.transform.position = _iconDisplayLayout.GetNewIconPosition(rightPosX, icon.transform.position.y, modifier, slideDirectionValue);
                            modifier++;
                        });
                    }

                    // position remaining (existing) icons
                    if (existingDisplayIcons.Count > 0)
                    {
                        existingDisplayIcons.ForEach(icon =>
                        {
                            icon.transform.position = _iconDisplayLayout.GetExistingIconPosition(_displayIcons.Find(oldIcon => oldIcon.ID == icon.ID).transform.position.x, icon.transform.position.y, slideDirectionValue);
                        });
                    }

                    // slide old icons
                    if (slideDirectionValue != 0)
                    {
                        float speed = GameSettings.Instance.GetDuration(Duration.displayIconHorizontalSlideSpeed);
                        duration = (int)(speed * 1000);
                        _displayIcons.ForEach(icon =>
                        {
                            float posX = _iconDisplayLayout.GetExistingIconPosition(icon.transform.position.x, icon.transform.position.y, slideDirectionValue).x;
                            icon.PosXTween(posX, speed);
                        });
                    }
                }
                else
                {
                    // no displayIcons present
                    List<DisplayIcon> initialDisplayIcons = _preparedDisplayIcons;
                    int modifier = -(initialDisplayIcons.Count - 1);
                    initialDisplayIcons.Reverse();
                    initialDisplayIcons.ForEach(icon =>
                    {
                        icon.transform.position = _iconDisplayLayout.GetNewIconPosition(icon.transform.position.x, icon.transform.position.y, modifier, initialDisplayIcons.Count - 1);
                        modifier++;
                    });
                }
                task.StartDelayMs(duration);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private void ChangeDisplayIconsHandler(GameTask task, List<HolderData> holderDataCollection)
    {
        switch (task.State)
        {
            case 0: // move new icons up, and old ones out
                float speed = GameSettings.Instance.GetDuration(Duration.displayIconVerticalSlideSpeed);
                _preparedDisplayIcons.ForEach(displayIcon =>
                {
                    DisplayIcon oldDisplayIcon = _displayIcons.Find(icon => icon.ID == displayIcon.ID);
                    displayIcon.transform.SetParent(_iconsTransform);
                    displayIcon.transform.position = new(displayIcon.transform.position.x, displayIcon.transform.position.y - 100f);
                    displayIcon.gameObject.SetActive(true);

                    if (displayIcon.MainIcons.Count > 1)
                    {
                        displayIcon.PlayIconToggle();
                    }
                    else if (displayIcon.MainIcons.Count == 1)
                    {
                        displayIcon.SetIconImage();
                    }
                    else
                    {
                        displayIcon.ChangeAlpha(0);
                    }

                    if (displayIcon.GroundIcons.Count > 1)
                    {
                        displayIcon.PlayBackgroundToggle();
                    }
                    else
                    {
                        displayIcon.background.color = displayIcon.GetColorByGroundIcon(displayIcon.GroundIcons.First());
                    }

                    if (oldDisplayIcon)
                    {
                        _displayIcons.Remove(oldDisplayIcon);
                        _oldDisplayIcons.Add(oldDisplayIcon);
                        oldDisplayIcon.posYTween(oldDisplayIcon.transform.position.y + 100f, speed, false);
                    }
                    _displayIcons.Add(displayIcon);
                    displayIcon.posYTween(displayIcon.transform.position.y + 100f, speed, true);
                });
                task.StartDelayMs((int)(speed * 1000));
                break;
            case 1: // re-order icons in hierarchy
                _oldDisplayIcons.ForEach(icon => AddDisplayIconToPool(icon));
                _oldDisplayIcons = new();
                _preparedDisplayIcons = new();
                _displayIcons.ForEach(displayIcon =>
                {
                    HolderData holderData = holderDataCollection.Find(data => data.ID == displayIcon.ID);
                    int siblingIndex = GetSiblingIndexByHolderData(holderDataCollection, holderData);
                    displayIcon.transform.SetSiblingIndex(siblingIndex);
                });
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }

    private int GetSiblingIndexByHolderData(List<HolderData> holderDataCollection, HolderData holderData)
    {
        return holderDataCollection.IndexOf(holderData);
    }
}
