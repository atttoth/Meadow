using DG.Tweening;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class IconDisplayView : MonoBehaviour
{
    protected Transform _displayIconPoolTransform;
    protected Transform _iconsTransform;
    protected Transform _preparedIconsTransform;
    protected List<DisplayIcon> _displayIconPool;
    protected List<DisplayIcon> _displayIcons;
    protected List<DisplayIcon> _preparedDisplayIcons;
    private IconDisplayLayout _iconDisplayLayout;

    public void Init()
    {
        _displayIconPoolTransform = transform.GetChild(0); // IconDisplayView/DisplayIconPool
        _iconsTransform = transform.GetChild(1); // IconDisplayView/DisplayIcons
        _preparedIconsTransform = transform.GetChild(2); // IconDisplayView/PreparedDisplayIcons
        _displayIconPool = new();
        _displayIcons = new();
        _preparedDisplayIcons = new();
        _iconDisplayLayout = new IconDisplayLayout();
    }

    public void UpdateIcons(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                List<Card> primaryTableCards = task.Data.cards;
                primaryTableCards
                    .OrderBy(card => card.transform.parent.GetSiblingIndex())
                    .ToList()
                    .ForEach(card => PrepareDisplayIcon(card, task.Data.holders));
                task.StartDelayMs(0);
                break;
            case 1:
                task.StartHandler(SetupDisplayIconsPositionHandler, task.Data);
                break;
            case 2:
                task.StartHandler(ChangeDisplayIconsHandler, task.Data);
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
            displayIcon = Instantiate(GameAssets.Instance.displayIconPrefab, _displayIconPoolTransform).GetComponent<DisplayIcon>();
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

    private void PrepareDisplayIcon(Card card, List<CardHolder> activePrimaryCardHolders)
    {
        CardHolder holder = card.transform.parent.GetComponent<CardHolder>();
        int holderID = holder.ID;
        DisplayIcon displayIcon = GetDisplayIconFromPool();
        displayIcon.transform.SetParent(_preparedIconsTransform);
        displayIcon.transform.position = _preparedIconsTransform.position;
        displayIcon.SetUpDisplayIcon(holderID);
        List<CardIcon> icons = card.Data.icons.ToList();
        if (card.Data.cardType != CardType.Ground) // need to find a ground icon for displayIcon
        {
            List<CardIcon> groundIcons = activePrimaryCardHolders
                .Find(holder => holder.ID == holderID)
                .GetAllIconsOfHolder()
                .Where(cardIcon => (int)cardIcon < 5)
                .ToList();
            icons.AddRange(groundIcons);
        }
        displayIcon.SaveIcons(icons);
        _preparedDisplayIcons.Add(displayIcon);
    }

    private void SetupDisplayIconsPositionHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0:
                int duration = 0;
                if (_displayIcons.Count > 0)
                {
                    float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.displayIconHorizontalSlideSpeed;
                    List<int> oldHolderIDs = _displayIcons.Select(icon => icon.ID).ToList();
                    List<int> preparedHolderIDs = _preparedDisplayIcons.Select(icon => icon.ID).ToList();
                    List<int> newHolderIDs = preparedHolderIDs.Except(oldHolderIDs).ToList();
                    List<DisplayIcon> leftDisplayIcons = new();
                    List<DisplayIcon> rightDisplayIcons = new();
                    List<DisplayIcon> existingDisplayIcons = new();
                    int minSiblingIndex = task.Data.holders
                        .Where(holder => oldHolderIDs.Contains(holder.ID))
                        .Select(holder => holder.transform.GetSiblingIndex())
                        .Min();

                    newHolderIDs.ForEach(holderID =>
                    {
                        DisplayIcon newDisplayIcon = _preparedDisplayIcons.Find(icon => icon.ID == holderID);
                        int siblingIndex = task.Data.holders.Find(holder => holder.ID == holderID).transform.GetSiblingIndex();
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
                        duration = (int)(speed * 1000);
                        _displayIcons.ForEach(icon =>
                        {
                            Vector2 pos = _iconDisplayLayout.GetExistingIconPosition(icon.transform.position.x, icon.transform.position.y, slideDirectionValue);
                            icon.transform.DOMove(pos, speed).SetEase(Ease.InOutSine);
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

    private void ChangeDisplayIconsHandler(GameTask task)
    {
        switch (task.State)
        {
            case 0: // move new icons up, and old ones out
                float speed = ReferenceManager.Instance.gameLogicManager.GameSettings.displayIconVerticalSlideSpeed;
                _preparedDisplayIcons.ForEach(displayIcon =>
                {
                    DisplayIcon oldDisplayIcon = _displayIcons.Find(icon => icon.ID == displayIcon.ID);
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

                    if (oldDisplayIcon != null)
                    {
                        _displayIcons.Remove(oldDisplayIcon);
                        oldDisplayIcon.transform.DOMoveY(_displayIconPoolTransform.position.y, speed).OnComplete(() => AddDisplayIconToPool(oldDisplayIcon));
                    }
                    _displayIcons.Add(displayIcon);
                    displayIcon.transform.DOMoveY(_iconsTransform.position.y, speed).OnComplete(() => displayIcon.transform.SetParent(_iconsTransform));
                });
                _preparedDisplayIcons = new();
                task.StartDelayMs((int)(speed * 1000));
                break;
            case 1: // re-order icons in hierarchy
                _displayIcons.ForEach(displayIcon =>
                {
                    int siblingIndex = task.Data.holders.Find(holder => holder.ID == displayIcon.ID).transform.GetSiblingIndex();
                    displayIcon.transform.SetSiblingIndex(siblingIndex);
                });
                task.StartDelayMs(0);
                break;
            default:
                task.Complete();
                break;
        }
    }
}
