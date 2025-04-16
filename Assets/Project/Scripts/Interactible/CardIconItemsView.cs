using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static UnityEditor.Progress;

public class CardIconItemsView : MonoBehaviour
{
    private static int ITEM_INDEX = 0;
    private readonly static float TOP_ICON_DIMENSION = 40f;
    private readonly static float REQUIRED_ICON_DIMENSION = 25f;

    private CardIconItemsLayout _iconItemsLayout;
    private Holder _topIconItemsHolder;
    private Holder _requiredIconItemsHolder;
    private CardIconItem _scoreItem;

    public Vector3 GetScoreItemPosition()
    {
        return _scoreItem.transform.position;
    }

    public int GetRequiredIconItemsNumber()
    {
        return _requiredIconItemsHolder.GetContentListSize();
    }

    public void Init(CardData data)
    {
        if(_iconItemsLayout == null)
        {
            _iconItemsLayout = new CardIconItemsLayout();
            _topIconItemsHolder = Instantiate(GameResourceManager.Instance.topIconsHolderPrefab, transform).GetComponent<Holder>();
            _requiredIconItemsHolder = Instantiate(GameResourceManager.Instance.requiredIconsHolderPrefab, transform).GetComponent<Holder>();
        }
        else // delete icons from prev initialization
        {
            _topIconItemsHolder.transform.GetComponentsInChildren<CardIconItem>().ToList().ForEach(item => Destroy(item.gameObject));
            _requiredIconItemsHolder.transform.GetComponentsInChildren<CardIconItem>().ToList().ForEach(item => Destroy(item.gameObject));
            if(_scoreItem)
            {
                Destroy(_scoreItem.gameObject);
            }
        }
        _topIconItemsHolder.Init(-1, HolderType.CardIcon);
        _requiredIconItemsHolder.Init(-1, HolderType.CardIcon);

        data.icons.ToList().ForEach(icon =>
        {
            CardIconItem item = Instantiate(GameResourceManager.Instance.cardIconItemPrefab, _topIconItemsHolder.transform).GetComponent<CardIconItem>();
            item.Create(new List<CardIcon>() { icon }, IconItemType.SINGLE, TOP_ICON_DIMENSION, -1);
            item.ToggleRayCast(false);
            _topIconItemsHolder.AddToHolder(item);
        });

        List<Interactable> topIconItems = new();
        topIconItems.AddRange(_topIconItemsHolder.GetAllContent());
        if (data.cardType == CardType.Ground) // ground icons are positioned to the bottom
        {
            List<CardIconItem> groundIconItems = new();
            for(int i = topIconItems.Count - 1; i >= 0; i--)
            {
                CardIconItem item = (CardIconItem)topIconItems[i];
                if((int)(item.Icons[0]) < 5)
                {
                    topIconItems.Remove(item);
                    groundIconItems.Add(item);
                }
            }

            Vector2[] groundIconPositions = _iconItemsLayout.GetTopIconItemPositions(groundIconItems.Count, TOP_ICON_DIMENSION, true);
            for (int i = 0; i < groundIconPositions.Length; i++)
            {
                CardIconItem item = groundIconItems[i];
                RectTransform rect = item.GetComponent<RectTransform>();
                rect.anchoredPosition = groundIconPositions[i];
            }
            topIconItems.Reverse();
        }

        Vector2[] topIconPositions = _iconItemsLayout.GetTopIconItemPositions(topIconItems.Count, TOP_ICON_DIMENSION);
        for (int i = 0; i < topIconPositions.Length; i++)
        {
            CardIconItem item = (CardIconItem)topIconItems[i];
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchoredPosition = topIconPositions[i];
        }
        
        if (data.cardType != CardType.Ground)
        {
            if (data.requirements.Length > 0)
            {
                if(data.requirements.Contains(CardIcon.AllMatching))
                {
                    // todo
                }
                else if(data.requirements.Contains(CardIcon.AllDifferent))
                {
                    // todo
                }
                else
                {
                    data.requirements.ToList().ForEach(icon =>
                    {
                        CardIconItem item = Instantiate(GameResourceManager.Instance.cardIconItemPrefab, _requiredIconItemsHolder.transform).GetComponent<CardIconItem>();
                        item.Create(new List<CardIcon>() { icon }, IconItemType.SINGLE, REQUIRED_ICON_DIMENSION, ITEM_INDEX++);
                        _requiredIconItemsHolder.AddToHolder(item);
                    });
                }
            }
            
            if(data.optionalRequirements.Length > 0)
            {
                List<CardIcon> optionalrequirements = data.optionalRequirements.ToList();
                for (int i = 0; i < optionalrequirements.Count; i++)
                {
                    if (i % 2 == 0)
                    {
                        CardIcon icon1 = optionalrequirements[i];
                        CardIcon icon2 = optionalrequirements[i + 1];
                        List<CardIcon> pair = new() { icon1, icon2 };
                        CardIconItem item = Instantiate(GameResourceManager.Instance.cardIconItemPrefab, _requiredIconItemsHolder.transform).GetComponent<CardIconItem>();
                        item.Create(pair, IconItemType.OPTIONAL, REQUIRED_ICON_DIMENSION, ITEM_INDEX++);
                        _requiredIconItemsHolder.AddToHolder(item);
                    }
                }
            }

            if(data.adjacentRequirements.Length > 0)
            {
                if(data.adjacentRequirements.Length == 1)
                {
                    CardIconItem item = Instantiate(GameResourceManager.Instance.cardIconItemPrefab, _requiredIconItemsHolder.transform).GetComponent<CardIconItem>();
                    item.Create(new List<CardIcon>() { data.adjacentRequirements[0] }, IconItemType.ADJACENT, REQUIRED_ICON_DIMENSION, ITEM_INDEX++);
                    _requiredIconItemsHolder.AddToHolder(item);
                }
                else
                {
                    List<CardIcon> optionalAndAdjacentRequirements = data.adjacentRequirements.ToList();
                    for (int i = 0; i < optionalAndAdjacentRequirements.Count; i++)
                    {
                        if (i % 2 == 0)
                        {
                            CardIcon icon1 = optionalAndAdjacentRequirements[i];
                            CardIcon icon2 = optionalAndAdjacentRequirements[i + 1];
                            List<CardIcon> pair = new() { icon1, icon2 };
                            CardIconItem item = Instantiate(GameResourceManager.Instance.cardIconItemPrefab, _requiredIconItemsHolder.transform).GetComponent<CardIconItem>();
                            item.Create(pair, IconItemType.OPTIONAL_AND_ADJACENT, REQUIRED_ICON_DIMENSION, ITEM_INDEX++);
                            _requiredIconItemsHolder.AddToHolder(item);
                        }
                    }
                }
            }

            PositionRequiredIconItems();

            if(data.score > 0)
            {
                _scoreItem = Instantiate(GameResourceManager.Instance.cardIconItemPrefab, transform).GetComponent<CardIconItem>();
                _scoreItem.Create(null, IconItemType.SCORE, TOP_ICON_DIMENSION, -1, data.score);
                _scoreItem.ToggleRayCast(false);
                _scoreItem.GetComponent<RectTransform>().anchoredPosition = _iconItemsLayout.GetScoreItemPosition();
            }
        }
        ITEM_INDEX = 0;
        Toggle(false);
    }

