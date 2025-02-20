using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static UnityEditor.Progress;

public class CardIconItemsView : MonoBehaviour
{
    private readonly static float TOP_ICON_DIMENSION = 40f;
    private readonly static float REQUIRED_ICON_DIMENSION = 20f;

    private CardIconItemsLayout _iconItemsLayout;
    private Holder _topIconItemsHolder;
    private Holder _requiredIconItemsHolder;
    
    public void Init(CardData data)
    {
        if(_iconItemsLayout == null)
        {
            _iconItemsLayout = new CardIconItemsLayout();
            _topIconItemsHolder = Instantiate(GameAssets.Instance.topIconsHolderPrefab, transform).GetComponent<Holder>();
            _requiredIconItemsHolder = Instantiate(GameAssets.Instance.requiredIconsHolderPrefab, transform).GetComponent<Holder>();
        }
        else // delete icons from prev initialization
        {
            _topIconItemsHolder.transform.GetComponentsInChildren<CardIconItem>().ToList().ForEach(item => Destroy(item.gameObject));
            _requiredIconItemsHolder.transform.GetComponentsInChildren<CardIconItem>().ToList().ForEach(item => Destroy(item.gameObject));
        }
        _topIconItemsHolder.Init(-1, HolderType.CardIcon);
        _requiredIconItemsHolder.Init(-1, HolderType.CardIcon);

        data.icons.ToList().ForEach(icon =>
        {
            CardIconItem item = Instantiate(GameAssets.Instance.cardIconItemPrefab, _topIconItemsHolder.transform).GetComponent<CardIconItem>();
            item.Create(new List<CardIcon>() { icon }, IconItemType.SINGLE, TOP_ICON_DIMENSION);
            _topIconItemsHolder.AddToContentList(item);
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
                        CardIconItem item = Instantiate(GameAssets.Instance.cardIconItemPrefab, _requiredIconItemsHolder.transform).GetComponent<CardIconItem>();
                        item.Create(new List<CardIcon>() { icon }, IconItemType.SINGLE, REQUIRED_ICON_DIMENSION);
                        _requiredIconItemsHolder.AddToContentList(item);
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
                        CardIconItem item = Instantiate(GameAssets.Instance.cardIconItemPrefab, _requiredIconItemsHolder.transform).GetComponent<CardIconItem>();
                        item.Create(pair, IconItemType.OPTIONAL, REQUIRED_ICON_DIMENSION);
                        _requiredIconItemsHolder.AddToContentList(item);
                    }
                }
            }

            if(data.adjacentRequirements.Length > 0)
            {
                if(data.adjacentRequirements.Length == 1)
                {
                    CardIconItem item = Instantiate(GameAssets.Instance.cardIconItemPrefab, _requiredIconItemsHolder.transform).GetComponent<CardIconItem>();
                    item.Create(new List<CardIcon>() { data.adjacentRequirements[0] }, IconItemType.ADJACENT, REQUIRED_ICON_DIMENSION);
                    _requiredIconItemsHolder.AddToContentList(item);
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
                            CardIconItem item = Instantiate(GameAssets.Instance.cardIconItemPrefab, _requiredIconItemsHolder.transform).GetComponent<CardIconItem>();
                            item.Create(pair, IconItemType.OPTIONAL_AND_ADJACENT, REQUIRED_ICON_DIMENSION);
                            _requiredIconItemsHolder.AddToContentList(item);
                        }
                    }
                }
            }

            List<Interactable> requiredIconItems = _requiredIconItemsHolder.GetAllContent();
            Vector2[] requiredIconPositions = _iconItemsLayout.GetRequiredIconItemPositions(requiredIconItems.Count, REQUIRED_ICON_DIMENSION);
            for (int i = 0; i < requiredIconPositions.Length; i++)
            {
                CardIconItem item = (CardIconItem)requiredIconItems[i];
                RectTransform rect = item.GetComponent<RectTransform>();
                rect.anchoredPosition = requiredIconPositions[i];
            }
        }
        ToggleRaycast(false);
        Toggle(false);
    }

    public void ToggleRaycast(bool value)
    {
        List<Interactable> topIconItems = _topIconItemsHolder.GetAllContent();
        for(int i = 0; i < topIconItems.Count; i++)
        {
            CardIconItem item = (CardIconItem)topIconItems[i];
            item.ToggleRayCast(value);
        }

        if(!_requiredIconItemsHolder.IsEmpty())
        {
            List<Interactable> requiredIconItems = _requiredIconItemsHolder.GetAllContent();
            for (int i = 0; i < requiredIconItems.Count; i++)
            {
                CardIconItem item = (CardIconItem)requiredIconItems[i];
                item.ToggleRayCast(value);
            }
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

    public void Rotate()
    {
        Vector3 rotation = new(0f, 0f, 0f);
        List<Interactable> topIconItems = _topIconItemsHolder.GetAllContent();
        for (int i = 0; i < topIconItems.Count; i++)
        {
            CardIconItem item = (CardIconItem)topIconItems[i];
            item.transform.eulerAngles = rotation;
        }

        if (!_requiredIconItemsHolder.IsEmpty())
        {
            List<Interactable> requiredIconItems = _requiredIconItemsHolder.GetAllContent();
            for (int i = 0; i < requiredIconItems.Count; i++)
            {
                CardIconItem item = (CardIconItem)requiredIconItems[i];
                item.transform.eulerAngles = rotation;
            }
        }
    }
}