    public void ToggleRequiredIconsRaycast(bool value)
    {
        List<Interactable> requiredIconItems = _requiredIconItemsHolder.GetAllContent();
        for (int i = 0; i < requiredIconItems.Count; i++)
        {
            CardIconItem item = (CardIconItem)requiredIconItems[i];
            if(!item.Icons.Contains(CardIcon.RoadToken)) // road token requirement cannot be removed
            {
                item.ToggleRayCast(value);
            }
        }
    }

    public void UpdateDisposeStatusOfItems(int iconItemID)
    {
        List<Interactable> requiredIconItems = _requiredIconItemsHolder.GetAllContent();
        for (int i = 0; i < requiredIconItems.Count; i++)
        {
            CardIconItem item = (CardIconItem)requiredIconItems[i];
            item.selectedToDispose = item.ID == iconItemID;
        }
    }

    public bool HasIconItemSelectedForDispose()
    {
        List<Interactable> requiredIconItems = _requiredIconItemsHolder.GetAllContent();
        for (int i = 0; i < requiredIconItems.Count; i++)
        {
            CardIconItem item = (CardIconItem)requiredIconItems[i];
            if(item.selectedToDispose)
            {
                return true;
            }
        }
        return false;
    }

    public CardIconItem GetIconItemByID(int iconItemID)
    {
        List<Interactable> requiredIconItems = _requiredIconItemsHolder.GetAllContent();
        for (int i = 0; i < requiredIconItems.Count; i++)
        {
            CardIconItem item = (CardIconItem)requiredIconItems[i];
            if (item.ID == iconItemID)
            {
                return item;
            }
        }
        return null;
    }

    public void DeleteIconItemByID(int iconItemID)
    {
        List<Interactable> requiredIconItems = _requiredIconItemsHolder.GetAllContent();
        for (int i = 0; i < requiredIconItems.Count; i++)
        {
            CardIconItem item = (CardIconItem)requiredIconItems[i];
            if (item.ID == iconItemID)
            {
                _requiredIconItemsHolder.RemoveItemFromContentList(item);
                Destroy(item.gameObject);
                return;
            }
        }
    }

    public void PositionRequiredIconItems()
    {
        List<Interactable> requiredIconItems = _requiredIconItemsHolder.GetAllContent();
        Vector2[] requiredIconPositions = _iconItemsLayout.GetRequiredIconItemPositions(requiredIconItems.Count, REQUIRED_ICON_DIMENSION);
        for (int i = 0; i < requiredIconPositions.Length; i++)
        {
            CardIconItem item = (CardIconItem)requiredIconItems[i];
            item.ToggleRayCast(false);
            RectTransform rect = item.GetComponent<RectTransform>();
            rect.anchoredPosition = requiredIconPositions[i];
        }
    }

    public void Toggle(bool value)
    {
        if(value)
        {
            Rotate();
        }
        gameObject.SetActive(value);
    }

    public void ToggleScoreItem(bool value)
    {
        _scoreItem.gameObject.SetActive(value);
    }

    public void Rotate(float target = 0f)
    {
        Vector3 rotation = new(0f, 0f, target);
        List<Interactable> topIconItems = _topIconItemsHolder.GetAllContent();
        for (int i = 0; i < topIconItems.Count; i++)
        {
            CardIconItem item = (CardIconItem)topIconItems[i];
            item.GetComponent<RectTransform>().eulerAngles = rotation;
        }

        if (!_requiredIconItemsHolder.IsEmpty())
        {
            List<Interactable> requiredIconItems = _requiredIconItemsHolder.GetAllContent();
            for (int i = 0; i < requiredIconItems.Count; i++)
            {
                CardIconItem item = (CardIconItem)requiredIconItems[i];
                item.GetComponent<RectTransform>().eulerAngles = rotation;
            }
        }

        if(_scoreItem != null)
        {
            _scoreItem.GetComponent<RectTransform>().eulerAngles = rotation;
        }
    }
}
